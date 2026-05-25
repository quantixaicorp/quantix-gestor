using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
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
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new LancamentoService(db, tenantContext));
    }

    [Fact]
    public async Task CreateAsync_PersisteDespesa()
    {
        var (_, service) = Setup();
        var req = new CreateLancamentoRequest(
            "Despesa", "Aluguel", 1500m,
            DateTime.Today.AddDays(5), "Aluguel", null);

        var result = await service.CreateAsync(req, default);

        Assert.Equal("Despesa", result.Tipo);
        Assert.Equal(1500m, result.Valor);
        Assert.Equal("Pendente", result.Status);
        Assert.False(result.Vencido);
    }

    [Fact]
    public async Task PagarAsync_SetaStatusPagoEDataPagamento()
    {
        var (db, service) = Setup();
        var lancamento = new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
            Descricao = "Água", Valor = 100m,
            DataVencimento = DateTime.Today.AddDays(-1),
            Status = StatusLancamento.Pendente,
            Categoria = "Utilidades"
        };
        db.Lancamentos.Add(lancamento);
        await db.SaveChangesAsync();

        var dataPagamento = DateTime.Today;
        var result = await service.PagarAsync(lancamento.Id, new PagarLancamentoRequest(dataPagamento), default);

        Assert.Equal("Pago", result.Status);
        Assert.Equal(dataPagamento, result.DataPagamento!.Value.Date);
    }

    [Fact]
    public async Task PagarAsync_ThrowsSeJaPago()
    {
        var (db, service) = Setup();
        var lancamento = new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
            Descricao = "Já pago", Valor = 50m,
            DataVencimento = DateTime.Today,
            DataPagamento = DateTime.Today,
            Status = StatusLancamento.Pago,
            Categoria = "Outros"
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
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
            Descricao = "Vencida", Valor = 200m,
            DataVencimento = DateTime.Today.AddDays(-3),
            Status = StatusLancamento.Pendente,
            Categoria = "Outros"
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
            new Lancamento
            {
                EmpresaId = _empresaId, Tipo = TipoLancamento.Receita,
                Descricao = "Venda", Valor = 500m,
                DataVencimento = hoje, DataPagamento = hoje,
                Status = StatusLancamento.Pago, Categoria = "Venda"
            },
            new Lancamento
            {
                EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
                Descricao = "Fornecedor", Valor = 200m,
                DataVencimento = hoje, DataPagamento = hoje,
                Status = StatusLancamento.Pago, Categoria = "Compras"
            });
        await db.SaveChangesAsync();

        var result = await service.GetFluxoCaixaAsync(hoje, hoje, default);

        Assert.Equal(500m, result.TotalReceitas);
        Assert.Equal(200m, result.TotalDespesas);
        Assert.Equal(300m, result.SaldoFinal);
    }
}
