// backend/tests/GestorAI.Tests/Services/OrcamentoServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Orcamentos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Orcamentos;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class OrcamentoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, OrcamentoService service) Setup()
    {
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new OrcamentoService(db, tenantContext));
    }

    [Fact]
    public async Task CreateAsync_PersisteComotRascunho()
    {
        var (_, service) = Setup();
        var req = new CreateOrcamentoRequest(
            null, "Orçamento Teste", DateTime.Today.AddDays(7), null,
            [new OrcamentoItemRequest("Livre", null, "Corte", 1, 50m)]);

        var result = await service.CreateAsync(req, default);

        Assert.Equal("Rascunho", result.Status);
        Assert.Equal(1, result.Numero);
        Assert.Equal(50m, result.Total);
    }

    [Fact]
    public async Task ListAsync_ExpiraOrcamentosVencidos()
    {
        var (db, service) = Setup();
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Cat" };
        db.Categorias.Add(cat);
        db.Orcamentos.Add(new Orcamento
        {
            EmpresaId = _empresaId,
            Numero = 1,
            Titulo = "Vencido",
            DataValidade = DateTime.UtcNow.AddDays(-1),
            Status = OrcamentoStatus.Enviado,
        });
        await db.SaveChangesAsync();

        var result = await service.ListAsync(null, default);

        Assert.Single(result);
        Assert.Equal("Expirado", result[0].Status);
    }

    [Fact]
    public async Task EnviarAsync_RascunhoViraEnviado()
    {
        var (db, service) = Setup();
        var o = new Orcamento
        {
            EmpresaId = _empresaId, Numero = 1, Titulo = "T",
            DataValidade = DateTime.Today.AddDays(7), Status = OrcamentoStatus.Rascunho
        };
        db.Orcamentos.Add(o);
        await db.SaveChangesAsync();

        var result = await service.EnviarAsync(o.Id, default);

        Assert.Equal("Enviado", result.Status);
    }

    [Fact]
    public async Task ConvertAsync_CriaVendaApenasComItensProduto()
    {
        var (db, service) = Setup();
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Cat" };
        db.Categorias.Add(cat);
        var produto = new Produto
        {
            EmpresaId = _empresaId, CategoriaId = cat.Id,
            Nome = "Shampoo", PrecoVenda = 30m, EstoqueAtual = 10
        };
        db.Produtos.Add(produto);
        var o = new Orcamento
        {
            EmpresaId = _empresaId, Numero = 1, Titulo = "T",
            DataValidade = DateTime.Today.AddDays(7), Status = OrcamentoStatus.Aprovado
        };
        o.Itens.Add(new OrcamentoItem
        {
            Tipo = OrcamentoItemTipo.Produto, ProdutoId = produto.Id,
            Descricao = "Shampoo", Quantidade = 2, ValorUnitario = 30m
        });
        o.Itens.Add(new OrcamentoItem
        {
            Tipo = OrcamentoItemTipo.Livre, Descricao = "Aplicação",
            Quantidade = 1, ValorUnitario = 50m
        });
        db.Orcamentos.Add(o);
        await db.SaveChangesAsync();

        var result = await service.ConvertAsync(o.Id, default);

        Assert.Equal("Convertido", result.Status);
        Assert.NotNull(result.VendaId);
        var venda = await db.Vendas.Include(v => v.Itens).FirstAsync();
        Assert.Single(venda.Itens);
        Assert.Equal(produto.Id, venda.Itens.First().ProdutoId);
    }

    [Fact]
    public async Task ConvertAsync_QuandoNaoAprovado_LancaExcecao()
    {
        var (db, service) = Setup();
        var o = new Orcamento
        {
            EmpresaId = _empresaId, Numero = 1, Titulo = "T",
            DataValidade = DateTime.Today.AddDays(7), Status = OrcamentoStatus.Enviado
        };
        db.Orcamentos.Add(o);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => service.ConvertAsync(o.Id, default));
    }

    [Fact]
    public async Task AprovarAsync_QuandoExpirado_LancaExcecao()
    {
        var (db, service) = Setup();
        var o = new Orcamento
        {
            EmpresaId = _empresaId, Numero = 1, Titulo = "T",
            DataValidade = DateTime.Today.AddDays(-1), Status = OrcamentoStatus.Enviado
        };
        db.Orcamentos.Add(o);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => service.AprovarAsync(o.Id, default));
    }
}
