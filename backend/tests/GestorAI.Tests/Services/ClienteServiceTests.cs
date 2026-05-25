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
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
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

        Assert.Equal("Maria", result.Nome);
        Assert.Equal("11999990001", result.Whatsapp);
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
        db.Clientes.AddRange(
            new Cliente { EmpresaId = _empresaId, Nome = "Ana Silva", Whatsapp = "11999990001" },
            new Cliente { EmpresaId = _empresaId, Nome = "Carlos", Whatsapp = "11999990002" });
        await db.SaveChangesAsync();

        var result = await service.ListAsync("Ana", default);

        Assert.Single(result);
        Assert.Equal("Ana Silva", result[0].Nome);
    }
}
