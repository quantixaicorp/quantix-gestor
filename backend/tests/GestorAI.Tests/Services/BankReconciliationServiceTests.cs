using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Conciliacao;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class BankReconciliationServiceTests
{
    private readonly Guid _companyId = Guid.NewGuid();

    private (AppDbContext db, BankReconciliationService svc, BankAccount account) Setup()
    {
        var tenant = new TenantContext { CompanyId = _companyId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);

        var account = new BankAccount
        {
            CompanyId = _companyId,
            Name = "Conta Teste",
            BankName = "Banco Teste"
        };
        db.BankAccounts.Add(account);
        db.SaveChanges();

        return (db, new BankReconciliationService(db, tenant, new BankStatementParserService()), account);
    }

    private Transaction AddTransaction(AppDbContext db, decimal amount, DateTime dueDate,
        TipoLancamento type = TipoLancamento.Receita)
    {
        var t = new Transaction
        {
            CompanyId = _companyId,
            Type = type,
            Description = "Lançamento teste",
            Amount = amount,
            DueDate = dueDate,
            Status = StatusLancamento.Pendente,
            Category = "Teste",
        };
        db.Transactions.Add(t);
        db.SaveChanges();
        return t;
    }

    [Fact]
    public async Task ImportAsync_ExactMatch_SetAutoConciliated()
    {
        var (db, svc, account) = Setup();
        var txDate = new DateTime(2025, 8, 1);
        AddTransaction(db, 250m, txDate, TipoLancamento.Receita);

        var parsed = new List<ParsedTransaction>
        {
            new(new DateOnly(2025, 8, 1), 250m, "PIX RECEBIDO", "FITID001")
        };

        var stmt = await svc.ImportAsync(account.Id, "extrato.ofx",
            BankStatementFormat.OFX, parsed, default);

        var item = db.BankStatementItems.Include(i => i.Reconciliation).First();
        Assert.Equal(BankStatementItemStatus.AutoConciliated, item.Status);
        Assert.NotNull(item.Reconciliation);
        Assert.Equal(100, item.Reconciliation!.ConfidenceScore);
    }

    [Fact]
    public async Task ImportAsync_DateWithin7Days_SetPendingReview()
    {
        var (db, svc, account) = Setup();
        var txDate = new DateTime(2025, 8, 5);
        AddTransaction(db, 250m, txDate, TipoLancamento.Receita);

        var parsed = new List<ParsedTransaction>
        {
            new(new DateOnly(2025, 8, 1), 250m, "PIX RECEBIDO", null)
        };

        await svc.ImportAsync(account.Id, "extrato.ofx",
            BankStatementFormat.OFX, parsed, default);

        var item = db.BankStatementItems.Include(i => i.Reconciliation).First();
        Assert.Equal(BankStatementItemStatus.PendingReview, item.Status);
        Assert.NotNull(item.Reconciliation);
        Assert.True(item.Reconciliation!.ConfidenceScore < 100);
    }

    [Fact]
    public async Task ImportAsync_NoMatch_CreatesNewTransaction()
    {
        var (db, svc, account) = Setup();

        var parsed = new List<ParsedTransaction>
        {
            new(new DateOnly(2025, 8, 1), 300m, "TAXA BANCARIA", null)
        };

        await svc.ImportAsync(account.Id, "extrato.ofx",
            BankStatementFormat.OFX, parsed, default);

        var item = db.BankStatementItems.First();
        Assert.Equal(BankStatementItemStatus.Unmatched, item.Status);
        var created = db.Transactions.FirstOrDefault(t => t.Source == TransactionSource.BankImport);
        Assert.NotNull(created);
        Assert.Equal(300m, created!.Amount);
    }

    [Fact]
    public async Task IgnoreAsync_SetsManuallyIgnored()
    {
        var (db, svc, account) = Setup();
        var parsed = new List<ParsedTransaction>
        {
            new(new DateOnly(2025, 8, 1), 10m, "IOF", null)
        };
        await svc.ImportAsync(account.Id, "extrato.ofx",
            BankStatementFormat.OFX, parsed, default);

        var item = db.BankStatementItems.First();
        // delete auto-created transaction (and its reconciliation) so we can ignore
        var tx = db.Transactions.FirstOrDefault(t => t.Source == TransactionSource.BankImport);
        if (tx != null)
        {
            var recon = db.BankReconciliations.FirstOrDefault(r => r.TransactionId == tx.Id);
            if (recon != null) db.BankReconciliations.Remove(recon);
            db.Transactions.Remove(tx);
        }
        db.SaveChanges();

        await svc.IgnoreAsync(item.Id, default);

        var updated = db.BankStatementItems.Find(item.Id)!;
        Assert.Equal(BankStatementItemStatus.ManuallyIgnored, updated.Status);
    }
}
