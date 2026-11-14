using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Clientes;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Clientes;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ClienteServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ClienteService service) Setup()
    {
        var tenantContext = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new ClienteService(db, tenantContext));
    }

    [Fact]
    public async Task CreateAsync_PersistsCliente()
    {
        var (_, service) = Setup();
        var req = new CreateClienteRequest("Maria", "11999990001", null, null);

        var result = await service.CreateAsync(req, default);

        Assert.Equal("Maria", result.Name);
        Assert.Equal("11999990001", result.WhatsApp);
        Assert.Null(result.PersonType);
        Assert.Null(result.CnpjCpf);
    }

    [Fact]
    public async Task CreateAsync_RemoveMascaraDoDocumento()
    {
        var (_, service) = Setup();
        var req = new CreateClienteRequest(
            "Maria", "11999990001", null, null, "PF", "123.456.789-09");

        var result = await service.CreateAsync(req, default);

        Assert.Equal("PF", result.PersonType);
        Assert.Equal("12345678909", result.CnpjCpf);
    }

    [Fact]
    public async Task CreateAsync_ZeraTipoDePessoaQuandoDocumentoVazio()
    {
        var (_, service) = Setup();
        var req = new CreateClienteRequest("Maria", "11999990001", null, null, "PJ", "   ");

        var result = await service.CreateAsync(req, default);

        Assert.Null(result.PersonType);
        Assert.Null(result.CnpjCpf);
    }

    [Fact]
    public async Task UpdateAsync_LimpaDocumentoQuandoRemovido()
    {
        var (db, service) = Setup();
        var cliente = new Customer
        {
            CompanyId = _empresaId,
            Name = "Maria",
            WhatsApp = "11999990001",
            PersonType = "PF",
            CnpjCpf = "12345678909",
        };
        db.Customers.Add(cliente);
        await db.SaveChangesAsync();

        var result = await service.UpdateAsync(
            cliente.Id, new UpdateClienteRequest("Maria", "11999990001", null, null), default);

        Assert.Null(result.PersonType);
        Assert.Null(result.CnpjCpf);
    }

    [Fact]
    public async Task ListAsync_FiltraPorDocumento()
    {
        var (db, service) = Setup();
        db.Customers.AddRange(
            new Customer
            {
                CompanyId = _empresaId, Name = "Ana", WhatsApp = "11999990001",
                PersonType = "PF", CnpjCpf = "12345678909",
            },
            new Customer { CompanyId = _empresaId, Name = "Carlos", WhatsApp = "11999990002" });
        await db.SaveChangesAsync();

        var result = await service.ListAsync("12345678909", default);

        Assert.Single(result);
        Assert.Equal("Ana", result[0].Name);
    }

    [Fact]
    public async Task GetAsync_ThrowsWhenNotFound()
    {
        var (_, service) = Setup();

        await Assert.ThrowsAsync<AppException>(() => service.GetAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task ListAsync_FiltersByBusca()
    {
        var (db, service) = Setup();
        db.Customers.AddRange(
            new Customer { CompanyId = _empresaId, Name = "Ana Silva", WhatsApp = "11999990001" },
            new Customer { CompanyId = _empresaId, Name = "Carlos", WhatsApp = "11999990002" });
        await db.SaveChangesAsync();

        var result = await service.ListAsync("Ana", default);

        Assert.Single(result);
        Assert.Equal("Ana Silva", result[0].Name);
    }
}
