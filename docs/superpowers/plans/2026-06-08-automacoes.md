# Automações — Lembretes de Cobrança e Geração Automática

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Automatizar envio de lembretes de cobrança via WhatsApp (Evolution API) e geração mensal de cobranças a partir de contratos ativos.

**Architecture:** Um `AutomacaoHostedService` (BackgroundService .NET) acorda a cada hora e, ao detectar as 07h00 UTC em um dia novo, executa `LembreteCobrancaService` (envia WhatsApp) e `GeracaoCobrancaService` (cria cobranças do dia 1). Ambos iteram todos os tenants via `IgnoreQueryFilters()`. `AutomacaoLog` registra cada envio para deduplicação e auditoria.

**Tech Stack:** .NET 10 BackgroundService + PeriodicTimer, IHttpClientFactory, EF Core + InMemory (testes), React + TypeScript + Tailwind.

---

## File Structure

### New Files
- `backend/src/GestorAI.API/Domain/Enums/AutomacaoTipoEvento.cs` — enum com os 7 tipos de evento
- `backend/src/GestorAI.API/Domain/Entities/AutomacaoLog.cs` — entidade de log + deduplicação
- `backend/src/GestorAI.API/Services/Automacao/IEvolutionApiService.cs` — interface para testabilidade
- `backend/src/GestorAI.API/Services/Automacao/EvolutionApiService.cs` — wrapper HTTP para Evolution API
- `backend/src/GestorAI.API/Services/Automacao/LembreteCobrancaService.cs` — lógica de lembretes
- `backend/src/GestorAI.API/Services/Automacao/GeracaoCobrancaService.cs` — geração de cobranças
- `backend/src/GestorAI.API/Services/Automacao/AutomacaoHostedService.cs` — scheduler de fundo
- `backend/src/GestorAI.API/DTOs/Automacao/AutomacaoDto.cs` — records de resposta/request
- `backend/src/GestorAI.API/Endpoints/AutomacaoEndpoints.cs` — GET log + POST testar-conexao
- `backend/tests/GestorAI.Tests/Services/LembreteCobrancaServiceTests.cs` — testes unitários
- `backend/tests/GestorAI.Tests/Services/GeracaoCobrancaServiceTests.cs` — testes unitários
- `frontend/src/types/automacao.ts` — tipos TypeScript
- `frontend/src/hooks/useAutomacao.ts` — hook de API calls
- `frontend/src/pages/configuracoes/Automacao.tsx` — página de configuração
- `frontend/src/pages/automacao/LogAutomacao.tsx` — página de log de envios

### Modified Files
- `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs` — +9 campos (Evolution API + 6 toggles)
- `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs` — DbSet, HasQueryFilter, HasIndex
- `backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs` — novo record + 9 campos em ConfiguracaoEmpresaResponse
- `backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs` — SalvarAutomacaoConfigAsync + atualizar ToResponse
- `backend/src/GestorAI.API/Endpoints/FiscalEndpoints.cs` — PUT /configuracao-empresa/automacao
- `backend/src/GestorAI.API/Program.cs` — registrar serviços + mapear endpoints
- `frontend/src/router/index.tsx` — 2 novas rotas
- `frontend/src/components/layout/Sidebar.tsx` — novo grupo "Automação"

---

## Task 1: Data Model + Migration + DTO + Endpoint

**Files:**
- Create: `backend/src/GestorAI.API/Domain/Enums/AutomacaoTipoEvento.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/AutomacaoLog.cs`
- Modify: `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs`
- Modify: `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`
- Modify: `backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs`
- Modify: `backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/FiscalEndpoints.cs`

- [ ] **Step 1: Criar AutomacaoTipoEvento enum**

Criar `backend/src/GestorAI.API/Domain/Enums/AutomacaoTipoEvento.cs`:

```csharp
namespace GestorAI.API.Domain.Enums;

public enum AutomacaoTipoEvento
{
    Lembrete3dAntes,
    Lembrete1dAntes,
    LembreteNoDia,
    Lembrete1dDepois,
    Lembrete3dDepois,
    Lembrete7dDepois,
    CobrancaGerada,
}
```

- [ ] **Step 2: Criar AutomacaoLog entity**

Criar `backend/src/GestorAI.API/Domain/Entities/AutomacaoLog.cs`:

```csharp
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class AutomacaoLog : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid CobrancaId { get; set; }
    public AutomacaoTipoEvento TipoEvento { get; set; }
    public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;
    public bool Sucesso { get; set; }
    public string? ErroMsg { get; set; }
    public Cobranca? Cobranca { get; set; }
}
```

- [ ] **Step 3: Adicionar 9 campos em ConfiguracaoEmpresa**

Abrir `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs` e adicionar ao final da classe (antes do fechamento de `}`), após `public bool AsaasSandbox { get; set; } = true;`:

```csharp
    // Evolution API
    public string? EvolutionApiUrl { get; set; }
    public string? EvolutionApiKey { get; set; }
    public string? EvolutionInstance { get; set; }

    // Toggles de lembrete
    public bool Lembrete3dAntes { get; set; } = true;
    public bool Lembrete1dAntes { get; set; } = true;
    public bool LembreteNoDia { get; set; } = true;
    public bool Lembrete1dDepois { get; set; } = true;
    public bool Lembrete3dDepois { get; set; } = false;
    public bool Lembrete7dDepois { get; set; } = false;
```

- [ ] **Step 4: Atualizar AppDbContext**

Em `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`:

1. Adicionar DbSet após `public DbSet<ContratoTemplateItem> ContratoTemplateItens => Set<ContratoTemplateItem>();`:

