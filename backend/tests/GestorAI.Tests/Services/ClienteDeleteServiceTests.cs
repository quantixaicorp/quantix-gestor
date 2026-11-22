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
        db.Customers.Add(cliente);
        await db.SaveChangesAsync();

        await svc.DeleteAsync(cliente.Id, default);

        var encontrado = await db.Customers.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == cliente.Id);
        Assert.Null(encontrado);
    }

    [Fact]
    public async Task DeleteAsync_DesvinculaAgendamentos_EmVezDeFalhar()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "Ana", WhatsApp = "11999990000" };
        db.Customers.Add(cliente);
        var profissional = new Professional { CompanyId = _empresaId, Name = "Dr. Carlos" };
        db.Professionals.Add(profissional);
        var servico = new Product
        {
            CompanyId = _empresaId, Name = "Consulta",
            Type = TipoProduto.Servico, DurationMinutes = 30,
        };
        db.Products.Add(servico);
        await db.SaveChangesAsync();

        var agendamento = new Appointment
        {
            CompanyId = _empresaId,
            ProfessionalId = profissional.Id,
            ServiceId = servico.Id,
            CustomerId = cliente.Id,
            CustomerName = "Ana",
            CustomerPhone = "11999990000",
            StartAt = new DateTime(2027, 6, 10, 9, 0, 0, DateTimeKind.Unspecified),
            EndAt = new DateTime(2027, 6, 10, 9, 30, 0, DateTimeKind.Unspecified),
        };
        db.Appointments.Add(agendamento);
        await db.SaveChangesAsync();

        // Numa requisição real o agendamento não está no change tracker, então o
        // ClientSetNull do EF não entra em ação — é o cenário que quebrou em produção.
        db.ChangeTracker.Clear();

        await svc.DeleteAsync(cliente.Id, default);

        Assert.Null(await db.Customers.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == cliente.Id));

        // O agendamento sobrevive: nome e telefone ficam na própria linha.
        var sobrevivente = await db.Appointments.IgnoreQueryFilters()
            .FirstAsync(a => a.Id == agendamento.Id);
        Assert.Null(sobrevivente.CustomerId);
        Assert.Equal("Ana", sobrevivente.CustomerName);
        Assert.Equal("11999990000", sobrevivente.CustomerPhone);
    }

    [Fact]
    public async Task DeleteAsync_LancaExcecao_QuandoTemAssinatura()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "Bob", WhatsApp = "11888880001" };
        db.Customers.Add(cliente);
        await db.SaveChangesAsync();

        db.CustomerSubscriptions.Add(new CustomerSubscription
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            SubscriptionPlanId = Guid.NewGuid(),
            ContractId = Guid.NewGuid(),
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.DeleteAsync(cliente.Id, default));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_LancaExcecao_QuandoTemVenda()
    {
        var (db, svc) = Setup();
        var cliente = new Customer { CompanyId = _empresaId, Name = "Bob", WhatsApp = "11888880000" };
        db.Customers.Add(cliente);
        db.Sales.Add(new Sale
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Status = StatusVenda.Concluida,
            PaymentMethod = FormaPagamento.Pix,
        });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.DeleteAsync(cliente.Id, default));
    }
}
