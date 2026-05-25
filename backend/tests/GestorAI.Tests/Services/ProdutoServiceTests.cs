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
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        var service = new ProdutoService(db, tenantContext);
        return (db, service);
    }

    private async Task<Categoria> SeedCategoriaAsync(AppDbContext db)
    {
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Categoria Teste" };
        db.Categorias.Add(cat);
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

        Assert.Equal("Camiseta", result.Nome);
        Assert.Equal(_empresaId, db.Produtos.First().EmpresaId);
    }

    [Fact]
    public async Task EntradaEstoqueAsync_UpdatesEstoqueAtualAndCustoMedio()
    {
        var (db, service) = Setup();
        var cat = await SeedCategoriaAsync(db);
        var produto = new Produto
        {
            EmpresaId = _empresaId, CategoriaId = cat.Id,
            Nome = "Produto", PrecoVenda = 100m,
            CustoMedio = 20m, EstoqueAtual = 10m, EstoqueMinimo = 2m
        };
        db.Produtos.Add(produto);
        await db.SaveChangesAsync();

        var req = new EntradaEstoqueRequest(produto.Id, 10m, 30m, null);
        await service.EntradaEstoqueAsync(req, default);

        var atualizado = await db.Produtos.FindAsync(produto.Id);
        Assert.Equal(20m, atualizado!.EstoqueAtual);
        Assert.Equal(25m, atualizado.CustoMedio); // (10*20 + 10*30) / 20 = 25
    }

    [Fact]
    public async Task EntradaEstoqueAsync_CreatesMovimentacaoEstoque()
    {
        var (db, service) = Setup();
        var cat = await SeedCategoriaAsync(db);
        var produto = new Produto
        {
            EmpresaId = _empresaId, CategoriaId = cat.Id,
            Nome = "Produto", PrecoVenda = 50m,
            CustoMedio = 10m, EstoqueAtual = 5m, EstoqueMinimo = 2m
        };
        db.Produtos.Add(produto);
        await db.SaveChangesAsync();

        await service.EntradaEstoqueAsync(
            new EntradaEstoqueRequest(produto.Id, 5m, null, "Reposição"), default);

        var mov = await db.MovimentacoesEstoque.IgnoreQueryFilters().FirstAsync();
        Assert.Equal(TipoMovimentacao.Entrada, mov.Tipo);
        Assert.Equal(OrigemMovimentacao.Manual, mov.Origem);
        Assert.Equal(5m, mov.Quantidade);
    }

    [Fact]
    public async Task EntradaEstoqueAsync_ThrowsWhenProdutoNotFound()
    {
        var (_, service) = Setup();
        var req = new EntradaEstoqueRequest(Guid.NewGuid(), 5m, null, null);

        await Assert.ThrowsAsync<AppException>(() => service.EntradaEstoqueAsync(req, default));
    }
}
