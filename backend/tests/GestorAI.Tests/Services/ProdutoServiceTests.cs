using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Estoque;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ProdutoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ProdutoService service) Setup()
    {
        var tenantContext = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        var service = new ProdutoService(db, tenantContext);
        return (db, service);
    }

    private async Task<Category> SeedCategoriaAsync(AppDbContext db)
    {
        var cat = new Category { CompanyId = _empresaId, Name = "Category Teste" };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }

    [Fact]
    public async Task CreateAsync_PersistsProduto()
    {
        var (db, service) = Setup();
        var cat = await SeedCategoriaAsync(db);
        var req = new CreateProdutoRequest(cat.Id, "Camiseta", null, 50m, 20m, 10m, 3m, null);

        var result = await service.CreateAsync(req, default);

        Assert.Equal("Camiseta", result.Name);
        Assert.Equal(_empresaId, db.Products.First().CompanyId);
    }

    [Fact]
    public async Task EntradaEstoqueAsync_UpdatesEstoqueAtualAndCustoMedio()
    {
        var (db, service) = Setup();
        var cat = await SeedCategoriaAsync(db);
        var produto = new Product
        {
            CompanyId = _empresaId, CategoryId = cat.Id,
            Name = "Product", SalePrice = 100m,
            AverageCost = 20m, CurrentStock = 10m, MinimumStock = 2m
        };
        db.Products.Add(produto);
        await db.SaveChangesAsync();

        var req = new EntradaEstoqueRequest(produto.Id, 10m, 30m, null);
        await service.EntradaEstoqueAsync(req, default);

        var atualizado = await db.Products.FindAsync(produto.Id);
        Assert.Equal(20m, atualizado!.CurrentStock);
        Assert.Equal(25m, atualizado.AverageCost); // (10*20 + 10*30) / 20 = 25
    }

    [Fact]
    public async Task EntradaEstoqueAsync_CreatesMovimentacaoEstoque()
    {
        var (db, service) = Setup();
        var cat = await SeedCategoriaAsync(db);
        var produto = new Product
        {
            CompanyId = _empresaId, CategoryId = cat.Id,
            Name = "Product", SalePrice = 50m,
            AverageCost = 10m, CurrentStock = 5m, MinimumStock = 2m
        };
        db.Products.Add(produto);
        await db.SaveChangesAsync();

        await service.EntradaEstoqueAsync(
            new EntradaEstoqueRequest(produto.Id, 5m, null, "Reposição"), default);

        var mov = await db.StockMovements.IgnoreQueryFilters().FirstAsync();
        Assert.Equal(TipoMovimentacao.Entrada, mov.Type);
        Assert.Equal(OrigemMovimentacao.Manual, mov.Source);
        Assert.Equal(5m, mov.Quantity);
    }

    [Fact]
    public async Task EntradaEstoqueAsync_ThrowsWhenProdutoNotFound()
    {
        var (_, service) = Setup();
        var req = new EntradaEstoqueRequest(Guid.NewGuid(), 5m, null, null);

        await Assert.ThrowsAsync<AppException>(() => service.EntradaEstoqueAsync(req, default));
    }
}
