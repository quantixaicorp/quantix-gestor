using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Agendamentos;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class AgendamentoCancelamentoTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private AppDbContext CreateDb(int? horasLimite = null)
    {
        var tc = new TenantContext { CompanyId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        db.CompanySettings.Add(new CompanySettings
        {
            CompanyId = _empresaId,
            CancellationLimitHours = horasLimite,
        });
        db.SaveChanges();
        return db;
    }

    private async Task<Appointment> CriarAgendamentoAsync(AppDbContext db, DateTime inicio)
    {
        var profissional = new Professional { CompanyId = _empresaId, Name = "Dr. Carlos" };
        db.Professionals.Add(profissional);
        var servico = new Product
        {
            CompanyId = _empresaId, Name = "Consulta",
            Type = TipoProduto.Servico, SalePrice = 100m, DurationMinutes = 30,
        };
        db.Products.Add(servico);
        await db.SaveChangesAsync();
        var agendamento = new Appointment
        {
            CompanyId = _empresaId, ProfessionalId = profissional.Id,
            ServiceId = servico.Id, CustomerName = "Maria",
            CustomerPhone = "11999990000",
            StartAt = inicio, EndAt = inicio.AddMinutes(30),
            Status = AgendamentoStatus.Agendado,
        };
        db.Appointments.Add(agendamento);
        await db.SaveChangesAsync();
        return agendamento;
    }

    [Fact]
    public async Task CancelarPublicoAsync_Bloqueia_DentroJanelaCancelamento()
    {
        var db = CreateDb(horasLimite: 24);
        var inicio = DateTime.UtcNow.AddHours(2);
        var ag = await CriarAgendamentoAsync(db, inicio);
        var svc = new AgendamentoService(db, new TenantContext { CompanyId = _empresaId });
        await Assert.ThrowsAsync<AppException>(() => svc.CancelarPublicoAsync(ag.Id, default));
    }

    [Fact]
    public async Task CancelarPublicoAsync_Permite_ForaDaJanelaCancelamento()
    {
        var db = CreateDb(horasLimite: 24);
        var inicio = DateTime.UtcNow.AddHours(48);
        var ag = await CriarAgendamentoAsync(db, inicio);
        var svc = new AgendamentoService(db, new TenantContext { CompanyId = _empresaId });
        var result = await svc.CancelarPublicoAsync(ag.Id, default);
        Assert.Equal(AgendamentoStatus.Cancelado, result.Status);
    }

    [Fact]
    public async Task CancelarPublicoAsync_Permite_SemPoliticaConfigurada()
    {
        var db = CreateDb(horasLimite: null);
        var inicio = DateTime.UtcNow.AddHours(1);
        var ag = await CriarAgendamentoAsync(db, inicio);
        var svc = new AgendamentoService(db, new TenantContext { CompanyId = _empresaId });
        var result = await svc.CancelarPublicoAsync(ag.Id, default);
        Assert.Equal(AgendamentoStatus.Cancelado, result.Status);
    }
}
