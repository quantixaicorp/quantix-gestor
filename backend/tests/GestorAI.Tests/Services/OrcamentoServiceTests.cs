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
        var tenantContext = new TenantContext { CompanyId = _empresaId };
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
        Assert.Equal(1, result.Number);
        Assert.Equal(50m, result.Total);
    }

    [Fact]
    public async Task ListAsync_ExpiraOrcamentosVencidos()
    {
        var (db, service) = Setup();
        var cat = new Category { CompanyId = _empresaId, Name = "Cat" };
        db.Categories.Add(cat);
        db.Quotes.Add(new Quote
        {
            CompanyId = _empresaId,
            Number = 1,
            Title = "Vencido",
            ExpirationDate = DateTime.UtcNow.AddDays(-1),
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
        var o = new Quote
        {
            CompanyId = _empresaId, Number = 1, Title = "T",
            ExpirationDate = DateTime.Today.AddDays(7), Status = OrcamentoStatus.Rascunho
        };
        db.Quotes.Add(o);
        await db.SaveChangesAsync();

        var result = await service.EnviarAsync(o.Id, default);

        Assert.Equal("Enviado", result.Status);
    }

    [Fact]
    public async Task ConvertAsync_CriaVendaApenasComItensProduto()
    {
        var (db, service) = Setup();
        var cat = new Category { CompanyId = _empresaId, Name = "Cat" };
        db.Categories.Add(cat);
        var produto = new Product
        {
            CompanyId = _empresaId, CategoryId = cat.Id,
            Name = "Shampoo", SalePrice = 30m, CurrentStock = 10
        };
        db.Products.Add(produto);
        var o = new Quote
        {
            CompanyId = _empresaId, Number = 1, Title = "T",
            ExpirationDate = DateTime.Today.AddDays(7), Status = OrcamentoStatus.Aprovado
        };
        o.Items.Add(new QuoteItem
        {
            Type = OrcamentoItemTipo.Produto, ProductId = produto.Id,
            Description = "Shampoo", Quantity = 2, UnitPrice = 30m
        });
        o.Items.Add(new QuoteItem
        {
            Type = OrcamentoItemTipo.Livre, Description = "Aplicação",
            Quantity = 1, UnitPrice = 50m
        });
        db.Quotes.Add(o);
        await db.SaveChangesAsync();

        var result = await service.ConvertAsync(o.Id, default);

        Assert.Equal("Convertido", result.Status);
        Assert.NotNull(result.SaleId);
        var venda = await db.Sales.Include(v => v.Items).FirstAsync();
        Assert.Single(venda.Items);
        Assert.Equal(produto.Id, venda.Items.First().ProductId);
    }

    [Fact]
    public async Task ConvertAsync_QuandoNaoAprovado_LancaExcecao()
    {
        var (db, service) = Setup();
        var o = new Quote
        {
            CompanyId = _empresaId, Number = 1, Title = "T",
            ExpirationDate = DateTime.Today.AddDays(7), Status = OrcamentoStatus.Enviado
        };
        db.Quotes.Add(o);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => service.ConvertAsync(o.Id, default));
    }

    [Fact]
    public async Task AprovarAsync_QuandoExpirado_LancaExcecao()
    {
        var (db, service) = Setup();
        var o = new Quote
        {
            CompanyId = _empresaId, Number = 1, Title = "T",
            ExpirationDate = DateTime.Today.AddDays(-1), Status = OrcamentoStatus.Enviado
        };
        db.Quotes.Add(o);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => service.AprovarAsync(o.Id, default));
    }

    [Fact]
    public async Task EnviarAsync_GeraTokenPublico()
    {
        var (_, service) = Setup();
        var req = new CreateOrcamentoRequest(
            null, "Orçamento Teste", DateTime.Today.AddDays(7), null,
            [new OrcamentoItemRequest("Livre", null, "Corte", 1, 50m)]);
        var created = await service.CreateAsync(req, default);

        var result = await service.EnviarAsync(created.Id, default);

        Assert.NotNull(result.PublicToken);
        Assert.NotEqual(Guid.Empty, result.PublicToken);
    }

    [Fact]
    public async Task GetPublicoAsync_RetornaOrcamentoPorToken()
    {
        var (db, service) = Setup();
        var token = Guid.NewGuid();
        db.Quotes.Add(new Quote
        {
            CompanyId = _empresaId, Number = 1, Title = "Teste Público",
            ExpirationDate = DateTime.UtcNow.AddDays(7),
            Status = OrcamentoStatus.Enviado,
            PublicToken = token,
        });
        await db.SaveChangesAsync();

        var result = await service.GetPublicoAsync(token, default);

        Assert.Equal("Teste Público", result.Title);
        Assert.Equal("Enviado", result.Status);
    }

    [Fact]
    public async Task GetPublicoAsync_Lanca404_QuandoTokenInvalido()
    {
        var (_, service) = Setup();

        await Assert.ThrowsAsync<AppException>(
            () => service.GetPublicoAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task AprovarPublicoAsync_AlteraStatusParaAprovado()
    {
        var (db, service) = Setup();
        var token = Guid.NewGuid();
        db.Quotes.Add(new Quote
        {
            CompanyId = _empresaId, Number = 1, Title = "T",
            ExpirationDate = DateTime.UtcNow.AddDays(7),
            Status = OrcamentoStatus.Enviado,
            PublicToken = token,
        });
        await db.SaveChangesAsync();

        var result = await service.AprovarPublicoAsync(token, default);

        Assert.Equal("Aprovado", result.Status);
    }

    [Fact]
    public async Task RejeitarPublicoAsync_AlteraStatusParaRejeitado()
    {
        var (db, service) = Setup();
        var token = Guid.NewGuid();
        db.Quotes.Add(new Quote
        {
            CompanyId = _empresaId, Number = 1, Title = "T",
            ExpirationDate = DateTime.UtcNow.AddDays(7),
            Status = OrcamentoStatus.Enviado,
            PublicToken = token,
        });
        await db.SaveChangesAsync();

        var result = await service.RejeitarPublicoAsync(token, default);

        Assert.Equal("Rejeitado", result.Status);
    }

    [Fact]
    public async Task AprovarPublicoAsync_Lanca400_QuandoExpirado()
    {
        var (db, service) = Setup();
        var token = Guid.NewGuid();
        db.Quotes.Add(new Quote
        {
            CompanyId = _empresaId, Number = 1, Title = "T",
            ExpirationDate = DateTime.UtcNow.AddDays(-1),
            Status = OrcamentoStatus.Enviado,
            PublicToken = token,
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(
            () => service.AprovarPublicoAsync(token, default));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task AprovarPublicoAsync_Lanca400_QuandoStatusNaoEhEnviado()
    {
        var (db, service) = Setup();
        var token = Guid.NewGuid();
        db.Quotes.Add(new Quote
        {
            CompanyId = _empresaId, Number = 1, Title = "T",
            ExpirationDate = DateTime.UtcNow.AddDays(7),
            Status = OrcamentoStatus.Aprovado,
            PublicToken = token,
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(
            () => service.AprovarPublicoAsync(token, default));
    }

    [Fact]
    public async Task RejeitarPublicoAsync_Lanca400_QuandoStatusNaoEhEnviado()
    {
        var (db, service) = Setup();
        var token = Guid.NewGuid();
        db.Quotes.Add(new Quote
        {
            CompanyId = _empresaId, Number = 1, Title = "T",
            ExpirationDate = DateTime.UtcNow.AddDays(7),
            Status = OrcamentoStatus.Rejeitado,
            PublicToken = token,
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(
            () => service.RejeitarPublicoAsync(token, default));
    }
}
