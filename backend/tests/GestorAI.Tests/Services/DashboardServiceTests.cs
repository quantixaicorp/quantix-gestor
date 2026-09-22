using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Dashboard;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class DashboardServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, DashboardService service) Setup()
    {
        var tenantContext = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new DashboardService(db));
    }

    [Fact]
    public async Task GetKpisAsync_TotalVendidoHoje_SomaVendasConcluidas()
    {
        var (db, service) = Setup();
        var hoje = DateTime.UtcNow;
        var ontem = hoje.AddDays(-1);

        db.Sales.AddRange(
            new Sale
            {
                CompanyId = _empresaId, SaleDate = hoje,
                Status = StatusVenda.Concluida, Total = 300m,
                Subtotal = 300m, Discount = 0m,
                PaymentMethod = FormaPagamento.Pix
            },
            new Sale
            {
                CompanyId = _empresaId, SaleDate = ontem,
                Status = StatusVenda.Concluida, Total = 500m,
                Subtotal = 500m, Discount = 0m,
                PaymentMethod = FormaPagamento.Dinheiro
            },
            new Sale
            {
                CompanyId = _empresaId, SaleDate = hoje,
                Status = StatusVenda.Cancelada, Total = 100m,
                Subtotal = 100m, Discount = 0m,
                PaymentMethod = FormaPagamento.Pix
            });
        await db.SaveChangesAsync();

        var result = await service.GetDashboardAsync(default);

        Assert.Equal(300m, result.Kpis.TotalVendidoHoje);
    }

    [Fact]
    public async Task GetKpisAsync_ProdutosEstoqueBaixo_ContaProdutosAbaixoMinimo()
    {
        var (db, service) = Setup();
        var cat = new Category { CompanyId = _empresaId, Name = "Cat" };
        db.Categories.Add(cat);
        db.Products.AddRange(
            new Product
            {
                CompanyId = _empresaId, CategoryId = cat.Id,
                Name = "Baixo", SalePrice = 10m,
                CurrentStock = 1m, MinimumStock = 5m
            },
            new Product
            {
                CompanyId = _empresaId, CategoryId = cat.Id,
                Name = "OK", SalePrice = 10m,
                CurrentStock = 10m, MinimumStock = 2m
            });
        await db.SaveChangesAsync();

        var result = await service.GetDashboardAsync(default);

        Assert.Equal(1, result.Kpis.ProdutosEstoqueBaixo);
    }

    [Fact]
    public async Task GetDashboardAsync_TopProdutos_OrdenaPorQuantidadeVendida()
    {
        var (db, service) = Setup();
        var cat = new Category { CompanyId = _empresaId, Name = "Cat" };
        db.Categories.Add(cat);
        var p1 = new Product { CompanyId = _empresaId, CategoryId = cat.Id, Name = "A", SalePrice = 10m, CurrentStock = 0m, MinimumStock = 0m };
        var p2 = new Product { CompanyId = _empresaId, CategoryId = cat.Id, Name = "B", SalePrice = 20m, CurrentStock = 0m, MinimumStock = 0m };
        db.Products.AddRange(p1, p2);

        var venda = new Sale
        {
            CompanyId = _empresaId, SaleDate = DateTime.UtcNow,
            Status = StatusVenda.Concluida, Subtotal = 50m,
            Discount = 0m, Total = 50m, PaymentMethod = FormaPagamento.Pix
        };
        db.Sales.Add(venda);
        await db.SaveChangesAsync();

        db.SaleItems.AddRange(
            new SaleItem { SaleId = venda.Id, ProductId = p1.Id, Quantity = 3m, UnitPrice = 10m, Discount = 0m, Total = 30m },
            new SaleItem { SaleId = venda.Id, ProductId = p2.Id, Quantity = 1m, UnitPrice = 20m, Discount = 0m, Total = 20m });
        await db.SaveChangesAsync();

        var result = await service.GetDashboardAsync(default);

        Assert.Equal("A", result.TopProdutos[0].Name);
        Assert.Equal(3m, result.TopProdutos[0].QuantidadeVendida);
    }
}
