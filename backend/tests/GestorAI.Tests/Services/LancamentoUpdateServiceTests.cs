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

public class LancamentoUpdateServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, LancamentoService svc) Setup()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        var parcelamentoService = new ParcelamentoService(db, tc);
        return (db, new LancamentoService(db, tc, parcelamentoService));
    }

    private async Task<Lancamento> SeedAsync(AppDbContext db, StatusLancamento status, Guid? vendaId = null)
    {
        var l = new Lancamento
        {
            EmpresaId = _empresaId,
            Tipo = TipoLancamento.Despesa,
            Descricao = "Original",
            Valor = 100m,
            DataVencimento = DateTime.UtcNow.AddDays(5),
            Status = status,
            Categoria = "Aluguel",
            VendaId = vendaId,
        };
        db.Lancamentos.Add(l);
        await db.SaveChangesAsync();
        return l;
    }

    [Fact]
    public async Task UpdateAsync_AtualizaLancamento_QuandoPendente()
    {
        var (db, svc) = Setup();
        var l = await SeedAsync(db, StatusLancamento.Pendente);
        var req = new UpdateLancamentoRequest(
            "Receita", "Novo nome", 250m,
            DateTime.UtcNow.AddDays(10), "Serviço", "Obs");

        var result = await svc.UpdateAsync(l.Id, req, default);

        Assert.Equal("Receita", result.Tipo);
        Assert.Equal("Novo nome", result.Descricao);
        Assert.Equal(250m, result.Valor);
        Assert.Equal("Obs", result.Observacao);
    }

    [Fact]
    public async Task UpdateAsync_LancaExcecao_QuandoPago()
    {
        var (db, svc) = Setup();
        var l = await SeedAsync(db, StatusLancamento.Pago);
        var req = new UpdateLancamentoRequest("Despesa", "Qualquer", 50m,
            DateTime.UtcNow, "Outros", null);

        await Assert.ThrowsAsync<AppException>(() => svc.UpdateAsync(l.Id, req, default));
    }

    [Fact]
    public async Task UpdateAsync_LancaExcecao_QuandoVinculadoAVenda()
    {
        var (db, svc) = Setup();
        var l = await SeedAsync(db, StatusLancamento.Pendente, vendaId: Guid.NewGuid());
        var req = new UpdateLancamentoRequest("Despesa", "Qualquer", 50m,
            DateTime.UtcNow, "Outros", null);

        await Assert.ThrowsAsync<AppException>(() => svc.UpdateAsync(l.Id, req, default));
    }
}
