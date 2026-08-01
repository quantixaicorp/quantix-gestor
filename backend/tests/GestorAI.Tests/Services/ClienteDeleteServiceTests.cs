using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Clientes;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ClienteDeleteServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ClienteService service) Setup()
    {
        var tc = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new ClienteService(db, tc));
    }

    [Fact]
    public async Task DeleteAsync_RemoveCliente_QuandoSemVinculos()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "Ana", WhatsApp = "11999990000" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        await svc.DeleteAsync(cliente.Id, default);

        var encontrado = await db.Clientes.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == cliente.Id);
        Assert.Null(encontrado);
    }

    [Fact]
    public async Task DeleteAsync_LancaExcecao_QuandoTemVenda()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "Bob", WhatsApp = "11888880000" };
        db.Clientes.Add(cliente);
        db.Vendas.Add(new Sale
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Status = StatusVenda.Concluida,
            FormaPagamento = FormaPagamento.Pix,
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.DeleteAsync(cliente.Id, default));
    }
}
