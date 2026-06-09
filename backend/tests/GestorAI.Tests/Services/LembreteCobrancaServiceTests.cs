using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Automacao;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

// ---------------------------------------------------------------------------
// Fake for IEvolutionApiService
// ---------------------------------------------------------------------------
internal class FakeEvolutionApiService : IEvolutionApiService
{
    public bool ShouldFail { get; set; }
    public bool ShouldThrow { get; set; }
    public List<string> EnviadosPara { get; } = [];

    public Task<bool> EnviarMensagemAsync(
        string apiUrl, string apiKey, string instance,
        string whatsapp, string mensagem, CancellationToken ct = default)
    {
        if (ShouldThrow)
            throw new HttpRequestException("Timeout simulado");

        EnviadosPara.Add(whatsapp);
        return Task.FromResult(!ShouldFail);
    }

    public Task<bool> TestarConexaoAsync(string apiUrl, string apiKey, CancellationToken ct)
        => Task.FromResult(true);
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------
public class LembreteCobrancaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private AppDbContext CreateDb()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
    }

    private static ConfiguracaoEmpresa ConfigComEvolution(Guid empresaId) => new()
    {
        EmpresaId        = empresaId,
        EvolutionApiUrl  = "https://evo.test",
        EvolutionApiKey  = "key",
        EvolutionInstance = "inst",
        LembreteNoDia    = true,
    };

    private async Task<(Cobranca cobranca, Cliente cliente)> CriarCobrancaAsync(
        AppDbContext db, Guid empresaId, DateOnly vencimento)
    {
        var cliente = new Cliente { EmpresaId = empresaId, Nome = "João", Whatsapp = "11999990000" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        var cobranca = new Cobranca
        {
            EmpresaId      = empresaId,
            ClienteId      = cliente.Id,
            Referencia     = "Mensalidade",
            Valor          = 200m,
            DataVencimento = vencimento,
            Status         = CobrancaStatus.Pendente,
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();

        return (cobranca, cliente);
    }

    // ------------------------------------------------------------------
    // 1. Tenant sem config Evolution → não envia nada
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_SemConfigEvolution_NaoEnviaNada()
    {
        var db = CreateDb();
        // ConfiguracaoEmpresa sem campos Evolution
        db.ConfiguracoesEmpresa.Add(new ConfiguracaoEmpresa
        {
            EmpresaId = _empresaId,
            LembreteNoDia = true,
        });
        var hoje = new DateOnly(2026, 6, 10);
        await CriarCobrancaAsync(db, _empresaId, hoje);

        var fake = new FakeEvolutionApiService();
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Empty(fake.EnviadosPara);
    }

    // ------------------------------------------------------------------
    // 2. Cobrança no dia → envia e grava log
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_EnviaEGravaLog_QuandoLembreteNoDia()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));
        var (cobranca, cliente) = await CriarCobrancaAsync(db, _empresaId, hoje);

        var fake = new FakeEvolutionApiService();
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Single(fake.EnviadosPara);
        Assert.Equal(cliente.Whatsapp, fake.EnviadosPara[0]);

        var log = db.AutomacaoLogs.IgnoreQueryFilters()
            .FirstOrDefault(l => l.CobrancaId == cobranca.Id
                              && l.TipoEvento == AutomacaoTipoEvento.LembreteNoDia);
        Assert.NotNull(log);
        Assert.True(log.Sucesso);
    }

    // ------------------------------------------------------------------
    // 3. Não reenvia para o mesmo par (cobranca, evento)
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoReenvia_QuandoJaEnviou()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));
        var (cobranca, _) = await CriarCobrancaAsync(db, _empresaId, hoje);

        // Simula log já existente
        db.AutomacaoLogs.Add(new AutomacaoLog
        {
            EmpresaId  = _empresaId,
            CobrancaId = cobranca.Id,
            TipoEvento = AutomacaoTipoEvento.LembreteNoDia,
            Sucesso    = true,
        });
        await db.SaveChangesAsync();

        var fake = new FakeEvolutionApiService();
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Empty(fake.EnviadosPara);
    }

    // ------------------------------------------------------------------
    // 4. Cobrança paga não recebe lembrete
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoEnvia_QuandoCobrancaPaga()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));

        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Maria", Whatsapp = "11988880000" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();

        db.Cobrancas.Add(new Cobranca
        {
            EmpresaId      = _empresaId,
            ClienteId      = cliente.Id,
            Referencia     = "Mens",
            Valor          = 100m,
            DataVencimento = hoje,
            Status         = CobrancaStatus.Pago,
        });
        await db.SaveChangesAsync();

        var fake = new FakeEvolutionApiService();
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Empty(fake.EnviadosPara);
    }

    // ------------------------------------------------------------------
    // 5. Múltiplos offsets habilitados → envia para cada data
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_EnviaParaMultiplosOffsets()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);

        db.ConfiguracoesEmpresa.Add(new ConfiguracaoEmpresa
        {
            EmpresaId        = _empresaId,
            EvolutionApiUrl  = "https://evo.test",
            EvolutionApiKey  = "key",
            EvolutionInstance = "inst",
            LembreteNoDia    = true,
            Lembrete1dAntes  = true,
        });
        await db.SaveChangesAsync();

        // Cobrança vencendo hoje
        await CriarCobrancaAsync(db, _empresaId, hoje);
        // Cobrança vencendo amanhã (hoje + 1d → Lembrete1dAntes)
        await CriarCobrancaAsync(db, _empresaId, hoje.AddDays(1));

        var fake = new FakeEvolutionApiService();
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Equal(2, fake.EnviadosPara.Count);
    }

    // ------------------------------------------------------------------
    // 6. Exceção na API → grava log com Sucesso=false e ErroMsg
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_GravaLog_QuandoApiLancaExcecao()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));
        var (cobranca, _) = await CriarCobrancaAsync(db, _empresaId, hoje);

        var fake = new FakeEvolutionApiService { ShouldThrow = true };
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        var log = db.AutomacaoLogs.IgnoreQueryFilters()
            .FirstOrDefault(l => l.CobrancaId == cobranca.Id
                              && l.TipoEvento == AutomacaoTipoEvento.LembreteNoDia);
        Assert.NotNull(log);
        Assert.False(log.Sucesso);
        Assert.NotNull(log.ErroMsg);
    }

    // ------------------------------------------------------------------
    // 7 (Fix 3 – Test A): Envio com retorno false → grava Sucesso=false
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_GravaLog_ComSucessoFalse_QuandoEnvioFalha()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));
        var (cobranca, _) = await CriarCobrancaAsync(db, _empresaId, hoje);

        var fake = new FakeEvolutionApiService { ShouldFail = true };
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        var log = db.AutomacaoLogs.IgnoreQueryFilters()
            .FirstOrDefault(l => l.CobrancaId == cobranca.Id && l.TipoEvento == AutomacaoTipoEvento.LembreteNoDia);
        Assert.NotNull(log);
        Assert.False(log.Sucesso);
    }

    // ------------------------------------------------------------------
    // 8 (Fix 3 – Test B): Cliente sem Whatsapp → não envia, não grava log
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoEnvia_NaoGravaLog_QuandoClienteSemWhatsapp()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.ConfiguracoesEmpresa.Add(ConfigComEvolution(_empresaId));

        var cliente = new Cliente { EmpresaId = _empresaId, Nome = "Sem Tel", Whatsapp = "" };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        var cobranca = new Cobranca
        {
            EmpresaId      = _empresaId,
            ClienteId      = cliente.Id,
            Referencia     = "Mensalidade",
            Valor          = 100m,
            DataVencimento = hoje,
            Status         = CobrancaStatus.Pendente,
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();

        var fake = new FakeEvolutionApiService();
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Empty(fake.EnviadosPara);
        Assert.Empty(db.AutomacaoLogs.IgnoreQueryFilters().ToList());
    }
}
