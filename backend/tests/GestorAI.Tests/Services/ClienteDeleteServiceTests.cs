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
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new ClienteService(db, tc));
    }

    [Fact]
    public async Task DeleteAsync_RemoveCliente_QuandoSemVinculos()
    {
        var (db, svc) = Setup();
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Ana", Whatsapp = "11999990000" };
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
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Bob", Whatsapp = "11888880000" };
        db.Clientes.Add(cliente);
        db.Vendas.Add(new Venda
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            Status = StatusVenda.Concluida,
            FormaPagamento = FormaPagamento.Pix,
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.DeleteAsync(cliente.Id, default));
    }
}
