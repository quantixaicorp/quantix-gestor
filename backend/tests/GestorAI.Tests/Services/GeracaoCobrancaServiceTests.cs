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
        var tc = new TenantContext { EmpresaId = _empresaId };
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
    }

    private async Task<Contrato> CriarContratoAtivoAsync(AppDbContext db, int diaVencimento = 10)
    {
        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Carlos", Whatsapp = "11977770000" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var contrato = new Contrato
        {
            EmpresaId = _empresaId,
            ClienteId = cliente.Id,
            Titulo = "Serviço Mensal",
            Objeto = "Prestação de serviços",
            Status = ContratoStatus.Ativo,
            Valor = 300m,
            DiaVencimento = diaVencimento,
            DataInicio = new DateOnly(2026, 1, 1),
            TipoCobranca = TipoCobranca.Recorrente,
            Periodicidade = Periodicidade.Mensal,
        };
        db.Contratos.Add(contrato);
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

        var cobranca = db.Cobrancas.IgnoreQueryFilters().FirstOrDefault();
        Assert.NotNull(cobranca);
        Assert.Equal(new DateOnly(2026, 7, 10), cobranca.DataVencimento);
        Assert.Equal("Mensalidade 07/2026", cobranca.Referencia);
        Assert.Equal(300m, cobranca.Valor);
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoRoda_ForaDoDia1()
    {
        var db = CreateDb();
        var dia2 = new DateOnly(2026, 7, 2);
        await CriarContratoAtivoAsync(db);

        var svc = new GeracaoCobrancaService(db);
        await svc.ProcessarTodosTenantsAsync(default, dia2);

        Assert.Empty(db.Cobrancas.IgnoreQueryFilters().ToList());
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoCria_QuandoJaExisteCobrancaNoMes()
    {
        var db = CreateDb();
        var dia1 = new DateOnly(2026, 7, 1);
        var contrato = await CriarContratoAtivoAsync(db, diaVencimento: 10);

        db.Cobrancas.Add(new Cobranca
        {
            EmpresaId = _empresaId,
            ClienteId = contrato.ClienteId,
            ContratoId = contrato.Id,
            Referencia = "Mensalidade 07/2026",
            Valor = 300m,
            DataVencimento = new DateOnly(2026, 7, 10),
            Status = CobrancaStatus.Pendente,
        });
        await db.SaveChangesAsync();

        var svc = new GeracaoCobrancaService(db);
        await svc.ProcessarTodosTenantsAsync(default, dia1);

        Assert.Single(db.Cobrancas.IgnoreQueryFilters().ToList());
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

        Assert.Empty(db.Cobrancas.IgnoreQueryFilters().ToList());
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_AjustaVencimento_ParaFevereiro()
    {
        var db = CreateDb();
        var dia1Fev = new DateOnly(2026, 2, 1);
        await CriarContratoAtivoAsync(db, diaVencimento: 31);

        var svc = new GeracaoCobrancaService(db);
        await svc.ProcessarTodosTenantsAsync(default, dia1Fev);

        var cobranca = db.Cobrancas.IgnoreQueryFilters().FirstOrDefault();
        Assert.NotNull(cobranca);
        Assert.Equal(new DateOnly(2026, 2, 28), cobranca.DataVencimento);
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_GravaAutomacaoLog_CobrancaGerada()
    {
        var db = CreateDb();
        var dia1 = new DateOnly(2026, 7, 1);
        await CriarContratoAtivoAsync(db);

        var svc = new GeracaoCobrancaService(db);
        await svc.ProcessarTodosTenantsAsync(default, dia1);

        var log = db.AutomacaoLogs.IgnoreQueryFilters().FirstOrDefault();
        Assert.NotNull(log);
        Assert.Equal(AutomacaoTipoEvento.CobrancaGerada, log.TipoEvento);
        Assert.True(log.Sucesso);
    }
}
