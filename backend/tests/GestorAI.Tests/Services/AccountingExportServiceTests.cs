using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Services.Contabilidade;
using GestorAI.API.Shared.Exceptions;
using GestorAI.Tests.Helpers;

namespace GestorAI.Tests.Services;

public class AccountingExportServiceTests
{
    [Fact]
    public async Task ExportAsync_DominioFormat_ContainsCorrectLines()
    {
        var (db, tenant) = TestDbHelper.Create();

        // Setup chart of accounts
        var coaSvc = new ChartOfAccountService(db, tenant);
        var cashAcc = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("1.1.1.02", "Banco CC", AccountType.Ativo, null), default);
        var recAcc = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("4.1.1", "Receita Vendas", AccountType.Receita, null), default);

        // Setup settings
        var settingsSvc = new AccountingSettingsService(db, tenant);
        await settingsSvc.UpdateAsync(
            new UpdateAccountingSettingsRequest(cashAcc.Id, AccountingSystem.Dominio), default);

        // Setup mapping
        var mappingSvc = new AccountMappingService(db, tenant);
        await mappingSvc.BulkUpsertAsync(
            new BulkUpsertAccountMappingsRequest([new("Vendas", recAcc.Id)]), default);

        // Add paid transaction
        db.Transactions.Add(new Transaction
        {
            CompanyId = tenant.CompanyId,
            Type = TipoLancamento.Receita,
            Description = "Venda balcão",
            Amount = 500m,
            DueDate = new DateTime(2025, 1, 15),
            PaymentDate = new DateTime(2025, 1, 15),
            Status = StatusLancamento.Pago,
            Category = "Vendas",
            Source = TransactionSource.Manual,
        });
        await db.SaveChangesAsync();

        var svc = new AccountingExportService(db, tenant);
        var req = new AccountingExportRequest(AccountingSystem.Dominio, ["2025-01"], false);
        var (content, fileName, contentType) = await svc.ExportAsync(req, default);

        var text = System.Text.Encoding.UTF8.GetString(content);
        Assert.Contains("15/01/2025", text);
        Assert.Contains("1.1.1.02", text);
        Assert.Contains("4.1.1", text);
        Assert.Contains("500,00", text);
        Assert.Equal("text/plain", contentType);
    }

    [Fact]
    public async Task ExportAsync_FortesFormat_HasCsvHeader()
    {
        var (db, tenant) = TestDbHelper.Create();
        var coaSvc = new ChartOfAccountService(db, tenant);
        var cashAcc = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("1.1.1.02", "Banco CC", AccountType.Ativo, null), default);
        var despAcc = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("5.1.1.01", "Aluguel", AccountType.Despesa, null), default);

        var settingsSvc = new AccountingSettingsService(db, tenant);
        await settingsSvc.UpdateAsync(
            new UpdateAccountingSettingsRequest(cashAcc.Id, AccountingSystem.Fortes), default);

        var mappingSvc = new AccountMappingService(db, tenant);
        await mappingSvc.BulkUpsertAsync(
            new BulkUpsertAccountMappingsRequest([new("Aluguel", despAcc.Id)]), default);

        db.Transactions.Add(new Transaction
        {
            CompanyId = tenant.CompanyId,
            Type = TipoLancamento.Despesa,
            Description = "Aluguel janeiro",
            Amount = 2000m,
            DueDate = new DateTime(2025, 1, 5),
            PaymentDate = new DateTime(2025, 1, 5),
            Status = StatusLancamento.Pago,
            Category = "Aluguel",
            Source = TransactionSource.Manual,
        });
        await db.SaveChangesAsync();

        var svc = new AccountingExportService(db, tenant);
        var req = new AccountingExportRequest(AccountingSystem.Fortes, ["2025-01"], false);
        var (content, fileName, contentType) = await svc.ExportAsync(req, default);

        var text = System.Text.Encoding.UTF8.GetString(content);
        var lines = text.Split('\n');
        Assert.Equal("Data;Historico;Debito;Credito;Valor", lines[0].Trim());
        Assert.Contains("5.1.1.01", text);
        Assert.Contains("1.1.1.02", text);
        Assert.Equal("text/csv", contentType);
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenNoCashAccount()
    {
        var (db, tenant) = TestDbHelper.Create();
        var svc = new AccountingExportService(db, tenant);
        var req = new AccountingExportRequest(AccountingSystem.Dominio, ["2025-01"], false);

        await Assert.ThrowsAsync<AppException>(() => svc.ExportAsync(req, default));
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenCategoryNotMapped()
    {
        var (db, tenant) = TestDbHelper.Create();
        var coaSvc = new ChartOfAccountService(db, tenant);
        var cashAcc = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("1.1.1.02", "Banco CC", AccountType.Ativo, null), default);
        var settingsSvc = new AccountingSettingsService(db, tenant);
        await settingsSvc.UpdateAsync(new UpdateAccountingSettingsRequest(cashAcc.Id, null), default);

        db.Transactions.Add(new Transaction
        {
            CompanyId = tenant.CompanyId,
            Type = TipoLancamento.Receita,
            Description = "Venda",
            Amount = 100m,
            DueDate = new DateTime(2025, 1, 1),
            PaymentDate = new DateTime(2025, 1, 1),
            Status = StatusLancamento.Pago,
            Category = "Vendas",
            Source = TransactionSource.Manual,
        });
        await db.SaveChangesAsync();

        var svc = new AccountingExportService(db, tenant);
        var req = new AccountingExportRequest(AccountingSystem.Dominio, ["2025-01"], false);

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.ExportAsync(req, default));
        Assert.Contains("Vendas", ex.Message);
    }
}
