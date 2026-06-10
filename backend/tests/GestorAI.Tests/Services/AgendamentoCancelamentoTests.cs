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
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        db.ConfiguracoesEmpresa.Add(new ConfiguracaoEmpresa
        {
            EmpresaId = _empresaId,
            HorasLimiteCancelamento = horasLimite,
        });
        db.SaveChanges();
        return db;
    }

    private async Task<Agendamento> CriarAgendamentoAsync(AppDbContext db, DateTime inicio)
    {
        var profissional = new Profissional { EmpresaId = _empresaId, Nome = "Dr. Carlos" };
        db.Profissionais.Add(profissional);
        var servico = new Produto
        {
            EmpresaId = _empresaId, Nome = "Consulta",
            Tipo = TipoProduto.Servico, PrecoVenda = 100m, DuracaoMinutos = 30,
        };
        db.Produtos.Add(servico);
        await db.SaveChangesAsync();
        var agendamento = new Agendamento
        {
            EmpresaId = _empresaId, ProfissionalId = profissional.Id,
            ServicoId = servico.Id, ClienteNome = "Maria",
            ClienteTelefone = "11999990000",
            DataHoraInicio = inicio, DataHoraFim = inicio.AddMinutes(30),
            Status = AgendamentoStatus.Agendado,
        };
        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync();
        return agendamento;
    }

    [Fact]
    public async Task CancelarPublicoAsync_Bloqueia_DentroJanelaCancelamento()
    {
        var db = CreateDb(horasLimite: 24);
        var inicio = DateTime.UtcNow.AddHours(2);
        var ag = await CriarAgendamentoAsync(db, inicio);
        var svc = new AgendamentoService(db, new TenantContext { EmpresaId = _empresaId });
        await Assert.ThrowsAsync<AppException>(() => svc.CancelarPublicoAsync(ag.Id, default));
    }

    [Fact]
    public async Task CancelarPublicoAsync_Permite_ForaDaJanelaCancelamento()
    {
        var db = CreateDb(horasLimite: 24);
        var inicio = DateTime.UtcNow.AddHours(48);
        var ag = await CriarAgendamentoAsync(db, inicio);
        var svc = new AgendamentoService(db, new TenantContext { EmpresaId = _empresaId });
        var result = await svc.CancelarPublicoAsync(ag.Id, default);
        Assert.Equal(AgendamentoStatus.Cancelado, result.Status);
    }

    [Fact]
    public async Task CancelarPublicoAsync_Permite_SemPoliticaConfigurada()
    {
        var db = CreateDb(horasLimite: null);
        var inicio = DateTime.UtcNow.AddHours(1);
        var ag = await CriarAgendamentoAsync(db, inicio);
        var svc = new AgendamentoService(db, new TenantContext { EmpresaId = _empresaId });
        var result = await svc.CancelarPublicoAsync(ag.Id, default);
        Assert.Equal(AgendamentoStatus.Cancelado, result.Status);
    }
}
