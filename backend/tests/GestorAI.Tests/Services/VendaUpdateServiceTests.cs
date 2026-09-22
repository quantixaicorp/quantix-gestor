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
        var tc = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new VendaService(db, tc));
    }

    private async Task<(Sale venda, Transaction lancamento, Customer cliente)> SeedAsync(AppDbContext db)
    {
        var cliente = new Customer { CompanyId = _empresaId, Name = "Carlos", WhatsApp = "11999990001" };
        db.Customers.Add(cliente);

        var venda = new Sale
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Status = StatusVenda.Concluida,
            PaymentMethod = FormaPagamento.Pix,
            SaleDate = DateTime.UtcNow.AddDays(-1),
            Total = 100m,
        };
        db.Sales.Add(venda);
        await db.SaveChangesAsync();

        var lancamento = new Transaction
        {
            CompanyId = _empresaId,
            Type = TipoLancamento.Receita,
            Description = $"Sale — {cliente.Name}",
            Amount = 100m,
            DueDate = venda.SaleDate,
            PaymentDate = venda.SaleDate,
            Status = StatusLancamento.Pago,
            Category = "Sale",
            SaleId = venda.Id,
        };
        db.Transactions.Add(lancamento);
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

        Assert.Null(result.CustomerId);
        Assert.Equal("Dinheiro", result.PaymentMethod);
        Assert.Equal(novaData.Date, result.SaleDate.Date);
    }

    [Fact]
    public async Task UpdateAsync_LancaExcecao_QuandoCancelada()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "X", WhatsApp = "1" };
        db.Customers.Add(cliente);
        var venda = new Sale
        {
            CompanyId = _empresaId, CustomerId = cliente.Id,
            Status = StatusVenda.Cancelada, PaymentMethod = FormaPagamento.Pix,
        };
        db.Sales.Add(venda);
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

        var lancAtualizado = await db.Transactions.IgnoreQueryFilters().FirstAsync(l => l.Id == lancamento.Id);
        Assert.Equal(novaData.Date, lancAtualizado.DueDate.Date);
        Assert.Equal("Sale — Sale balcão", lancAtualizado.Description);
    }
}
