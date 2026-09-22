using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Vendas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Vendas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class VendaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, VendaService service) Setup()
    {
        var tenantContext = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new VendaService(db, tenantContext));
    }

    private async Task<Product> SeedProdutoAsync(AppDbContext db, decimal preco = 50m,
        decimal custo = 20m, decimal estoque = 10m)
    {
        var cat = new Category { CompanyId = _empresaId, Name = "Cat" };
        db.Categories.Add(cat);
        var p = new Product
        {
            CompanyId = _empresaId, CategoryId = cat.Id,
            Name = "Product Teste", SalePrice = preco,
            AverageCost = custo, CurrentStock = estoque, MinimumStock = 1m
        };
        db.Products.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    [Fact]
    public async Task CreateAsync_CriasVendaComItensEMovimentacoes()
    {
        var (db, service) = Setup();
        var produto = await SeedProdutoAsync(db);
        var req = new CreateVendaRequest(
            null,
            [new ItemVendaRequest(produto.Id, 2m, 0m)],
            0m, "Pix", null, null);

        var result = await service.CreateAsync(req, default);

        Assert.Equal(StatusVenda.Concluida.ToString(), result.Status);
        Assert.Equal(100m, result.Total); // 2 * 50
        Assert.Single(result.Items);

        var produto2 = await db.Products.FindAsync(produto.Id);
        Assert.Equal(8m, produto2!.CurrentStock); // 10 - 2

        var mov = await db.StockMovements.IgnoreQueryFilters().FirstAsync();
        Assert.Equal(TipoMovimentacao.Saida, mov.Type);
        Assert.Equal(OrigemMovimentacao.Venda, mov.Source);
    }

    [Fact]
    public async Task CreateAsync_CriaLancamentoDeReceita()
    {
        var (db, service) = Setup();
        var produto = await SeedProdutoAsync(db);
        var req = new CreateVendaRequest(
            null,
            [new ItemVendaRequest(produto.Id, 1m, 0m)],
            0m, "Dinheiro", null, null);

        await service.CreateAsync(req, default);

        var lancamento = await db.Transactions.IgnoreQueryFilters().FirstAsync();
        Assert.Equal(TipoLancamento.Receita, lancamento.Type);
        Assert.Equal(50m, lancamento.Amount);
        Assert.Equal(StatusLancamento.Pago, lancamento.Status);
    }

    [Fact]
    public async Task CreateAsync_ThrowsQuandoEstoqueInsuficiente()
    {
        var (db, service) = Setup();
        var produto = await SeedProdutoAsync(db, estoque: 1m);
        var req = new CreateVendaRequest(
            null,
            [new ItemVendaRequest(produto.Id, 5m, 0m)],
            0m, "Pix", null, null);

        await Assert.ThrowsAsync<AppException>(() => service.CreateAsync(req, default));
    }

    [Fact]
    public async Task CreateAsync_AplicaDescontoCorretamente()
    {
        var (db, service) = Setup();
        var produto = await SeedProdutoAsync(db, preco: 100m);
        var req = new CreateVendaRequest(
            null,
            [new ItemVendaRequest(produto.Id, 2m, 0m)],
            30m, "Cartao", null, null);

        var result = await service.CreateAsync(req, default);

        Assert.Equal(200m, result.Subtotal);
        Assert.Equal(170m, result.Total);
    }

    [Fact]
    public async Task CancelarAsync_EstornaEstoqueECancelalancamento()
    {
        var (db, service) = Setup();
        var produto = await SeedProdutoAsync(db, estoque: 10m);
        var req = new CreateVendaRequest(
            null,
            [new ItemVendaRequest(produto.Id, 3m, 0m)],
            0m, "Pix", null, null);
        var venda = await service.CreateAsync(req, default);
        Assert.Equal(7m, (await db.Products.FindAsync(produto.Id))!.CurrentStock);

        await service.CancelarAsync(venda.Id, default);

        var produtoAtualizado = await db.Products.FindAsync(produto.Id);
        Assert.Equal(10m, produtoAtualizado!.CurrentStock); // estorno

        var vendaAtualizada = await db.Sales.FindAsync(venda.Id);
        Assert.Equal(StatusVenda.Cancelada, vendaAtualizada!.Status);

        var lancamento = await db.Transactions.IgnoreQueryFilters().FirstAsync();
        Assert.Equal(StatusLancamento.Cancelado, lancamento.Status);
    }
}
