using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Automacao;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class GeracaoCobrancaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private AppDbContext CreateDb()
    {
        var tc = new TenantContext { CompanyId = _empresaId };
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
    }

    private async Task<Contract> CriarContratoAtivoAsync(AppDbContext db, int diaVencimento = 10)
    {
        var cliente = new Customer { CompanyId = _empresaId, Name = "Carlos", WhatsApp = "11977770000" };
        db.Customers.Add(cliente);
        await db.SaveChangesAsync();

        var contrato = new Contract
        {
            CompanyId = _empresaId,
            CustomerId = cliente.Id,
            Title = "Serviço Mensal",
            Subject = "Prestação de serviços",
            Status = ContratoStatus.Ativo,
            Amount = 300m,
            DueDay = diaVencimento,
            StartDate = new DateOnly(2026, 1, 1),
            ChargeType = TipoCobranca.Recorrente,
            Frequency = Periodicidade.Mensal,
        };
        db.Contracts.Add(contrato);
        await db.SaveChangesAsync();
        return contrato;
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_CriaCobranca_NoDia1()
    {
        var db = CreateDb();
        var dia1 = new DateOnly(2026, 7, 1);
        await CriarContratoAtivoAsync(db, diaVencimento: 10);

        var svc = new GeracaoCobrancaService(db);
        await svc.ProcessarTodosTenantsAsync(default, dia1);

        var cobranca = db.Charges.IgnoreQueryFilters().FirstOrDefault();
        Assert.NotNull(cobranca);
        Assert.Equal(new DateOnly(2026, 7, 10), cobranca.DueDate);
        Assert.Equal("Mensalidade 07/2026", cobranca.Reference);
        Assert.Equal(300m, cobranca.Amount);
        Assert.Equal(_empresaId, cobranca.CompanyId);
        Assert.Equal(CobrancaStatus.Pendente, cobranca.Status);
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoRoda_ForaDoDia1()
    {
        var db = CreateDb();
        var dia2 = new DateOnly(2026, 7, 2);
        await CriarContratoAtivoAsync(db);

        var svc = new GeracaoCobrancaService(db);
        await svc.ProcessarTodosTenantsAsync(default, dia2);

        Assert.Empty(db.Charges.IgnoreQueryFilters().ToList());
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoCria_QuandoJaExisteCobrancaNoMes()
    {
        var db = CreateDb();
        var dia1 = new DateOnly(2026, 7, 1);
        var contrato = await CriarContratoAtivoAsync(db, diaVencimento: 10);

        db.Charges.Add(new Charge
        {
            CompanyId = _empresaId,
            CustomerId = contrato.CustomerId,
            ContractId = contrato.Id,
            Reference = "Mensalidade 07/2026",
            Amount = 300m,
            DueDate = new DateOnly(2026, 7, 10),
            Status = CobrancaStatus.Pendente,
        });
        await db.SaveChangesAsync();

        var svc = new GeracaoCobrancaService(db);
        await svc.ProcessarTodosTenantsAsync(default, dia1);

        Assert.Single(db.Charges.IgnoreQueryFilters().ToList());
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoCria_ContratoInativo()
    {
        var db = CreateDb();
        var dia1 = new DateOnly(2026, 7, 1);
        var contrato = await CriarContratoAtivoAsync(db);
        contrato.Status = ContratoStatus.Encerrado;
        await db.SaveChangesAsync();

        var svc = new GeracaoCobrancaService(db);
        await svc.ProcessarTodosTenantsAsync(default, dia1);

        Assert.Empty(db.Charges.IgnoreQueryFilters().ToList());
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_AjustaVencimento_ParaFevereiro()
    {
        var db = CreateDb();
        var dia1Fev = new DateOnly(2026, 2, 1);
        await CriarContratoAtivoAsync(db, diaVencimento: 31);

        var svc = new GeracaoCobrancaService(db);
        await svc.ProcessarTodosTenantsAsync(default, dia1Fev);

        var cobranca = db.Charges.IgnoreQueryFilters().FirstOrDefault();
        Assert.NotNull(cobranca);
        Assert.Equal(new DateOnly(2026, 2, 28), cobranca.DueDate);
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_GravaAutomacaoLog_CobrancaGerada()
    {
        var db = CreateDb();
        var dia1 = new DateOnly(2026, 7, 1);
        await CriarContratoAtivoAsync(db);

        var svc = new GeracaoCobrancaService(db);
        await svc.ProcessarTodosTenantsAsync(default, dia1);

        var log = db.AutomationLogs.IgnoreQueryFilters().FirstOrDefault();
        Assert.NotNull(log);
        Assert.Equal(AutomacaoTipoEvento.CobrancaGerada, log.EventType);
        Assert.True(log.Success);
    }
}