```csharp
    public DbSet<AutomacaoLog> AutomacaoLogs => Set<AutomacaoLog>();
```

2. Em `OnModelCreating`, adicionar após `modelBuilder.Entity<ContratoTemplate>().HasQueryFilter(...)`:

```csharp
        modelBuilder.Entity<AutomacaoLog>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<AutomacaoLog>()
            .HasIndex(l => new { l.CobrancaId, l.TipoEvento });
```

Também é necessário adicionar o `using` para o novo enum no AppDbContext. Como o namespace correto já é importado via `GestorAI.API.Domain.Entities`, o enum em `GestorAI.API.Domain.Enums` precisa ser verificado — mas como `AutomacaoLog` é uma entity no namespace correto, não é necessário import adicional no DbContext.

- [ ] **Step 5: Criar migration AddAutomacaoLog**

Rodar a partir da pasta `backend/`:

```bash
cd gestorai-erp/backend
dotnet ef migrations add AddAutomacaoLog --project src/GestorAI.API --startup-project src/GestorAI.API
```

Verificar que o arquivo foi criado em `src/GestorAI.API/Migrations/`.

- [ ] **Step 6: Criar migration AddAutomacaoConfigFields**

```bash
dotnet ef migrations add AddAutomacaoConfigFields --project src/GestorAI.API --startup-project src/GestorAI.API
```

Verificar que o arquivo foi criado.

- [ ] **Step 7: Atualizar ConfiguracaoEmpresaDto — novo record + campos**

Em `backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs`:

1. Adicionar `SalvarAutomacaoConfigRequest` após `SalvarIntegracoesRequest`:

```csharp
public record SalvarAutomacaoConfigRequest(
    string? EvolutionApiUrl,
    string? EvolutionApiKey,
    string? EvolutionInstance,
    bool Lembrete3dAntes,
    bool Lembrete1dAntes,
    bool LembreteNoDia,
    bool Lembrete1dDepois,
    bool Lembrete3dDepois,
    bool Lembrete7dDepois);
```

2. Substituir `ConfiguracaoEmpresaResponse` inteiro por (adiciona 9 campos ao final):

```csharp
public record ConfiguracaoEmpresaResponse(
    Guid Id,
    string? RazaoSocial,
    string? NomeFantasia,
    string? Cnpj,
    string? InscricaoEstadual,
    string? InscricaoMunicipal,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? CodigoMunicipio,
    string? Municipio,
    string? Uf,
    string? Cep,
    int? RegimeTributario,
    int? Ambiente,
    int? SerieNfe,
    int? SerieNfce,
    bool TemToken,
    string? Slug,
    string? LogoUrl,
    string? CorPrimaria,
    string? DescricaoPublica,
    string? AsaasApiKey,
    bool AsaasSandbox,
    string? EvolutionApiUrl,
    bool TemEvolutionKey,
    string? EvolutionInstance,
    bool Lembrete3dAntes,
    bool Lembrete1dAntes,
    bool LembreteNoDia,
    bool Lembrete1dDepois,
    bool Lembrete3dDepois,
    bool Lembrete7dDepois);
```

- [ ] **Step 8: Atualizar ConfiguracaoEmpresaService — ToResponse + SalvarAutomacaoConfigAsync**

Em `backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs`:

1. Atualizar o método `ToResponse` (substituir o return statement existente):

```csharp
    private static ConfiguracaoEmpresaResponse ToResponse(ConfiguracaoEmpresa c) => new(
        c.Id,
        c.RazaoSocial,
        c.NomeFantasia,
        c.Cnpj,
        c.InscricaoEstadual,
        c.InscricaoMunicipal,
        c.Logradouro,
        c.Numero,
        c.Complemento,
        c.Bairro,
        c.CodigoMunicipio,
        c.Municipio,
        c.Uf,
        c.Cep,
        c.RegimeTributario,
        c.Ambiente,
        c.SerieNfe,
        c.SerieNfce,
        c.FocusNfeToken is not null,
        c.Slug,
        c.LogoUrl,
        c.CorPrimaria,
        c.DescricaoPublica,
        c.AsaasApiKey,
        c.AsaasSandbox,
        c.EvolutionApiUrl,
        c.EvolutionApiKey is not null,
        c.EvolutionInstance,
        c.Lembrete3dAntes,
        c.Lembrete1dAntes,
        c.LembreteNoDia,
        c.Lembrete1dDepois,
        c.Lembrete3dDepois,
        c.Lembrete7dDepois);
```

2. Adicionar o método `SalvarAutomacaoConfigAsync` após `SalvarIntegracoesAsync`:

```csharp
    public async Task SalvarAutomacaoConfigAsync(SalvarAutomacaoConfigRequest req, CancellationToken ct)
    {
        var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct)
            ?? new ConfiguracaoEmpresa { EmpresaId = tenantContext.EmpresaId };
        var isNew = config.Id == Guid.Empty;
        config.EvolutionApiUrl = req.EvolutionApiUrl;
        if (req.EvolutionApiKey is not null) config.EvolutionApiKey = req.EvolutionApiKey;
        config.EvolutionInstance = req.EvolutionInstance;
        config.Lembrete3dAntes = req.Lembrete3dAntes;
        config.Lembrete1dAntes = req.Lembrete1dAntes;
        config.LembreteNoDia = req.LembreteNoDia;
        config.Lembrete1dDepois = req.Lembrete1dDepois;
        config.Lembrete3dDepois = req.Lembrete3dDepois;
        config.Lembrete7dDepois = req.Lembrete7dDepois;
        if (isNew) db.ConfiguracoesEmpresa.Add(config);
        await db.SaveChangesAsync(ct);
    }
```

