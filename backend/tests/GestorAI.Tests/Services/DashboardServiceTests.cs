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
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
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

        db.Vendas.AddRange(
            new Venda
            {
                EmpresaId = _empresaId, DataHora = hoje,
                Status = StatusVenda.Concluida, Total = 300m,
                Subtotal = 300m, Desconto = 0m,
                FormaPagamento = FormaPagamento.Pix
            },
            new Venda
            {
                EmpresaId = _empresaId, DataHora = ontem,
                Status = StatusVenda.Concluida, Total = 500m,
                Subtotal = 500m, Desconto = 0m,
                FormaPagamento = FormaPagamento.Dinheiro
            },
            new Venda
            {
                EmpresaId = _empresaId, DataHora = hoje,
                Status = StatusVenda.Cancelada, Total = 100m,
                Subtotal = 100m, Desconto = 0m,
                FormaPagamento = FormaPagamento.Pix
            });
        await db.SaveChangesAsync();

        var result = await service.GetDashboardAsync(default);

        Assert.Equal(300m, result.Kpis.TotalVendidoHoje);
    }

    [Fact]
    public async Task GetKpisAsync_ProdutosEstoqueBaixo_ContaProdutosAbaixoMinimo()
    {
        var (db, service) = Setup();
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Cat" };
        db.Categorias.Add(cat);
        db.Produtos.AddRange(
            new Produto
            {
                EmpresaId = _empresaId, CategoriaId = cat.Id,
                Nome = "Baixo", PrecoVenda = 10m,
                EstoqueAtual = 1m, EstoqueMinimo = 5m
            },
            new Produto
            {
                EmpresaId = _empresaId, CategoriaId = cat.Id,
                Nome = "OK", PrecoVenda = 10m,
                EstoqueAtual = 10m, EstoqueMinimo = 2m
            });
        await db.SaveChangesAsync();

        var result = await service.GetDashboardAsync(default);

        Assert.Equal(1, result.Kpis.ProdutosEstoqueBaixo);
    }

    [Fact]
    public async Task GetDashboardAsync_TopProdutos_OrdenaPorQuantidadeVendida()
    {
        var (db, service) = Setup();
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Cat" };
        db.Categorias.Add(cat);
        var p1 = new Produto { EmpresaId = _empresaId, CategoriaId = cat.Id, Nome = "A", PrecoVenda = 10m, EstoqueAtual = 0m, EstoqueMinimo = 0m };
        var p2 = new Produto { EmpresaId = _empresaId, CategoriaId = cat.Id, Nome = "B", PrecoVenda = 20m, EstoqueAtual = 0m, EstoqueMinimo = 0m };
        db.Produtos.AddRange(p1, p2);

        var venda = new Venda
        {
            EmpresaId = _empresaId, DataHora = DateTime.UtcNow,
            Status = StatusVenda.Concluida, Subtotal = 50m,
            Desconto = 0m, Total = 50m, FormaPagamento = FormaPagamento.Pix
        };
        db.Vendas.Add(venda);
        await db.SaveChangesAsync();

        db.ItensVenda.AddRange(
            new ItemVenda { VendaId = venda.Id, ProdutoId = p1.Id, Quantidade = 3m, PrecoUnitario = 10m, Desconto = 0m, Total = 30m },
            new ItemVenda { VendaId = venda.Id, ProdutoId = p2.Id, Quantidade = 1m, PrecoUnitario = 20m, Desconto = 0m, Total = 20m });
        await db.SaveChangesAsync();

        var result = await service.GetDashboardAsync(default);

        Assert.Equal("A", result.TopProdutos[0].Nome);
        Assert.Equal(3m, result.TopProdutos[0].QuantidadeVendida);
    }
}
