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
        var tenant = new TenantContext { CompanyId = _empresaId };
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
    }

    private static CompanySettings ConfigComEvolution(Guid empresaId) => new()
    {
        CompanyId        = empresaId,
        EvolutionApiUrl  = "https://evo.test",
        EvolutionApiKey  = "key",
        EvolutionInstance = "inst",
        ReminderOnDueDate    = true,
    };

    private async Task<(Charge cobranca, Customer cliente)> CriarCobrancaAsync(
        AppDbContext db, Guid empresaId, DateOnly vencimento)
    {
        var cliente = new Customer { CompanyId = empresaId, Name = "João", WhatsApp = "11999990000" };
        db.Customers.Add(cliente);
        await db.SaveChangesAsync();

        var cobranca = new Charge
        {
            CompanyId      = empresaId,
            CustomerId      = cliente.Id,
            Reference     = "Mensalidade",
            Amount          = 200m,
            DueDate = vencimento,
            Status         = CobrancaStatus.Pendente,
        };
        db.Charges.Add(cobranca);
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
        // CompanySettings sem campos Evolution
        db.CompanySettings.Add(new CompanySettings
        {
            CompanyId = _empresaId,
            ReminderOnDueDate = true,
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
        db.CompanySettings.Add(ConfigComEvolution(_empresaId));
        var (cobranca, cliente) = await CriarCobrancaAsync(db, _empresaId, hoje);

        var fake = new FakeEvolutionApiService();
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Single(fake.EnviadosPara);
        Assert.Equal(cliente.WhatsApp, fake.EnviadosPara[0]);

        var log = db.AutomationLogs.IgnoreQueryFilters()
            .FirstOrDefault(l => l.ChargeId == cobranca.Id
                              && l.EventType == AutomacaoTipoEvento.LembreteNoDia);
        Assert.NotNull(log);
        Assert.True(log.Success);
    }

    // ------------------------------------------------------------------
    // 3. Não reenvia para o mesmo par (cobranca, evento)
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoReenvia_QuandoJaEnviou()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.CompanySettings.Add(ConfigComEvolution(_empresaId));
        var (cobranca, _) = await CriarCobrancaAsync(db, _empresaId, hoje);

        // Simula log já existente
        db.AutomationLogs.Add(new AutomationLog
        {
            CompanyId  = _empresaId,
            ChargeId = cobranca.Id,
            EventType = AutomacaoTipoEvento.LembreteNoDia,
            Success    = true,
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
        db.CompanySettings.Add(ConfigComEvolution(_empresaId));

        var cliente = new Customer { CompanyId = _empresaId, Name = "Maria", WhatsApp = "11988880000" };
        db.Customers.Add(cliente);
        await db.SaveChangesAsync();

        db.Charges.Add(new Charge
        {
            CompanyId      = _empresaId,
            CustomerId      = cliente.Id,
            Reference     = "Mens",
            Amount          = 100m,
            DueDate = hoje,
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

        db.CompanySettings.Add(new CompanySettings
        {
            CompanyId        = _empresaId,
            EvolutionApiUrl  = "https://evo.test",
            EvolutionApiKey  = "key",
            EvolutionInstance = "inst",
            ReminderOnDueDate    = true,
            Reminder1DayBefore  = true,
        });
        await db.SaveChangesAsync();

        // Cobrança vencendo hoje
        await CriarCobrancaAsync(db, _empresaId, hoje);
        // Cobrança vencendo amanhã (hoje + 1d → Reminder1DayBefore)
        await CriarCobrancaAsync(db, _empresaId, hoje.AddDays(1));

        var fake = new FakeEvolutionApiService();
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Equal(2, fake.EnviadosPara.Count);
    }

    // ------------------------------------------------------------------
    // 6. Exceção na API → grava log com Success=false e ErrorMessage
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_GravaLog_QuandoApiLancaExcecao()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.CompanySettings.Add(ConfigComEvolution(_empresaId));
        var (cobranca, _) = await CriarCobrancaAsync(db, _empresaId, hoje);

        var fake = new FakeEvolutionApiService { ShouldThrow = true };
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        var log = db.AutomationLogs.IgnoreQueryFilters()
            .FirstOrDefault(l => l.ChargeId == cobranca.Id
                              && l.EventType == AutomacaoTipoEvento.LembreteNoDia);
        Assert.NotNull(log);
        Assert.False(log.Success);
        Assert.NotNull(log.ErrorMessage);
    }

    // ------------------------------------------------------------------
    // 7 (Fix 3 – Test A): Envio com retorno false → grava Success=false
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_GravaLog_ComSucessoFalse_QuandoEnvioFalha()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.CompanySettings.Add(ConfigComEvolution(_empresaId));
        var (cobranca, _) = await CriarCobrancaAsync(db, _empresaId, hoje);

        var fake = new FakeEvolutionApiService { ShouldFail = true };
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        var log = db.AutomationLogs.IgnoreQueryFilters()
            .FirstOrDefault(l => l.ChargeId == cobranca.Id && l.EventType == AutomacaoTipoEvento.LembreteNoDia);
        Assert.NotNull(log);
        Assert.False(log.Success);
    }

    // ------------------------------------------------------------------
    // 8 (Fix 3 – Test B): Customer sem WhatsApp → não envia, não grava log
    // ------------------------------------------------------------------
    [Fact]
    public async Task ProcessarTodosTenantsAsync_NaoEnvia_NaoGravaLog_QuandoClienteSemWhatsapp()
    {
        var db   = CreateDb();
        var hoje = new DateOnly(2026, 6, 10);
        db.CompanySettings.Add(ConfigComEvolution(_empresaId));

        var cliente = new Customer { CompanyId = _empresaId, Name = "Sem Tel", WhatsApp = "" };
        db.Customers.Add(cliente);
        await db.SaveChangesAsync();
        var cobranca = new Charge
        {
            CompanyId      = _empresaId,
            CustomerId      = cliente.Id,
            Reference     = "Mensalidade",
            Amount          = 100m,
            DueDate = hoje,
            Status         = CobrancaStatus.Pendente,
        };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync();

        var fake = new FakeEvolutionApiService();
        var svc  = new LembreteCobrancaService(db, fake);
        await svc.ProcessarTodosTenantsAsync(default, hoje);

        Assert.Empty(fake.EnviadosPara);
        Assert.Empty(db.AutomationLogs.IgnoreQueryFilters().ToList());
    }
}