- [ ] **Step 9: Adicionar endpoint PUT /configuracao-empresa/automacao em FiscalEndpoints**

Em `backend/src/GestorAI.API/Endpoints/FiscalEndpoints.cs`, adicionar antes do fechamento de `MapFiscal`:

```csharp
        group.MapPut("/configuracao-empresa/automacao", async (
            SalvarAutomacaoConfigRequest req, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
        {
            await svc.SalvarAutomacaoConfigAsync(req, ct);
            return Results.Ok();
        });
```

- [ ] **Step 10: Compilar o projeto para verificar**

```bash
cd gestorai-erp/backend
dotnet build src/GestorAI.API
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 11: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Enums/AutomacaoTipoEvento.cs
git add backend/src/GestorAI.API/Domain/Entities/AutomacaoLog.cs
git add backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs
git add backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs
git add backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs
git add backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs
git add backend/src/GestorAI.API/Endpoints/FiscalEndpoints.cs
git add backend/src/GestorAI.API/Migrations/
git commit -m "feat: add AutomacaoLog entity, 9 automacao fields in ConfiguracaoEmpresa, migrations, DTO and endpoint"
```

---

## Task 2: EvolutionApiService

**Files:**
- Create: `backend/src/GestorAI.API/Services/Automacao/IEvolutionApiService.cs`
- Create: `backend/src/GestorAI.API/Services/Automacao/EvolutionApiService.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar IEvolutionApiService**

Criar `backend/src/GestorAI.API/Services/Automacao/IEvolutionApiService.cs`:

```csharp
namespace GestorAI.API.Services.Automacao;

public interface IEvolutionApiService
{
    Task<bool> EnviarMensagemAsync(
        string apiUrl, string apiKey, string instance,
        string phone, string text, CancellationToken ct);

    Task<bool> TestarConexaoAsync(
        string apiUrl, string apiKey, CancellationToken ct);
}
```

- [ ] **Step 2: Criar EvolutionApiService**

Criar `backend/src/GestorAI.API/Services/Automacao/EvolutionApiService.cs`:

```csharp
using System.Net.Http.Json;

namespace GestorAI.API.Services.Automacao;

