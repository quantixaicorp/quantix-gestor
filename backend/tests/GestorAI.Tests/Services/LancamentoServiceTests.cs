using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Compras;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class LancamentoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, LancamentoService service) Setup()
    {
        var tenantContext = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        var parcelamentoService = new ParcelamentoService(db, tenantContext);
        return (db, new LancamentoService(db, tenantContext, parcelamentoService));
    }

    [Fact]
    public async Task CreateAsync_PersisteDespesa()
    {
        var (_, service) = Setup();
        var req = new CreateLancamentoRequest(
            "Despesa", "Aluguel", 1500m,
            DateTime.Today.AddDays(5), "Aluguel", null);

        var result = await service.CreateAsync(req, default);

        Assert.Equal("Despesa", result.Type);
        Assert.Equal(1500m, result.Amount);
        Assert.Equal("Pendente", result.Status);
        Assert.False(result.Vencido);
    }

    [Fact]
    public async Task PagarAsync_SetaStatusPagoEDataPagamento()
    {
        var (db, service) = Setup();
        var lancamento = new Transaction
        {
            CompanyId = _empresaId, Type = TipoLancamento.Despesa,
            Description = "Água", Amount = 100m,
            DueDate = DateTime.Today.AddDays(-1),
            Status = StatusLancamento.Pendente,
            Category = "Utilidades"
        };
        db.Lancamentos.Add(lancamento);
        await db.SaveChangesAsync();

        var dataPagamento = DateTime.Today;
        var result = await service.PagarAsync(lancamento.Id, new PagarLancamentoRequest(dataPagamento), default);

        Assert.Equal("Pago", result.Status);
        Assert.Equal(dataPagamento, result.PaymentDate!.Value.Date);
    }

    [Fact]
    public async Task PagarAsync_ThrowsSeJaPago()
    {
        var (db, service) = Setup();
        var lancamento = new Transaction
        {
            CompanyId = _empresaId, Type = TipoLancamento.Despesa,
            Description = "Já pago", Amount = 50m,
            DueDate = DateTime.Today,
            PaymentDate = DateTime.Today,
            Status = StatusLancamento.Pago,
            Category = "Outros"
        };
        db.Lancamentos.Add(lancamento);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() =>
            service.PagarAsync(lancamento.Id, new PagarLancamentoRequest(DateTime.Today), default));
    }

    [Fact]
    public async Task ListAsync_VencidoCalculadoPorQuery()
    {
        var (db, service) = Setup();
        db.Lancamentos.Add(new Transaction
        {
            CompanyId = _empresaId, Type = TipoLancamento.Despesa,
            Description = "Vencida", Amount = 200m,
            DueDate = DateTime.Today.AddDays(-3),
            Status = StatusLancamento.Pendente,
            Category = "Outros"
        });
        await db.SaveChangesAsync();

        var result = await service.ListAsync(null, null, null, default);

        Assert.Single(result);
        Assert.True(result[0].Vencido);
    }

    [Fact]
    public async Task GetFluxoCaixaAsync_AgregaPorDia()
    {
        var (db, service) = Setup();
        var hoje = DateTime.Today;
        db.Lancamentos.AddRange(
            new Transaction
            {
                CompanyId = _empresaId, Type = TipoLancamento.Receita,
                Description = "Sale", Amount = 500m,
                DueDate = hoje, PaymentDate = hoje,
                Status = StatusLancamento.Pago, Category = "Sale"
            },
            new Transaction
            {
                CompanyId = _empresaId, Type = TipoLancamento.Despesa,
                Description = "Supplier", Amount = 200m,
                DueDate = hoje, PaymentDate = hoje,
                Status = StatusLancamento.Pago, Category = "Compras"
            });
        await db.SaveChangesAsync();

        var result = await service.GetFluxoCaixaAsync(hoje, hoje, default);

        Assert.Equal(500m, result.TotalReceitas);
        Assert.Equal(200m, result.TotalDespesas);
        Assert.Equal(300m, result.SaldoFinal);
    }
}
