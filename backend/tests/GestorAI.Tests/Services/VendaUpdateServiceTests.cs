using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Vendas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Vendas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class VendaUpdateServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, VendaService svc) Setup()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new VendaService(db, tc));
    }

    private async Task<(Venda venda, Lancamento lancamento, Cliente cliente)> SeedAsync(AppDbContext db)
    {
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Carlos", Whatsapp = "11999990001" };
        db.Clientes.Add(cliente);

        var venda = new Venda
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            Status = StatusVenda.Concluida,
            FormaPagamento = FormaPagamento.Pix,
            DataHora = DateTime.UtcNow.AddDays(-1),
            Total = 100m,
        };
        db.Vendas.Add(venda);
        await db.SaveChangesAsync();

        var lancamento = new Lancamento
        {
            EmpresaId = _empresaId,
            Tipo = TipoLancamento.Receita,
            Descricao = $"Venda — {cliente.Nome}",
            Valor = 100m,
            DataVencimento = venda.DataHora,
            DataPagamento = venda.DataHora,
            Status = StatusLancamento.Pago,
            Categoria = "Venda",
            VendaId = venda.Id,
        };
        db.Lancamentos.Add(lancamento);
        await db.SaveChangesAsync();

        return (venda, lancamento, cliente);
    }

    [Fact]
    public async Task UpdateAsync_AtualizaVenda_QuandoConcluida()
    {
        var (db, svc) = Setup();
        var (venda, _, _) = await SeedAsync(db);
        var novaData = DateTime.UtcNow.AddDays(-2);
        var req = new UpdateVendaRequest(null, "Dinheiro", novaData);

        var result = await svc.UpdateAsync(venda.Id, req, default);

        Assert.Null(result.ClienteId);
        Assert.Equal("Dinheiro", result.FormaPagamento);
        Assert.Equal(novaData.Date, result.DataHora.Date);
    }

    [Fact]
    public async Task UpdateAsync_LancaExcecao_QuandoCancelada()
    {
        var (db, svc) = Setup();
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "X", Whatsapp = "1" };
        db.Clientes.Add(cliente);
        var venda = new Venda
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Status = StatusVenda.Cancelada, FormaPagamento = FormaPagamento.Pix,
        };
        db.Vendas.Add(venda);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(
            () => svc.UpdateAsync(venda.Id, new UpdateVendaRequest(null, "Pix", DateTime.UtcNow), default));
    }

    [Fact]
    public async Task UpdateAsync_AtualizaLancamentoVinculado()
    {
        var (db, svc) = Setup();
        var (venda, lancamento, _) = await SeedAsync(db);
        var novaData = DateTime.UtcNow.AddDays(-3);
        var req = new UpdateVendaRequest(null, "Dinheiro", novaData);

        await svc.UpdateAsync(venda.Id, req, default);

        var lancAtualizado = await db.Lancamentos.IgnoreQueryFilters().FirstAsync(l => l.Id == lancamento.Id);
        Assert.Equal(novaData.Date, lancAtualizado.DataVencimento.Date);
        Assert.Equal("Venda — Venda balcão", lancAtualizado.Descricao);
    }
}