public class EvolutionApiService(IHttpClientFactory httpClientFactory) : IEvolutionApiService
{
    public async Task<bool> EnviarMensagemAsync(
        string apiUrl, string apiKey, string instance,
        string phone, string text, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("apikey", apiKey);
        var body = new { number = $"55{phone}", text };
        var res = await client.PostAsJsonAsync($"{apiUrl.TrimEnd('/')}/message/sendText/{instance}", body, ct);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> TestarConexaoAsync(
        string apiUrl, string apiKey, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("apikey", apiKey);
        var res = await client.GetAsync($"{apiUrl.TrimEnd('/')}/instance/fetchInstances", ct);
        return res.IsSuccessStatusCode;
    }
}
```

- [ ] **Step 3: Registrar no Program.cs**

Em `backend/src/GestorAI.API/Program.cs`, adicionar após `builder.Services.AddScoped<AsaasService>();`:

```csharp
builder.Services.AddScoped<IEvolutionApiService, EvolutionApiService>();
```

Adicionar o using no topo do arquivo:

```csharp
using GestorAI.API.Services.Automacao;
```

- [ ] **Step 4: Compilar**

```bash
cd gestorai-erp/backend
dotnet build src/GestorAI.API
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Services/Automacao/IEvolutionApiService.cs
git add backend/src/GestorAI.API/Services/Automacao/EvolutionApiService.cs
git add backend/src/GestorAI.API/Program.cs
git commit -m "feat: add IEvolutionApiService interface and EvolutionApiService HTTP wrapper"
```

---

## Task 3: LembreteCobrancaService (TDD)

**Files:**
- Create: `backend/src/GestorAI.API/Services/Automacao/LembreteCobrancaService.cs`
- Create: `backend/tests/GestorAI.Tests/Services/LembreteCobrancaServiceTests.cs`

- [ ] **Step 1: Escrever os testes (falharão — serviço ainda não existe)**

Criar `backend/tests/GestorAI.Tests/Services/LembreteCobrancaServiceTests.cs`:

```csharp
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
```

- [ ] **Step 2: Executar os testes para confirmar que falham (serviço não existe)**

```bash
cd gestorai-erp/backend
dotnet test tests/GestorAI.Tests --filter "FullyQualifiedName~LembreteCobrancaServiceTests" -v minimal
```

Expected: compile error ou test failures — `LembreteCobrancaService` não existe ainda.

- [ ] **Step 3: Implementar LembreteCobrancaService**

Criar `backend/src/GestorAI.API/Services/Automacao/LembreteCobrancaService.cs`:

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Automacao;

public class LembreteCobrancaService(AppDbContext db, IEvolutionApiService evolutionService)
{
    public async Task ProcessarTodosTenantsAsync(CancellationToken ct, DateOnly? hoje = null)
    {
        var dataHoje = hoje ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var configs = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .Where(c => c.EvolutionApiUrl != null && c.EvolutionApiKey != null && c.EvolutionInstance != null)
            .ToListAsync(ct);

        foreach (var config in configs)
            await ProcessarTenantAsync(config, dataHoje, ct);
    }

    private async Task ProcessarTenantAsync(ConfiguracaoEmpresa config, DateOnly hoje, CancellationToken ct)
    {
        var offsets = new List<(DateOnly TargetDate, AutomacaoTipoEvento TipoEvento)>();
        if (config.Lembrete3dAntes) offsets.Add((hoje.AddDays(3), AutomacaoTipoEvento.Lembrete3dAntes));
        if (config.Lembrete1dAntes) offsets.Add((hoje.AddDays(1), AutomacaoTipoEvento.Lembrete1dAntes));
        if (config.LembreteNoDia)   offsets.Add((hoje,            AutomacaoTipoEvento.LembreteNoDia));
        if (config.Lembrete1dDepois) offsets.Add((hoje.AddDays(-1), AutomacaoTipoEvento.Lembrete1dDepois));
        if (config.Lembrete3dDepois) offsets.Add((hoje.AddDays(-3), AutomacaoTipoEvento.Lembrete3dDepois));
        if (config.Lembrete7dDepois) offsets.Add((hoje.AddDays(-7), AutomacaoTipoEvento.Lembrete7dDepois));

        foreach (var (targetDate, tipoEvento) in offsets)
        {
            var cobrancas = await db.Cobrancas
                .IgnoreQueryFilters()
                .Include(c => c.Cliente)
                .Where(c => c.EmpresaId == config.EmpresaId
                         && c.DataVencimento == targetDate
                         && c.Status == CobrancaStatus.Pendente)
                .ToListAsync(ct);

            foreach (var cobranca in cobrancas)
            {
                if (string.IsNullOrWhiteSpace(cobranca.Cliente?.Whatsapp))
                    continue;

                var jaEnviou = await db.AutomacaoLogs
                    .IgnoreQueryFilters()
                    .AnyAsync(l => l.CobrancaId == cobranca.Id && l.TipoEvento == tipoEvento, ct);
                if (jaEnviou) continue;

                var mensagem = MontarMensagem(cobranca, tipoEvento);
                bool sucesso;
                string? erroMsg = null;
                try
                {
                    sucesso = await evolutionService.EnviarMensagemAsync(
                        config.EvolutionApiUrl!, config.EvolutionApiKey!, config.EvolutionInstance!,
                        cobranca.Cliente.Whatsapp, mensagem, ct);
                }
                catch (Exception ex)
                {
                    sucesso = false;
                    erroMsg = ex.Message;
                }

                db.AutomacaoLogs.Add(new AutomacaoLog
                {
                    EmpresaId = config.EmpresaId,
                    CobrancaId = cobranca.Id,
                    TipoEvento = tipoEvento,
                    Sucesso = sucesso,
                    ErroMsg = erroMsg,
                });
                await db.SaveChangesAsync(ct);
            }
        }
    }

    private static string MontarMensagem(Cobranca cobranca, AutomacaoTipoEvento tipoEvento)
    {
        var nome = cobranca.Cliente!.Nome;
        var valor = cobranca.Valor.ToString("F2");
        var ref_ = cobranca.Referencia;
        var data = cobranca.DataVencimento.ToString("dd/MM/yyyy");

        return tipoEvento switch
        {
            AutomacaoTipoEvento.Lembrete1dDepois or
            AutomacaoTipoEvento.Lembrete3dDepois or
            AutomacaoTipoEvento.Lembrete7dDepois =>
                $"Olá {nome}, sua cobrança de R$ {valor} ({ref_}) venceu em {data} e ainda está em aberto. Por favor, regularize.",
            _ =>
                $"Olá {nome}, sua cobrança de R$ {valor} ({ref_}) vence em {data}. Por favor, realize o pagamento para evitar juros.",
        };
    }
}
```

- [ ] **Step 4: Executar os testes para confirmar que passam**

```bash
cd gestorai-erp/backend
dotnet test tests/GestorAI.Tests --filter "FullyQualifiedName~LembreteCobrancaServiceTests" -v minimal
```

Expected: 6/6 passed.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Services/Automacao/LembreteCobrancaService.cs
git add backend/tests/GestorAI.Tests/Services/LembreteCobrancaServiceTests.cs
git commit -m "feat: implement LembreteCobrancaService with deduplication and tenant isolation (TDD)"
```

---

## Task 4: GeracaoCobrancaService (TDD)

**Files:**
- Create: `backend/src/GestorAI.API/Services/Automacao/GeracaoCobrancaService.cs`
- Create: `backend/tests/GestorAI.Tests/Services/GeracaoCobrancaServiceTests.cs`

- [ ] **Step 1: Escrever os testes (falharão — serviço não existe)**

Criar `backend/tests/GestorAI.Tests/Services/GeracaoCobrancaServiceTests.cs`:

```csharp
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
    public async Task ProcessarTodosTenantsAsync_AjustaVencimento_ParaFevoreiro()
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
```

- [ ] **Step 2: Executar os testes para confirmar que falham**

```bash
cd gestorai-erp/backend
dotnet test tests/GestorAI.Tests --filter "FullyQualifiedName~GeracaoCobrancaServiceTests" -v minimal
```

Expected: compile error — `GeracaoCobrancaService` não existe.

- [ ] **Step 3: Implementar GeracaoCobrancaService**

Criar `backend/src/GestorAI.API/Services/Automacao/GeracaoCobrancaService.cs`:

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Automacao;

public class GeracaoCobrancaService(AppDbContext db)
{
    public async Task ProcessarTodosTenantsAsync(CancellationToken ct, DateOnly? hoje = null)
    {
        var dataHoje = hoje ?? DateOnly.FromDateTime(DateTime.UtcNow);
        if (dataHoje.Day != 1) return;

        var contratos = await db.Contratos
            .IgnoreQueryFilters()
            .Where(c => c.Status == ContratoStatus.Ativo)
            .ToListAsync(ct);

        foreach (var contrato in contratos)
            await ProcessarContratoAsync(contrato, dataHoje.Year, dataHoje.Month, ct);
    }

    private async Task ProcessarContratoAsync(Contrato contrato, int ano, int mes, CancellationToken ct)
    {
        var jaExiste = await db.Cobrancas
            .IgnoreQueryFilters()
            .AnyAsync(c => c.ContratoId == contrato.Id
                        && c.DataVencimento.Year == ano
                        && c.DataVencimento.Month == mes, ct);
        if (jaExiste) return;

        var diaVenc = Math.Min(contrato.DiaVencimento, DateTime.DaysInMonth(ano, mes));
        var dataVencimento = new DateOnly(ano, mes, diaVenc);

        var cobranca = new Cobranca
        {
            EmpresaId = contrato.EmpresaId,
            ClienteId = contrato.ClienteId,
            ContratoId = contrato.Id,
            Referencia = $"Mensalidade {mes:00}/{ano}",
            Valor = contrato.Valor,
            DataVencimento = dataVencimento,
            Status = CobrancaStatus.Pendente,
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync(ct);

        db.AutomacaoLogs.Add(new AutomacaoLog
        {
            EmpresaId = contrato.EmpresaId,
            CobrancaId = cobranca.Id,
            TipoEvento = AutomacaoTipoEvento.CobrancaGerada,
            Sucesso = true,
        });
        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 4: Executar os testes para confirmar que passam**

```bash
cd gestorai-erp/backend
dotnet test tests/GestorAI.Tests --filter "FullyQualifiedName~GeracaoCobrancaServiceTests" -v minimal
```

Expected: 6/6 passed.

- [ ] **Step 5: Executar todos os testes para verificar regressões**

```bash
cd gestorai-erp/backend
dotnet test tests/GestorAI.Tests -v minimal
```

Expected: todos os testes passam.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Services/Automacao/GeracaoCobrancaService.cs
git add backend/tests/GestorAI.Tests/Services/GeracaoCobrancaServiceTests.cs
git commit -m "feat: implement GeracaoCobrancaService with day-1 guard, deduplication and Feb edge case (TDD)"
```

---

## Task 5: AutomacaoHostedService + AutomacaoEndpoints + Program.cs wiring

**Files:**
- Create: `backend/src/GestorAI.API/Services/Automacao/AutomacaoHostedService.cs`
- Create: `backend/src/GestorAI.API/DTOs/Automacao/AutomacaoDto.cs`
- Create: `backend/src/GestorAI.API/Endpoints/AutomacaoEndpoints.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar AutomacaoHostedService**

Criar `backend/src/GestorAI.API/Services/Automacao/AutomacaoHostedService.cs`:

```csharp
using Microsoft.Extensions.Hosting;

namespace GestorAI.API.Services.Automacao;

public class AutomacaoHostedService(IServiceScopeFactory scopeFactory) : BackgroundService
{
    private DateTime _ultimaExecucao = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
        while (await timer.WaitForNextTickAsync(ct))
        {
            var agora = DateTime.UtcNow;
            if (agora.Hour == 7 && agora.Date > _ultimaExecucao.Date)
            {
                _ultimaExecucao = agora;
                await using var scope = scopeFactory.CreateAsyncScope();
                var lembretes = scope.ServiceProvider.GetRequiredService<LembreteCobrancaService>();
                var geracao = scope.ServiceProvider.GetRequiredService<GeracaoCobrancaService>();
                await lembretes.ProcessarTodosTenantsAsync(ct);
                await geracao.ProcessarTodosTenantsAsync(ct);
            }
        }
    }
}
```

- [ ] **Step 2: Criar AutomacaoDto**

Criar `backend/src/GestorAI.API/DTOs/Automacao/AutomacaoDto.cs`:

```csharp
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Automacao;

