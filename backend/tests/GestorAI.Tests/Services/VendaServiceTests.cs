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
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new VendaService(db, tenantContext));
    }

    private async Task<Produto> SeedProdutoAsync(AppDbContext db, decimal preco = 50m,
        decimal custo = 20m, decimal estoque = 10m)
    {
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Cat" };
        db.Categorias.Add(cat);
        var p = new Produto
        {
            EmpresaId = _empresaId, CategoriaId = cat.Id,
            Nome = "Produto Teste", PrecoVenda = preco,
            CustoMedio = custo, EstoqueAtual = estoque, EstoqueMinimo = 1m
        };
        db.Produtos.Add(p);
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
        Assert.Single(result.Itens);

        var produto2 = await db.Produtos.FindAsync(produto.Id);
        Assert.Equal(8m, produto2!.EstoqueAtual); // 10 - 2

        var mov = await db.MovimentacoesEstoque.IgnoreQueryFilters().FirstAsync();
        Assert.Equal(TipoMovimentacao.Saida, mov.Tipo);
        Assert.Equal(OrigemMovimentacao.Venda, mov.Origem);
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

        var lancamento = await db.Lancamentos.IgnoreQueryFilters().FirstAsync();
        Assert.Equal(TipoLancamento.Receita, lancamento.Tipo);
        Assert.Equal(50m, lancamento.Valor);
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
        Assert.Equal(7m, (await db.Produtos.FindAsync(produto.Id))!.EstoqueAtual);

        await service.CancelarAsync(venda.Id, default);

        var produtoAtualizado = await db.Produtos.FindAsync(produto.Id);
        Assert.Equal(10m, produtoAtualizado!.EstoqueAtual); // estorno

        var vendaAtualizada = await db.Vendas.FindAsync(venda.Id);
        Assert.Equal(StatusVenda.Cancelada, vendaAtualizada!.Status);

        var lancamento = await db.Lancamentos.IgnoreQueryFilters().FirstAsync();
        Assert.Equal(StatusLancamento.Cancelado, lancamento.Status);
    }
}
