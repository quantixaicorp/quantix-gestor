using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Automacao;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class LembreteCobrancaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private AppDbContext CreateDb()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
    }

    private ConfiguracaoEmpresa ConfigComEvolution(Guid empresaId) => new()
    {
        EmpresaId = empresaId,
        EvolutionApiUrl = "http://evo.test",
        EvolutionApiKey = "key123",
        EvolutionInstance = "inst1",
        Lembrete3dAntes = true,
        Lembrete1dAntes = true,
        LembreteNoDia = true,
        Lembrete1dDepois = true,
        Lembrete3dDepois = false,
        Lembrete7dDepois = false,
    };

    private async Task<(Cobranca cobranca, Cliente cliente)> CriarCobrancaAsync(
        AppDbContext db, Guid empresaId, DateOnly vencimento)
    {
        var cliente = new Cliente
        {
            EmpresaId = empresaId,
            Nome = "Ana",
            Whatsapp = "11988880000",
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var cobranca = new Cobranca
        {
            EmpresaId = empresaId,
            ClienteId = cliente.Id,
            Referencia = "Mensalidade 06/2026",
            Valor = 150m,
            DataVencimento = vencimento,
            Status = CobrancaStatus.Pendente,
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();
        return (cobranca, cliente);
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_PulaSeEvolutionNaoConfigurado()
    {
        var db = CreateDb();
        db.ConfiguracoesEmpresa.Add(new ConfiguracaoEmpresa
        {
            EmpresaId = _empresaId,
            EvolutionApiUrl = null,
        });
        var hoje = new DateOnly(2026, 6, 10);
        await CriarCobrancaAsync(db, _empresaId, hoje);

        var fake = new FakeEvolutionApiService();
        var svc = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Empty(fake.EnviadosPara);
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_EnviaLembrete_NoDiaDoVencimento()
    {
        var db = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));
        await CriarCobrancaAsync(db, _empresaId, hoje);

        var fake = new FakeEvolutionApiService();
        var svc = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Single(fake.EnviadosPara);
        Assert.Contains("11988880000", fake.EnviadosPara[0].Phone);
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_PulaDeduplicacao_QuandoLogJaExiste()
    {
        var db = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));
        var (cobranca, _) = await CriarCobrancaAsync(db, _empresaId, hoje);

        db.AutomacaoLogs.Add(new AutomacaoLog
        {
            EmpresaId = _empresaId,
            CobrancaId = cobranca.Id,
            TipoEvento = AutomacaoTipoEvento.LembreteNoDia,
            Sucesso = true,
        });
        await db.SaveChangesAsync();

        var fake = new FakeEvolutionApiService();
        var svc = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Empty(fake.EnviadosPara);
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_GravaLog_AposEnvioComSucesso()
    {
        var db = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));
        var (cobranca, _) = await CriarCobrancaAsync(db, _empresaId, hoje);

        var fake = new FakeEvolutionApiService();
        var svc = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        var log = db.AutomacaoLogs.IgnoreQueryFilters()
            .FirstOrDefault(l => l.CobrancaId == cobranca.Id && l.TipoEvento == AutomacaoTipoEvento.LembreteNoDia);
        Assert.NotNull(log);
        Assert.True(log.Sucesso);
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoEnvia_QuandoCobrancaJaPaga()
    {
        var db = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));
        var (cobranca, _) = await CriarCobrancaAsync(db, _empresaId, hoje);
        cobranca.Status = CobrancaStatus.Pago;
        await db.SaveChangesAsync();

        var fake = new FakeEvolutionApiService();
        var svc = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Empty(fake.EnviadosPara);
    }

    [Fact]
    public async Task ProcessarTodosTenantsAsync_Envia3dAntes_QuandoVenceEmTresDias()
    {
        var db = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        var vencimento = hoje.AddDays(3);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));
        await CriarCobrancaAsync(db, _empresaId, vencimento);

        var fake = new FakeEvolutionApiService();
        var svc = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Single(fake.EnviadosPara);
    }
}

public class FakeEvolutionApiService : IEvolutionApiService
{
    public List<(string Phone, string Text)> EnviadosPara { get; } = [];
    public bool ShouldFail { get; set; }

    public Task<bool> EnviarMensagemAsync(
        string apiUrl, string apiKey, string instance,
        string phone, string text, CancellationToken ct)
    {
        if (!ShouldFail) EnviadosPara.Add((phone, text));
        return Task.FromResult(!ShouldFail);
    }

    public Task<bool> TestarConexaoAsync(string apiUrl, string apiKey, CancellationToken ct)
        => Task.FromResult(true);
}