public record AutomacaoLogResponse(
    Guid Id,
    DateTime EnviadoEm,
    string ClienteNome,
    string Referencia,
    AutomacaoTipoEvento TipoEvento,
    bool Sucesso,
    string? ErroMsg);

public record TestarConexaoRequest(string ApiUrl, string ApiKey);
```

- [ ] **Step 3: Criar AutomacaoEndpoints**

Criar `backend/src/GestorAI.API/Endpoints/AutomacaoEndpoints.cs`:

```csharp
using GestorAI.API.DTOs.Automacao;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Automacao;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Endpoints;

public static class AutomacaoEndpoints
{
    public static void MapAutomacao(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/automacao").RequireAuthorization();

        group.MapGet("/log", async (
            AppDbContext db, CancellationToken ct,
            bool? apenasErros = null) =>
        {
            var query = db.AutomacaoLogs
                .Include(l => l.Cobranca).ThenInclude(c => c!.Cliente)
                .AsQueryable();

            if (apenasErros == true)
                query = query.Where(l => !l.Sucesso);

            var logs = await query
                .OrderByDescending(l => l.EnviadoEm)
                .Take(100)
                .Select(l => new AutomacaoLogResponse(
                    l.Id,
                    l.EnviadoEm,
                    l.Cobranca!.Cliente!.Nome,
                    l.Cobranca.Referencia,
                    l.TipoEvento,
                    l.Sucesso,
                    l.ErroMsg))
                .ToListAsync(ct);

            return Results.Ok(logs);
        });

        group.MapPost("/testar-conexao", async (
            TestarConexaoRequest req, IEvolutionApiService evolutionSvc, CancellationToken ct) =>
        {
            var ok = await evolutionSvc.TestarConexaoAsync(req.ApiUrl, req.ApiKey, ct);
            return Results.Ok(new { sucesso = ok });
        });
    }
}
```

- [ ] **Step 4: Registrar no Program.cs**

Em `backend/src/GestorAI.API/Program.cs`:

1. Adicionar os registros de serviço após `builder.Services.AddScoped<IEvolutionApiService, EvolutionApiService>();`:

```csharp
builder.Services.AddScoped<LembreteCobrancaService>();
builder.Services.AddScoped<GeracaoCobrancaService>();
builder.Services.AddHostedService<AutomacaoHostedService>();
```

2. Adicionar o mapeamento de endpoints após `app.MapCobrancas();`:

```csharp
app.MapAutomacao();
```

3. Adicionar o using no topo se necessário (se `AutomacaoHostedService` não estiver acessível):

O namespace `GestorAI.API.Services.Automacao` já deve ser resolvido pelas diretivas de using existentes, mas adicionar:

```csharp
using GestorAI.API.Endpoints;
```

já está presente. O using do Automacao namespace já foi adicionado em Task 2. Verificar e adicionar se necessário.

- [ ] **Step 5: Compilar**

```bash
cd gestorai-erp/backend
dotnet build src/GestorAI.API
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Services/Automacao/AutomacaoHostedService.cs
git add backend/src/GestorAI.API/DTOs/Automacao/AutomacaoDto.cs
git add backend/src/GestorAI.API/Endpoints/AutomacaoEndpoints.cs
git add backend/src/GestorAI.API/Program.cs
git commit -m "feat: add AutomacaoHostedService, AutomacaoEndpoints, wire all automacao services"
```

---

## Task 6: Frontend — Configuração de Automação

**Files:**
- Create: `frontend/src/pages/configuracoes/Automacao.tsx`
- Modify: `frontend/src/router/index.tsx`

- [ ] **Step 1: Criar página Automacao.tsx**

Criar `frontend/src/pages/configuracoes/Automacao.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'

interface AutomacaoConfig {
  evolutionApiUrl: string
  evolutionApiKey: string
  evolutionInstance: string
  lembrete3dAntes: boolean
  lembrete1dAntes: boolean
  lembreteNoDia: boolean
  lembrete1dDepois: boolean
  lembrete3dDepois: boolean
  lembrete7dDepois: boolean
}

export default function Automacao() {
  const [config, setConfig] = useState<AutomacaoConfig>({
    evolutionApiUrl: '',
    evolutionApiKey: '',
    evolutionInstance: '',
    lembrete3dAntes: true,
    lembrete1dAntes: true,
    lembreteNoDia: true,
    lembrete1dDepois: true,
    lembrete3dDepois: false,
    lembrete7dDepois: false,
  })
  const [saving, setSaving] = useState(false)
  const [testando, setTestando] = useState(false)

  useEffect(() => {
    api.get<{
      evolutionApiUrl?: string | null
      temEvolutionKey?: boolean
      evolutionInstance?: string | null
      lembrete3dAntes?: boolean
      lembrete1dAntes?: boolean
      lembreteNoDia?: boolean
      lembrete1dDepois?: boolean
      lembrete3dDepois?: boolean
      lembrete7dDepois?: boolean
    }>('/api/configuracao-empresa')
      .then(d => setConfig(c => ({
        ...c,
        evolutionApiUrl: d.evolutionApiUrl ?? '',
        evolutionInstance: d.evolutionInstance ?? '',
        lembrete3dAntes: d.lembrete3dAntes ?? true,
        lembrete1dAntes: d.lembrete1dAntes ?? true,
        lembreteNoDia: d.lembreteNoDia ?? true,
        lembrete1dDepois: d.lembrete1dDepois ?? true,
        lembrete3dDepois: d.lembrete3dDepois ?? false,
        lembrete7dDepois: d.lembrete7dDepois ?? false,
      })))
      .catch(() => {})
  }, [])

  async function handleSave() {
    setSaving(true)
    try {
      await api.put('/api/configuracao-empresa/automacao', {
        evolutionApiUrl: config.evolutionApiUrl || null,
        evolutionApiKey: config.evolutionApiKey || null,
        evolutionInstance: config.evolutionInstance || null,
        lembrete3dAntes: config.lembrete3dAntes,
        lembrete1dAntes: config.lembrete1dAntes,
        lembreteNoDia: config.lembreteNoDia,
        lembrete1dDepois: config.lembrete1dDepois,
        lembrete3dDepois: config.lembrete3dDepois,
        lembrete7dDepois: config.lembrete7dDepois,
      })
      toast.success('Configurações salvas!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  async function handleTestarConexao() {
    if (!config.evolutionApiUrl || !config.evolutionApiKey) {
      toast.error('Preencha a URL e a API Key antes de testar.')
      return
    }
    setTestando(true)
    try {
      const res = await api.post<{ sucesso: boolean }>('/api/automacao/testar-conexao', {
        apiUrl: config.evolutionApiUrl,
        apiKey: config.evolutionApiKey,
      })
      if (res.sucesso) toast.success('Conexão bem-sucedida! ✅')
      else toast.error('Falha na conexão. Verifique URL e API Key. ❌')
    } catch {
      toast.error('Falha na conexão. ❌')
    } finally {
      setTestando(false)
    }
  }

  const toggles: { key: keyof AutomacaoConfig; label: string }[] = [
    { key: 'lembrete3dAntes',  label: '3 dias antes do vencimento' },
    { key: 'lembrete1dAntes',  label: '1 dia antes do vencimento' },
    { key: 'lembreteNoDia',    label: 'No dia do vencimento' },
    { key: 'lembrete1dDepois', label: '1 dia após o vencimento' },
    { key: 'lembrete3dDepois', label: '3 dias após o vencimento' },
    { key: 'lembrete7dDepois', label: '7 dias após o vencimento' },
  ]

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold">Automação</h1>

      {/* Bloco 1: Evolution API */}
      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Integração Evolution API</h2>
        <p className="text-sm text-muted-foreground">
          Conecte sua instância do Evolution API para enviar lembretes de cobrança via WhatsApp automaticamente.
        </p>
        <div className="grid gap-2">
          <Label>URL da instância</Label>
          <Input
            value={config.evolutionApiUrl}
            onChange={e => setConfig(c => ({ ...c, evolutionApiUrl: e.target.value }))}
            placeholder="https://evolution.suaempresa.com"
          />
        </div>
        <div className="grid gap-2">
          <Label>API Key</Label>
          <Input
            type="password"
            value={config.evolutionApiKey}
            onChange={e => setConfig(c => ({ ...c, evolutionApiKey: e.target.value }))}
            placeholder="Deixe em branco para manter a atual"
          />
        </div>
        <div className="grid gap-2">
          <Label>Nome da instância</Label>
          <Input
            value={config.evolutionInstance}
            onChange={e => setConfig(c => ({ ...c, evolutionInstance: e.target.value }))}
            placeholder="minha-instancia"
          />
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => void handleTestarConexao()} disabled={testando}>
            {testando ? 'Testando...' : 'Testar conexão'}
          </Button>
          <Button onClick={() => void handleSave()} disabled={saving}>
            {saving ? 'Salvando...' : 'Salvar'}
          </Button>
        </div>
      </div>

      {/* Bloco 2: Lembretes ativos */}
      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Lembretes ativos</h2>
        <p className="text-sm text-muted-foreground">
          Escolha quando enviar lembretes de cobrança automaticamente via WhatsApp.
        </p>
        <div className="space-y-3">
          {toggles.map(({ key, label }) => (
            <div key={key} className="flex items-center gap-2">
              <input
                type="checkbox"
                id={key}
                checked={config[key] as boolean}
                onChange={e => setConfig(c => ({ ...c, [key]: e.target.checked }))}
              />
              <Label htmlFor={key}>{label}</Label>
            </div>
          ))}
        </div>
        <Button onClick={() => void handleSave()} disabled={saving}>
          {saving ? 'Salvando...' : 'Salvar'}
        </Button>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Adicionar rota no router**

Em `frontend/src/router/index.tsx`:

1. Adicionar import no topo:

```tsx
import Automacao from '@/pages/configuracoes/Automacao'
```

2. Adicionar rota nos children do AppLayout, após `{ path: '/configuracoes/integracoes', element: <Integracoes /> }`:

```tsx
      { path: '/configuracoes/automacao', element: <Automacao /> },
```

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/configuracoes/Automacao.tsx
git add frontend/src/router/index.tsx
git commit -m "feat: add automacao config page with Evolution API form and reminder toggles"
```

---

## Task 7: Frontend — Log de Automações + Sidebar

**Files:**
- Create: `frontend/src/types/automacao.ts`
- Create: `frontend/src/pages/automacao/LogAutomacao.tsx`
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Criar types/automacao.ts**

Criar `frontend/src/types/automacao.ts`:

```ts
export type AutomacaoTipoEvento =
  | 'Lembrete3dAntes'
  | 'Lembrete1dAntes'
  | 'LembreteNoDia'
  | 'Lembrete1dDepois'
  | 'Lembrete3dDepois'
  | 'Lembrete7dDepois'
  | 'CobrancaGerada'

export const EVENTO_LABEL: Record<AutomacaoTipoEvento, string> = {
  Lembrete3dAntes:  '3 dias antes',
  Lembrete1dAntes:  '1 dia antes',
  LembreteNoDia:    'No dia',
  Lembrete1dDepois: '1 dia depois',
  Lembrete3dDepois: '3 dias depois',
  Lembrete7dDepois: '7 dias depois',
  CobrancaGerada:   'Cobrança gerada',
}

export interface AutomacaoLogItem {
  id: string
  enviadoEm: string
  clienteNome: string
  referencia: string
  tipoEvento: AutomacaoTipoEvento
  sucesso: boolean
  erroMsg: string | null
}
```

- [ ] **Step 2: Criar página LogAutomacao.tsx**

Criar `frontend/src/pages/automacao/LogAutomacao.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import type { AutomacaoLogItem } from '@/types/automacao'
import { EVENTO_LABEL } from '@/types/automacao'
import { Badge } from '@/components/ui/badge'

export default function LogAutomacao() {
  const [logs, setLogs] = useState<AutomacaoLogItem[]>([])
  const [apenasErros, setApenasErros] = useState(false)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    setLoading(true)
    const qs = apenasErros ? '?apenasErros=true' : ''
    api.get<AutomacaoLogItem[]>(`/api/automacao/log${qs}`)
      .then(setLogs)
      .catch(() => {})
      .finally(() => setLoading(false))
  }, [apenasErros])

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Log de Automações</h1>
        <div className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            id="apenasErros"
            checked={apenasErros}
            onChange={e => setApenasErros(e.target.checked)}
          />
          <label htmlFor="apenasErros">Apenas falhas</label>
        </div>
      </div>

      {loading && <p className="text-muted-foreground text-sm">Carregando...</p>}

      {!loading && logs.length === 0 && (
        <p className="text-muted-foreground text-sm">Nenhum registro encontrado.</p>
      )}

      {!loading && logs.length > 0 && (
        <div className="rounded-md border overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="p-3 text-left font-medium">Data/hora</th>
                <th className="p-3 text-left font-medium">Cliente</th>
                <th className="p-3 text-left font-medium">Referência</th>
                <th className="p-3 text-left font-medium">Evento</th>
                <th className="p-3 text-left font-medium">Status</th>
                <th className="p-3 text-left font-medium">Erro</th>
              </tr>
            </thead>
            <tbody>
              {logs.map(log => (
                <tr key={log.id} className="border-b last:border-0 hover:bg-muted/30">
                  <td className="p-3 text-muted-foreground whitespace-nowrap">
                    {new Date(log.enviadoEm).toLocaleString('pt-BR')}
                  </td>
                  <td className="p-3">{log.clienteNome}</td>
                  <td className="p-3">{log.referencia}</td>
                  <td className="p-3">{EVENTO_LABEL[log.tipoEvento]}</td>
                  <td className="p-3">
                    {log.sucesso
                      ? <Badge variant="secondary" className="bg-green-100 text-green-700">Enviado</Badge>
                      : <Badge variant="destructive">Falha</Badge>
                    }
                  </td>
                  <td className="p-3 text-muted-foreground text-xs max-w-[200px] truncate">
                    {log.erroMsg ?? '—'}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 3: Adicionar rota no router**

Em `frontend/src/router/index.tsx`:

1. Adicionar import:

```tsx
import LogAutomacao from '@/pages/automacao/LogAutomacao'
```

2. Adicionar rota nos children, após `{ path: '/configuracoes/automacao', element: <Automacao /> }`:

```tsx
      { path: '/automacao/log', element: <LogAutomacao /> },
```

- [ ] **Step 4: Adicionar grupo "Automação" no Sidebar**

Em `frontend/src/components/layout/Sidebar.tsx`:

1. Adicionar import do ícone `Bot` no topo (ao lado dos outros imports de `lucide-react`):

```tsx
  Bot,
```

2. Adicionar o grupo "Automação" no array `menuGroups`, após o grupo "Configurações":

```tsx
  {
    label: 'Automação',
    items: [
      { icon: Bot, label: 'Configurações', path: '/configuracoes/automacao' },
      { icon: Bot, label: 'Log de envios',  path: '/automacao/log' },
    ],
  },
```

Nota: ambos os itens do grupo compartilham o ícone `Bot` por simplicidade. Se preferir diferenciação visual, usar `Settings` para Configurações e `ScrollText` para Log — ambos já disponíveis via lucide-react.

- [ ] **Step 5: Verificar compilação do frontend**

```bash
cd gestorai-erp/frontend
npx tsc --noEmit
```

Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/types/automacao.ts
git add frontend/src/pages/automacao/LogAutomacao.tsx
git add frontend/src/router/index.tsx
git add frontend/src/components/layout/Sidebar.tsx
git commit -m "feat: add automacao log page, types, routes and sidebar Automação group"
```

---

## Self-Review

### Spec Coverage Check

| Requisito do spec | Task |
|---|---|
| `AutomacaoLog` entity + enum `AutomacaoTipoEvento` | Task 1 |
| 9 campos em `ConfiguracaoEmpresa` | Task 1 |
| Migration `AddAutomacaoLog` | Task 1 |
| Migration `AddAutomacaoConfigFields` | Task 1 |
| `PUT /api/configuracao-empresa/automacao` | Task 1 |
| `IEvolutionApiService` + `EvolutionApiService` | Task 2 |
| `LembreteCobrancaService` — deduplicação, offsets, tenant isolation | Task 3 |
| `GeracaoCobrancaService` — dia 1, DateOnly edge case, deduplicação | Task 4 |
| `AutomacaoHostedService` — PeriodicTimer, hora 7, `_ultimaExecucao` guard | Task 5 |
| `GET /api/automacao/log` | Task 5 |
| `POST /api/automacao/testar-conexao` | Task 5 |
| Página `/configuracoes/automacao` — Bloco 1 (Evolution API + testar + salvar) | Task 6 |
| Página `/configuracoes/automacao` — Bloco 2 (6 checkboxes + salvar) | Task 6 |
| Página `/automacao/log` — tabela + filtro | Task 7 |
| Sidebar — grupo "Automação" com 2 itens | Task 7 |

### Error Handling
- Falha no envio Evolution API → `Sucesso = false, ErroMsg = ex.Message`, continua para o próximo ✅ (Task 3, `try/catch` no `ProcessarTenantAsync`)
- Cobrança sem WhatsApp → pula silenciosamente ✅ (Task 3, `if (string.IsNullOrWhiteSpace)`)
- Evolution não configurado → tenant inteiro pulado ✅ (Task 3, WHERE clause no `ProcessarTodosTenantsAsync`)
- Cobrança no dia 31, mês com 28 dias → `Math.Min(DiaVencimento, DateTime.DaysInMonth)` ✅ (Task 4)

### Testing
- `LembreteCobrancaService`: 6 testes (deduplicação, offset correto, tenant isolation via config, já pago, grava log) ✅
- `GeracaoCobrancaService`: 6 testes (cria no dia 1, não roda fora dia 1, deduplicação, contrato inativo, fevereiro, grava log) ✅
- `AutomacaoHostedService`: não precisa de teste (apenas wiring) ✅
