# Página Pública de Agendamento — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Criar página pública `/agendar/:slug` onde clientes agendam serviços sem login, com branding personalizável e confirmação interna obrigatória.

**Architecture:** Novos endpoints públicos `/public/{slug}/...` sem autenticação no backend existente, usando `IgnoreQueryFilters()` para bypass do multi-tenant. Nova rota React fora do `AppLayout`. Agendamentos públicos nascem com `AguardandoConfirmacao` (slot bloqueado) até confirmação interna.

**Tech Stack:** .NET 10 Minimal APIs, EF Core + PostgreSQL + InMemory (testes), React + TypeScript, Tailwind CSS, react-hook-form + Zod.

---

## File Map

**Backend — criar:**
- `backend/src/GestorAI.API/Domain/Enums/AgendamentoStatus.cs` — adicionar valor
- `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs` — adicionar campos
- `backend/src/GestorAI.API/DTOs/PublicBooking/PublicBookingDto.cs` — DTOs públicos
- `backend/src/GestorAI.API/Services/PublicBooking/PublicBookingService.cs` — lógica pública
- `backend/src/GestorAI.API/Endpoints/PublicBookingEndpoints.cs` — endpoints sem auth
- `backend/tests/GestorAI.Tests/Services/PublicBookingServiceTests.cs` — testes

**Backend — modificar:**
- `backend/src/GestorAI.API/Services/Agendamentos/AgendamentoService.cs` — ConfirmarAsync + RecusarAsync + PendentesConfirmacaoAsync
- `backend/src/GestorAI.API/Endpoints/AgendamentosEndpoints.cs` — /recusar + /pendentes-confirmacao
- `backend/src/GestorAI.API/Endpoints/FiscalEndpoints.cs` — logo upload + branding config
- `backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs` — SalvarBrandingAsync + UploadLogoAsync
- `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs` — índice único no Slug
- `backend/src/GestorAI.API/Program.cs` — UseStaticFiles + registrar PublicBookingService
- `backend/src/GestorAI.API/Migrations/` — nova migration AddPublicBookingFields

**Frontend — criar:**
- `frontend/src/services/publicBookingApi.ts` — chamadas públicas sem token
- `frontend/src/pages/agendamento-publico/usePublicBooking.ts` — estado do wizard
- `frontend/src/pages/agendamento-publico/AgendamentoPublico.tsx` — página wizard pública
- `frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx` — tela de configuração

**Frontend — modificar:**
- `frontend/src/types/agendamento.ts` — adicionar AguardandoConfirmacao
- `frontend/src/hooks/useAgendamentos.ts` — adicionar recusar + pendentesConfirmacao
- `frontend/src/router/index.tsx` — nova rota pública + rota de config
- `frontend/src/components/layout/Sidebar.tsx` — link Configurações
- `frontend/src/pages/agendamentos/Agendamentos.tsx` — seção pendentes

---

## Task 1: Enum, entidade e migration

**Files:**
- Modify: `backend/src/GestorAI.API/Domain/Enums/AgendamentoStatus.cs`
- Modify: `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs`
- Modify: `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`
- Create: `backend/src/GestorAI.API/Migrations/` (gerado pelo EF)

- [ ] **Step 1: Adicionar AguardandoConfirmacao ao enum**

```csharp
// backend/src/GestorAI.API/Domain/Enums/AgendamentoStatus.cs
namespace GestorAI.API.Domain.Enums;

public enum AgendamentoStatus
{
    Agendado,
    Confirmado,
    Concluido,
    Cancelado,
    AguardandoConfirmacao,
}
```

- [ ] **Step 2: Adicionar campos de branding em ConfiguracaoEmpresa**

```csharp
// backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs
namespace GestorAI.API.Domain.Entities;

public class ConfiguracaoEmpresa : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Cnpj { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? CodigoMunicipio { get; set; }
    public string? Municipio { get; set; }
    public string? Uf { get; set; }
    public string? Cep { get; set; }
    public int? RegimeTributario { get; set; }
    public string? CscId { get; set; }
    public string? CscToken { get; set; }
    public int? Ambiente { get; set; }
    public int? SerieNfe { get; set; }
    public int? SerieNfce { get; set; }
    public string? FocusNfeToken { get; set; }
    // Branding público
    public string? Slug { get; set; }
    public string? LogoUrl { get; set; }
    public string? CorPrimaria { get; set; }
    public string? DescricaoPublica { get; set; }
}
```

- [ ] **Step 3: Adicionar índice único no Slug em AppDbContext.OnModelCreating**

Encontre o bloco `OnModelCreating` em `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs` e adicione antes do `}` de fechamento:

```csharp
        modelBuilder.Entity<ConfiguracaoEmpresa>()
            .HasIndex(c => c.Slug)
            .IsUnique()
            .HasFilter("\"Slug\" IS NOT NULL");
```

- [ ] **Step 4: Gerar migration**

```bash
cd backend
/usr/local/share/dotnet/dotnet ef migrations add AddPublicBookingFields \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API
```

Expected: arquivo `Migrations/YYYYMMDDHHMMSS_AddPublicBookingFields.cs` criado.

- [ ] **Step 5: Aplicar migration**

```bash
/usr/local/share/dotnet/dotnet ef database update \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API
```

Expected: `Done.`

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Enums/AgendamentoStatus.cs \
        backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs \
        backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs \
        backend/src/GestorAI.API/Migrations/
git commit -m "feat: adiciona AguardandoConfirmacao e campos de branding"
```

---

## Task 2: DTOs públicos

**Files:**
- Create: `backend/src/GestorAI.API/DTOs/PublicBooking/PublicBookingDto.cs`

- [ ] **Step 1: Criar arquivo de DTOs**

```csharp
// backend/src/GestorAI.API/DTOs/PublicBooking/PublicBookingDto.cs
namespace GestorAI.API.DTOs.PublicBooking;

public record PublicEmpresaInfo(
    string Nome,
    string? LogoUrl,
    string? CorPrimaria,
    string? Descricao);

public record PublicServicoResponse(
    Guid Id,
    string Nome,
    decimal Preco,
    int? DuracaoMinutos);

public record PublicProfissionalResponse(
    Guid Id,
    string Nome);

public record PublicDisponibilidadeResponse(
    List<int> DiasComDisponibilidade);

public record PublicCriarAgendamentoRequest(
    Guid ServicoId,
    Guid ProfissionalId,
    DateTime DataHoraInicio,
    string ClienteNome,
    string ClienteTelefone);

public record PublicAgendamentoConfirmado(
    Guid Id,
    string ServicoNome,
    string ProfissionalNome,
    DateTime DataHoraInicio,
    DateTime DataHoraFim);

public record ConfigurarBrandingRequest(
    string Slug,
    string? CorPrimaria,
    string? DescricaoPublica);
```

- [ ] **Step 2: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/PublicBooking/
git commit -m "feat: DTOs para booking público"
```

---

## Task 3: PublicBookingService

**Files:**
- Create: `backend/src/GestorAI.API/Services/PublicBooking/PublicBookingService.cs`

**Contexto importante:** Este serviço usa `IgnoreQueryFilters()` em todas as queries porque não há `TenantContext` nos endpoints públicos. Sempre filtra por `EmpresaId` manualmente.

- [ ] **Step 1: Criar PublicBookingService**

```csharp
// backend/src/GestorAI.API/Services/PublicBooking/PublicBookingService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.PublicBooking;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.PublicBooking;

public class PublicBookingService(AppDbContext db)
{
    public async Task<Guid> ResolveEmpresaAsync(string slug, CancellationToken ct)
    {
        var config = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Slug == slug, ct)
            ?? throw new AppException("Empresa não encontrada.", 404);
        return config.EmpresaId;
    }

    public async Task<PublicEmpresaInfo> GetInfoAsync(Guid empresaId, CancellationToken ct)
    {
        var config = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId, ct)
            ?? throw new AppException("Empresa não encontrada.", 404);
        return new PublicEmpresaInfo(
            config.NomeFantasia ?? config.RazaoSocial ?? "Empresa",
            config.LogoUrl,
            config.CorPrimaria,
            config.DescricaoPublica);
    }

    public async Task<List<PublicServicoResponse>> GetServicosAsync(Guid empresaId, CancellationToken ct) =>
        await db.Produtos
            .IgnoreQueryFilters()
            .Where(p => p.EmpresaId == empresaId && p.Tipo == TipoProduto.Servico && p.Ativo && p.DuracaoMinutos != null)
            .OrderBy(p => p.Nome)
            .Select(p => new PublicServicoResponse(p.Id, p.Nome, p.PrecoVenda, p.DuracaoMinutos))
            .ToListAsync(ct);

    public async Task<List<PublicProfissionalResponse>> GetProfissionaisAsync(Guid empresaId, CancellationToken ct) =>
        await db.Profissionais
            .IgnoreQueryFilters()
            .Where(p => p.EmpresaId == empresaId && p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new PublicProfissionalResponse(p.Id, p.Nome))
            .ToListAsync(ct);

    public async Task<List<int>> GetDiasComDisponibilidadeAsync(Guid empresaId, Guid profissionalId, CancellationToken ct)
    {
        var profissional = await db.Profissionais
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == profissionalId && p.EmpresaId == empresaId, ct)
            ?? throw new AppException("Profissional não encontrado.", 404);

        return await db.DisponibilidadeSemanais
            .IgnoreQueryFilters()
            .Where(d => d.ProfissionalId == profissionalId)
            .Select(d => d.DiaSemana)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<List<DateTime>> GetSlotsAsync(
        Guid empresaId, Guid profissionalId, Guid servicoId, DateOnly data, CancellationToken ct)
    {
        var diaSemana = (int)data.DayOfWeek;

        var faixas = await db.DisponibilidadeSemanais
            .IgnoreQueryFilters()
            .Where(d => d.ProfissionalId == profissionalId && d.DiaSemana == diaSemana)
            .ToListAsync(ct);

        if (faixas.Count == 0) return [];

        var servico = await db.Produtos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == servicoId && p.EmpresaId == empresaId && p.DuracaoMinutos != null, ct)
            ?? throw new AppException("Serviço não encontrado.", 404);

        var duracao = TimeSpan.FromMinutes(servico.DuracaoMinutos!.Value);
        var inicioDia = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimDia = inicioDia.AddDays(1);

        var bloqueios = await db.BloqueiosAgenda
            .IgnoreQueryFilters()
            .Where(b => b.EmpresaId == empresaId
                && b.DataInicio < fimDia && b.DataFim > inicioDia
                && (b.ProfissionalId == null || b.ProfissionalId == profissionalId))
            .ToListAsync(ct);

        var ocupados = await db.Agendamentos
            .IgnoreQueryFilters()
            .Where(a => a.EmpresaId == empresaId
                && a.ProfissionalId == profissionalId
                && a.DataHoraInicio >= inicioDia && a.DataHoraInicio < fimDia
                && a.Status != AgendamentoStatus.Cancelado)
            .ToListAsync(ct);

        var slots = new List<DateTime>();
        var incremento = TimeSpan.FromMinutes(30);

        foreach (var faixa in faixas)
        {
            var cursor = data.ToDateTime(TimeOnly.FromTimeSpan(faixa.HoraInicio), DateTimeKind.Utc);
            var limite = data.ToDateTime(TimeOnly.FromTimeSpan(faixa.HoraFim), DateTimeKind.Utc) - duracao;

            while (cursor <= limite)
            {
                var fim = cursor + duracao;
                var bloqueado = bloqueios.Any(b => b.DataInicio < fim && b.DataFim > cursor);
                var ocupado = ocupados.Any(a => a.DataHoraInicio < fim && a.DataHoraFim > cursor);

                if (!bloqueado && !ocupado)
                    slots.Add(cursor);

                cursor += incremento;
            }
        }

        return [.. slots.OrderBy(s => s)];
    }

    public async Task<PublicAgendamentoConfirmado> CriarAgendamentoAsync(
        Guid empresaId, PublicCriarAgendamentoRequest req, CancellationToken ct)
    {
        _ = await db.Profissionais
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == req.ProfissionalId && p.EmpresaId == empresaId, ct)
            ?? throw new AppException("Profissional não encontrado.", 404);

        var servico = await db.Produtos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == req.ServicoId && p.EmpresaId == empresaId && p.DuracaoMinutos != null, ct)
            ?? throw new AppException("Serviço não encontrado.", 404);

        var dataHoraFim = req.DataHoraInicio.AddMinutes(servico.DuracaoMinutos!.Value);

        var conflito = await db.Agendamentos
            .IgnoreQueryFilters()
            .AnyAsync(a => a.EmpresaId == empresaId
                && a.ProfissionalId == req.ProfissionalId
                && a.Status != AgendamentoStatus.Cancelado
                && a.DataHoraInicio < dataHoraFim && a.DataHoraFim > req.DataHoraInicio, ct);

        if (conflito)
            throw new AppException("Horário não está mais disponível.", 409);

        var agendamento = new Agendamento
        {
            EmpresaId = empresaId,
            ProfissionalId = req.ProfissionalId,
            ClienteNome = req.ClienteNome,
            ClienteTelefone = req.ClienteTelefone,
            ServicoId = req.ServicoId,
            DataHoraInicio = req.DataHoraInicio,
            DataHoraFim = dataHoraFim,
            Status = AgendamentoStatus.AguardandoConfirmacao,
        };

        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync(ct);

        return new PublicAgendamentoConfirmado(
            agendamento.Id,
            servico.Nome,
            (await db.Profissionais.IgnoreQueryFilters()
                .FirstAsync(p => p.Id == req.ProfissionalId, ct)).Nome,
            agendamento.DataHoraInicio,
            agendamento.DataHoraFim);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add backend/src/GestorAI.API/Services/PublicBooking/
git commit -m "feat: PublicBookingService com IgnoreQueryFilters"
```

---

## Task 4: PublicBookingEndpoints + registros no Program.cs

**Files:**
- Create: `backend/src/GestorAI.API/Endpoints/PublicBookingEndpoints.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar PublicBookingEndpoints**

```csharp
// backend/src/GestorAI.API/Endpoints/PublicBookingEndpoints.cs
using GestorAI.API.DTOs.PublicBooking;
using GestorAI.API.Services.PublicBooking;

namespace GestorAI.API.Endpoints;

public static class PublicBookingEndpoints
{
    public static void MapPublicBooking(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/public/{slug}");

        group.MapGet("/info", async (
            string slug, PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetInfoAsync(empresaId, ct));
        });

        group.MapGet("/servicos", async (
            string slug, PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetServicosAsync(empresaId, ct));
        });

        group.MapGet("/profissionais", async (
            string slug, PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetProfissionaisAsync(empresaId, ct));
        });

        group.MapGet("/disponibilidade", async (
            string slug, Guid profissionalId,
            PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            var dias = await svc.GetDiasComDisponibilidadeAsync(empresaId, profissionalId, ct);
            return Results.Ok(new PublicDisponibilidadeResponse(dias));
        });

        group.MapGet("/slots", async (
            string slug, Guid profissionalId, Guid servicoId, DateOnly data,
            PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetSlotsAsync(empresaId, profissionalId, servicoId, data, ct));
        });

        group.MapPost("/agendamentos", async (
            string slug, PublicCriarAgendamentoRequest req,
            PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            var result = await svc.CriarAgendamentoAsync(empresaId, req, ct);
            return Results.Created($"/public/{slug}/agendamentos/{result.Id}", result);
        });
    }
}
```

- [ ] **Step 2: Registrar PublicBookingService e UseStaticFiles no Program.cs**

No `Program.cs`, adicione após a linha `builder.Services.AddScoped<CobrancaService>();`:

```csharp
// Services — Public Booking
builder.Services.AddScoped<PublicBookingService>();
```

Adicione o using no topo do arquivo:
```csharp
using GestorAI.API.Services.PublicBooking;
```

Adicione `app.UseStaticFiles();` logo após `app.UseMiddleware<ExceptionMiddleware>();`:
```csharp
app.UseStaticFiles();
```

Adicione `app.MapPublicBooking();` junto com os outros mapeamentos:
```csharp
app.MapPublicBooking();
```

- [ ] **Step 3: Verificar build**

```bash
cd backend
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Endpoints/PublicBookingEndpoints.cs \
        backend/src/GestorAI.API/Program.cs
git commit -m "feat: endpoints públicos de agendamento sem autenticação"
```

---

## Task 5: AgendamentoService — ConfirmarAsync, RecusarAsync, PendentesConfirmacaoAsync

**Files:**
- Modify: `backend/src/GestorAI.API/Services/Agendamentos/AgendamentoService.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/AgendamentosEndpoints.cs`

- [ ] **Step 1: Atualizar ConfirmarAsync para aceitar AguardandoConfirmacao**

No arquivo `AgendamentoService.cs`, substitua o método `ConfirmarAsync`:

```csharp
    public async Task<AgendamentoResponse> ConfirmarAsync(Guid id, CancellationToken ct)
    {
        var a = await FindAsync(id, ct);
        if (a.Status != AgendamentoStatus.Agendado && a.Status != AgendamentoStatus.AguardandoConfirmacao)
            throw new AppException("Apenas agendamentos nos status Agendado ou AguardandoConfirmacao podem ser confirmados.", 400);
        a.Status = AgendamentoStatus.Confirmado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }
```

- [ ] **Step 2: Adicionar RecusarAsync**

No arquivo `AgendamentoService.cs`, adicione após o método `CancelarAsync`:

```csharp
    public async Task<AgendamentoResponse> RecusarAsync(Guid id, CancellationToken ct)
    {
        var a = await FindAsync(id, ct);
        if (a.Status != AgendamentoStatus.AguardandoConfirmacao)
            throw new AppException("Apenas agendamentos AguardandoConfirmacao podem ser recusados.", 400);
        a.Status = AgendamentoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }
```

- [ ] **Step 3: Adicionar PendentesConfirmacaoAsync**

No arquivo `AgendamentoService.cs`, adicione após `RecusarAsync`:

```csharp
    public async Task<List<AgendamentoListItem>> PendentesConfirmacaoAsync(CancellationToken ct) =>
        await db.Agendamentos
            .Include(a => a.Profissional)
            .Include(a => a.Servico)
            .Where(a => a.Status == AgendamentoStatus.AguardandoConfirmacao)
            .OrderBy(a => a.DataHoraInicio)
            .Select(a => new AgendamentoListItem(
                a.Id,
                a.ProfissionalId,
                a.Profissional!.Nome,
                a.ClienteNome,
                a.Servico!.Nome,
                a.DataHoraInicio,
                a.DataHoraFim,
                a.Status))
            .ToListAsync(ct);
```

- [ ] **Step 4: Adicionar endpoints /recusar e /pendentes-confirmacao**

No arquivo `AgendamentosEndpoints.cs`, adicione após o endpoint `/cancelar`:

```csharp
        group.MapPost("/{id:guid}/recusar", async (
            Guid id, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.RecusarAsync(id, ct)));

        group.MapGet("/pendentes-confirmacao", async (
            AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.PendentesConfirmacaoAsync(ct)));
```

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Services/Agendamentos/AgendamentoService.cs \
        backend/src/GestorAI.API/Endpoints/AgendamentosEndpoints.cs
git commit -m "feat: ConfirmarAsync aceita AguardandoConfirmacao, RecusarAsync e pendentes"
```

---

## Task 6: Logo upload + config de branding

**Files:**
- Modify: `backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/FiscalEndpoints.cs`

- [ ] **Step 1: Adicionar SalvarBrandingAsync e UploadLogoAsync em ConfiguracaoEmpresaService**

Primeiro leia o arquivo `ConfiguracaoEmpresaService.cs` para ver a estrutura atual. Em seguida adicione os dois métodos. O serviço recebe `IWebHostEnvironment env` no construtor — atualize o construtor:

```csharp
// Substituir assinatura do construtor (mantenha os campos existentes):
public class ConfiguracaoEmpresaService(AppDbContext db, TenantContext tenantContext, IWebHostEnvironment env)
```

Adicione ao final da classe:

```csharp
    public async Task<ConfiguracaoEmpresaResponse> SalvarBrandingAsync(
        ConfigurarBrandingRequest req, CancellationToken ct)
    {
        // Valida slug único (ignora query filter para checar todos os tenants)
        var slugEmUso = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Slug == req.Slug && c.EmpresaId != tenantContext.EmpresaId, ct);
        if (slugEmUso)
            throw new AppException("Este slug já está em uso.", 400);

        var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct)
            ?? new ConfiguracaoEmpresa { EmpresaId = tenantContext.EmpresaId };

        var isNew = config.Id == Guid.Empty;
        config.Slug = req.Slug;
        config.CorPrimaria = req.CorPrimaria;
        config.DescricaoPublica = req.DescricaoPublica;

        if (isNew) db.ConfiguracoesEmpresa.Add(config);
        await db.SaveChangesAsync(ct);
        return await ObterAsync(ct);
    }

    public async Task<string> UploadLogoAsync(IFormFile file, CancellationToken ct)
    {
        var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!extensoesPermitidas.Contains(ext))
            throw new AppException("Formato inválido. Use jpg, png ou webp.", 400);
        if (file.Length > 2 * 1024 * 1024)
            throw new AppException("Arquivo muito grande. Máximo 2MB.", 400);

        var dir = Path.Combine(env.WebRootPath ?? "wwwroot", "uploads", "logos");
        Directory.CreateDirectory(dir);

        var fileName = $"{tenantContext.EmpresaId}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);

        var logoUrl = $"/uploads/logos/{fileName}";

        var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct)
            ?? new ConfiguracaoEmpresa { EmpresaId = tenantContext.EmpresaId };

        var isNew = config.Id == Guid.Empty;
        config.LogoUrl = logoUrl;
        if (isNew) db.ConfiguracoesEmpresa.Add(config);
        await db.SaveChangesAsync(ct);

        return logoUrl;
    }
```

Adicione o using necessário no topo:
```csharp
using GestorAI.API.DTOs.PublicBooking;
using Microsoft.AspNetCore.Http;
```

- [ ] **Step 2: Atualizar ConfiguracaoEmpresaResponse para incluir campos de branding**

Em `backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs`, adicione os campos ao final do record `ConfiguracaoEmpresaResponse`:

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
    string? DescricaoPublica);
```

Em seguida, leia `ConfiguracaoEmpresaService.cs` e atualize o método `ObterAsync` para mapear os novos campos no retorno do record (adicione `c.Slug, c.LogoUrl, c.CorPrimaria, c.DescricaoPublica` ao final da construção do `ConfiguracaoEmpresaResponse`).

- [ ] **Step 3: Adicionar endpoints de branding em FiscalEndpoints.cs**

No final do grupo de endpoints, antes do `}`, adicione:

```csharp
        group.MapPut("/configuracao-empresa/branding", async (
            ConfigurarBrandingRequest req, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
            Results.Ok(await svc.SalvarBrandingAsync(req, ct)));

        group.MapPost("/configuracao-empresa/logo", async (
            IFormFile file, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
            Results.Ok(new { logoUrl = await svc.UploadLogoAsync(file, ct) }));
```

Adicione `using GestorAI.API.DTOs.PublicBooking;` ao topo de `FiscalEndpoints.cs`.

- [ ] **Step 4: Verificar build**

```bash
cd backend
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj
```

Expected: `Build succeeded.`

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs \
        backend/src/GestorAI.API/Endpoints/FiscalEndpoints.cs \
        backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs
git commit -m "feat: upload de logo, configuração de branding e response atualizado"
```

---

## Task 7: Testes de PublicBookingService

**Files:**
- Create: `backend/tests/GestorAI.Tests/Services/PublicBookingServiceTests.cs`

- [ ] **Step 1: Escrever testes**

```csharp
// backend/tests/GestorAI.Tests/Services/PublicBookingServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.PublicBooking;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.PublicBooking;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class PublicBookingServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, PublicBookingService service) Setup()
    {
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new PublicBookingService(db));
    }

    [Fact]
    public async Task ResolveEmpresaAsync_RetornaEmpresaId_QuandoSlugExiste()
    {
        var (db, svc) = Setup();
        db.ConfiguracoesEmpresa.Add(new ConfiguracaoEmpresa
        {
            EmpresaId = _empresaId,
            Slug = "minha-empresa",
        });
        await db.SaveChangesAsync();

        var result = await svc.ResolveEmpresaAsync("minha-empresa", default);

        Assert.Equal(_empresaId, result);
    }

    [Fact]
    public async Task ResolveEmpresaAsync_Lanca404_QuandoSlugNaoExiste()
    {
        var (_, svc) = Setup();

        await Assert.ThrowsAsync<AppException>(() =>
            svc.ResolveEmpresaAsync("nao-existe", default));
    }

    [Fact]
    public async Task GetServicosAsync_RetornaApenasServicosAtivos()
    {
        var (db, svc) = Setup();
        var catId = Guid.NewGuid();
        db.Categorias.Add(new Categoria { Id = catId, EmpresaId = _empresaId, Nome = "Cat" });
        db.Produtos.AddRange(
            new Produto { EmpresaId = _empresaId, CategoriaId = catId, Nome = "Corte", Tipo = TipoProduto.Servico, Ativo = true, DuracaoMinutos = 30, PrecoVenda = 50 },
            new Produto { EmpresaId = _empresaId, CategoriaId = catId, Nome = "Inativo", Tipo = TipoProduto.Servico, Ativo = false, DuracaoMinutos = 30, PrecoVenda = 50 },
            new Produto { EmpresaId = _empresaId, CategoriaId = catId, Nome = "Produto", Tipo = TipoProduto.Produto, Ativo = true, PrecoVenda = 10 }
        );
        await db.SaveChangesAsync();

        var result = await svc.GetServicosAsync(_empresaId, default);

        Assert.Single(result);
        Assert.Equal("Corte", result[0].Nome);
    }

    [Fact]
    public async Task CriarAgendamentoAsync_CriaComStatusAguardandoConfirmacao()
    {
        var (db, svc) = Setup();
        var catId = Guid.NewGuid();
        db.Categorias.Add(new Categoria { Id = catId, EmpresaId = _empresaId, Nome = "Cat" });
        var profId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        db.Profissionais.Add(new Profissional { Id = profId, EmpresaId = _empresaId, Nome = "Ana" });
        db.Produtos.Add(new Produto { Id = servicoId, EmpresaId = _empresaId, CategoriaId = catId, Nome = "Corte", Tipo = TipoProduto.Servico, Ativo = true, DuracaoMinutos = 30, PrecoVenda = 50 });
        await db.SaveChangesAsync();

        var req = new PublicCriarAgendamentoRequest(
            servicoId, profId,
            new DateTime(2030, 1, 20, 9, 0, 0, DateTimeKind.Utc),
            "João", "11999990001");

        var result = await svc.CriarAgendamentoAsync(_empresaId, req, default);

        Assert.Equal("Corte", result.ServicoNome);
        var agendamento = await db.Agendamentos.IgnoreQueryFilters().FirstAsync();
        Assert.Equal(AgendamentoStatus.AguardandoConfirmacao, agendamento.Status);
    }

    [Fact]
    public async Task CriarAgendamentoAsync_LancaConflito_QuandoHorarioOcupado()
    {
        var (db, svc) = Setup();
        var catId = Guid.NewGuid();
        db.Categorias.Add(new Categoria { Id = catId, EmpresaId = _empresaId, Nome = "Cat" });
        var profId = Guid.NewGuid();
        var servicoId = Guid.NewGuid();
        db.Profissionais.Add(new Profissional { Id = profId, EmpresaId = _empresaId, Nome = "Ana" });
        db.Produtos.Add(new Produto { Id = servicoId, EmpresaId = _empresaId, CategoriaId = catId, Nome = "Corte", Tipo = TipoProduto.Servico, Ativo = true, DuracaoMinutos = 30, PrecoVenda = 50 });
        db.Agendamentos.Add(new Agendamento
        {
            EmpresaId = _empresaId, ProfissionalId = profId, ServicoId = servicoId,
            ClienteNome = "Maria", ClienteTelefone = "11000000001",
            DataHoraInicio = new DateTime(2030, 1, 20, 9, 0, 0, DateTimeKind.Utc),
            DataHoraFim = new DateTime(2030, 1, 20, 9, 30, 0, DateTimeKind.Utc),
            Status = AgendamentoStatus.AguardandoConfirmacao,
        });
        await db.SaveChangesAsync();

        var req = new PublicCriarAgendamentoRequest(
            servicoId, profId,
            new DateTime(2030, 1, 20, 9, 0, 0, DateTimeKind.Utc),
            "João", "11999990001");

        await Assert.ThrowsAsync<AppException>(() =>
            svc.CriarAgendamentoAsync(_empresaId, req, default));
    }
}
```

- [ ] **Step 2: Rodar testes**

```bash
cd backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "PublicBookingServiceTests" -v minimal
```

Expected: `4 passed`.

- [ ] **Step 3: Commit**

```bash
git add backend/tests/GestorAI.Tests/Services/PublicBookingServiceTests.cs
git commit -m "test: PublicBookingService — resolve slug, lista serviços, cria agendamento"
```

---

## Task 8: Frontend — tipos e hook

**Files:**
- Modify: `frontend/src/types/agendamento.ts`
- Modify: `frontend/src/hooks/useAgendamentos.ts`

- [ ] **Step 1: Adicionar AguardandoConfirmacao ao tipo AgendamentoStatus**

No arquivo `frontend/src/types/agendamento.ts`, substitua a primeira linha:

```typescript
export type AgendamentoStatus = 'Agendado' | 'Confirmado' | 'Concluido' | 'Cancelado' | 'AguardandoConfirmacao'
```

- [ ] **Step 2: Adicionar recusar e pendentesConfirmacao ao hook**

No arquivo `frontend/src/hooks/useAgendamentos.ts`, adicione após o método `cancelar`:

```typescript
  const recusar = useCallback(async (id: string) => {
    const result = await api.post<AgendamentoResponse>(`/api/agendamentos/${id}/recusar`, {})
    setAgendamento(result)
  }, [])

  const pendentesConfirmacao = useCallback(async () => {
    return api.get<AgendamentoListItem[]>('/api/agendamentos/pendentes-confirmacao')
  }, [])
```

No `return` do hook, adicione `recusar` e `pendentesConfirmacao`.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/types/agendamento.ts \
        frontend/src/hooks/useAgendamentos.ts
git commit -m "feat: tipo AguardandoConfirmacao e métodos recusar/pendentesConfirmacao"
```

---

## Task 9: publicBookingApi.ts

**Files:**
- Create: `frontend/src/services/publicBookingApi.ts`

- [ ] **Step 1: Criar serviço de API pública (sem token)**

```typescript
// frontend/src/services/publicBookingApi.ts
const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

async function publicRequest<T>(path: string, options: RequestInit = {}): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options.headers },
  })
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }))
    throw new Error((err as { error?: string }).error ?? res.statusText)
  }
  return res.json()
}

export interface PublicEmpresaInfo {
  nome: string
  logoUrl: string | null
  corPrimaria: string | null
  descricao: string | null
}

export interface PublicServicoResponse {
  id: string
  nome: string
  preco: number
  duracaoMinutos: number | null
}

export interface PublicProfissionalResponse {
  id: string
  nome: string
}

export interface PublicAgendamentoConfirmado {
  id: string
  servicoNome: string
  profissionalNome: string
  dataHoraInicio: string
  dataHoraFim: string
}

export interface PublicCriarAgendamentoRequest {
  servicoId: string
  profissionalId: string
  dataHoraInicio: string
  clienteNome: string
  clienteTelefone: string
}

export const publicBookingApi = {
  getInfo: (slug: string) =>
    publicRequest<PublicEmpresaInfo>(`/public/${slug}/info`),

  getServicos: (slug: string) =>
    publicRequest<PublicServicoResponse[]>(`/public/${slug}/servicos`),

  getProfissionais: (slug: string) =>
    publicRequest<PublicProfissionalResponse[]>(`/public/${slug}/profissionais`),

  getDisponibilidade: (slug: string, profissionalId: string) =>
    publicRequest<{ diasComDisponibilidade: number[] }>(
      `/public/${slug}/disponibilidade?profissionalId=${profissionalId}`),

  getSlots: (slug: string, profissionalId: string, servicoId: string, data: string) =>
    publicRequest<string[]>(
      `/public/${slug}/slots?profissionalId=${profissionalId}&servicoId=${servicoId}&data=${data}`),

  criarAgendamento: (slug: string, req: PublicCriarAgendamentoRequest) =>
    publicRequest<PublicAgendamentoConfirmado>(`/public/${slug}/agendamentos`, {
      method: 'POST',
      body: JSON.stringify(req),
    }),
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/services/publicBookingApi.ts
git commit -m "feat: publicBookingApi sem autenticação"
```

---

## Task 10: usePublicBooking hook

**Files:**
- Create: `frontend/src/pages/agendamento-publico/usePublicBooking.ts`

- [ ] **Step 1: Criar hook de estado do wizard**

```typescript
// frontend/src/pages/agendamento-publico/usePublicBooking.ts
import { useState, useEffect, useCallback } from 'react'
import { publicBookingApi } from '@/services/publicBookingApi'
import type {
  PublicEmpresaInfo,
  PublicServicoResponse,
  PublicProfissionalResponse,
  PublicAgendamentoConfirmado,
} from '@/services/publicBookingApi'

export type WizardStep = 'servico' | 'profissional' | 'data' | 'horario' | 'dados' | 'confirmado'

export function usePublicBooking(slug: string) {
  const [step, setStep] = useState<WizardStep>('servico')
  const [info, setInfo] = useState<PublicEmpresaInfo | null>(null)
  const [servicos, setServicos] = useState<PublicServicoResponse[]>([])
  const [profissionais, setProfissionais] = useState<PublicProfissionalResponse[]>([])
  const [diasDisponiveis, setDiasDisponiveis] = useState<number[]>([])
  const [slots, setSlots] = useState<string[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [confirmado, setConfirmado] = useState<PublicAgendamentoConfirmado | null>(null)

  const [servicoSelecionado, setServicoSelecionado] = useState<PublicServicoResponse | null>(null)
  const [profissionalSelecionado, setProfissionalSelecionado] = useState<PublicProfissionalResponse | null>(null)
  const [dataSelecionada, setDataSelecionada] = useState<string | null>(null)
  const [slotSelecionado, setSlotSelecionado] = useState<string | null>(null)

  useEffect(() => {
    async function load() {
      setLoading(true)
      try {
        const [infoData, servicosData] = await Promise.all([
          publicBookingApi.getInfo(slug),
          publicBookingApi.getServicos(slug),
        ])
        setInfo(infoData)
        setServicos(servicosData)
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Erro ao carregar página')
      } finally {
        setLoading(false)
      }
    }
    void load()
  }, [slug])

  const selecionarServico = useCallback(async (servico: PublicServicoResponse) => {
    setServicoSelecionado(servico)
    setLoading(true)
    try {
      const data = await publicBookingApi.getProfissionais(slug)
      setProfissionais(data)
      setStep('profissional')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar profissionais')
    } finally {
      setLoading(false)
    }
  }, [slug])

  const selecionarProfissional = useCallback(async (prof: PublicProfissionalResponse) => {
    setProfissionalSelecionado(prof)
    setLoading(true)
    try {
      const data = await publicBookingApi.getDisponibilidade(slug, prof.id)
      setDiasDisponiveis(data.diasComDisponibilidade)
      setStep('data')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar disponibilidade')
    } finally {
      setLoading(false)
    }
  }, [slug])

  const selecionarData = useCallback(async (data: string) => {
    setDataSelecionada(data)
    if (!profissionalSelecionado || !servicoSelecionado) return
    setLoading(true)
    try {
      const slotsData = await publicBookingApi.getSlots(
        slug, profissionalSelecionado.id, servicoSelecionado.id, data)
      setSlots(slotsData)
      setStep('horario')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar horários')
    } finally {
      setLoading(false)
    }
  }, [slug, profissionalSelecionado, servicoSelecionado])

  const selecionarSlot = useCallback((slot: string) => {
    setSlotSelecionado(slot)
    setStep('dados')
  }, [])

  const confirmar = useCallback(async (clienteNome: string, clienteTelefone: string) => {
    if (!servicoSelecionado || !profissionalSelecionado || !slotSelecionado) return
    setLoading(true)
    try {
      const result = await publicBookingApi.criarAgendamento(slug, {
        servicoId: servicoSelecionado.id,
        profissionalId: profissionalSelecionado.id,
        dataHoraInicio: slotSelecionado,
        clienteNome,
        clienteTelefone,
      })
      setConfirmado(result)
      setStep('confirmado')
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao confirmar agendamento')
    } finally {
      setLoading(false)
    }
  }, [slug, servicoSelecionado, profissionalSelecionado, slotSelecionado])

  const voltar = useCallback(() => {
    const ordem: WizardStep[] = ['servico', 'profissional', 'data', 'horario', 'dados']
    const idx = ordem.indexOf(step as WizardStep)
    if (idx > 0) setStep(ordem[idx - 1])
  }, [step])

  return {
    step, info, servicos, profissionais, diasDisponiveis, slots,
    loading, error, confirmado,
    servicoSelecionado, profissionalSelecionado, dataSelecionada, slotSelecionado,
    selecionarServico, selecionarProfissional, selecionarData, selecionarSlot, confirmar, voltar,
  }
}
```

- [ ] **Step 2: Commit**

```bash
git add frontend/src/pages/agendamento-publico/usePublicBooking.ts
git commit -m "feat: usePublicBooking hook com estado do wizard"
```

---

## Task 11: Página pública AgendamentoPublico.tsx

**Files:**
- Create: `frontend/src/pages/agendamento-publico/AgendamentoPublico.tsx`

- [ ] **Step 1: Criar página wizard pública**

```tsx
// frontend/src/pages/agendamento-publico/AgendamentoPublico.tsx
import { useParams } from 'react-router-dom'
import { usePublicBooking } from './usePublicBooking'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'

const STEPS = ['Serviço', 'Profissional', 'Data', 'Horário', 'Seus dados']
const stepIndex = (s: string) => ['servico', 'profissional', 'data', 'horario', 'dados'].indexOf(s)

const dadosSchema = z.object({
  nome: z.string().min(2, 'Nome obrigatório'),
  telefone: z.string().min(10, 'Telefone inválido'),
})
type DadosForm = z.infer<typeof dadosSchema>

const fmtData = (iso: string) =>
  new Date(iso).toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })
const fmtHora = (iso: string) =>
  new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
const fmtPreco = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

function buildCalendar(year: number, month: number, diasDisponiveis: number[]) {
  const first = new Date(year, month, 1).getDay()
  const days = new Date(year, month + 1, 0).getDate()
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  const cells: Array<{ day: number; date: string; disabled: boolean } | null> = Array(first).fill(null)
  for (let d = 1; d <= days; d++) {
    const date = new Date(year, month, d)
    const dow = date.getDay()
    const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(d).padStart(2, '0')}`
    cells.push({ day: d, date: dateStr, disabled: date < today || !diasDisponiveis.includes(dow) })
  }
  return cells
}

export default function AgendamentoPublico() {
  const { slug } = useParams<{ slug: string }>()
  const bk = usePublicBooking(slug!)

  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<DadosForm>({ resolver: zodResolver(dadosSchema) })

  const cor = bk.info?.corPrimaria ?? '#3B82F6'

  const today = new Date()
  const [calYear, setCalYear] = React.useState(today.getFullYear())
  const [calMonth, setCalMonth] = React.useState(today.getMonth())
  const cells = buildCalendar(calYear, calMonth, bk.diasDisponiveis)

  if (bk.loading && !bk.info) {
    return (
      <div className="flex h-screen items-center justify-center">
        <p className="text-gray-500">Carregando...</p>
      </div>
    )
  }

  if (bk.error && !bk.info) {
    return (
      <div className="flex h-screen items-center justify-center">
        <p className="text-red-500">{bk.error}</p>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header com branding */}
      <div className="shadow-sm" style={{ backgroundColor: cor }}>
        <div className="max-w-lg mx-auto px-4 py-4 flex items-center gap-3">
          {bk.info?.logoUrl && (
            <img src={bk.info.logoUrl} alt="Logo" className="h-10 w-10 rounded-full object-cover" />
          )}
          <div>
            <h1 className="text-white font-bold text-lg leading-tight">{bk.info?.nome}</h1>
            {bk.info?.descricao && (
              <p className="text-white/80 text-xs">{bk.info.descricao}</p>
            )}
          </div>
        </div>
      </div>

      <div className="max-w-lg mx-auto px-4 py-6 space-y-6">
        {/* Barra de progresso */}
        {bk.step !== 'confirmado' && (
          <div className="flex gap-1">
            {STEPS.map((label, i) => (
              <div key={label} className="flex-1">
                <div
                  className="h-1 rounded-full transition-colors"
                  style={{ backgroundColor: i <= stepIndex(bk.step) ? cor : '#E5E7EB' }}
                />
                <p className="text-xs mt-1 text-center text-gray-500 hidden sm:block">{label}</p>
              </div>
            ))}
          </div>
        )}

        {bk.error && bk.step !== 'confirmado' && (
          <p className="text-red-500 text-sm bg-red-50 rounded-md px-3 py-2">{bk.error}</p>
        )}

        {/* Step: Serviço */}
        {bk.step === 'servico' && (
          <div className="space-y-3">
            <h2 className="font-semibold text-gray-800">Escolha o serviço</h2>
            {bk.servicos.map(s => (
              <button
                key={s.id}
                onClick={() => void bk.selecionarServico(s)}
                className="w-full text-left rounded-xl border-2 border-gray-200 p-4 hover:border-current transition-colors"
                style={{ '--tw-ring-color': cor } as React.CSSProperties}
              >
                <div className="flex justify-between items-start">
                  <div>
                    <p className="font-medium text-gray-900">{s.nome}</p>
                    {s.duracaoMinutos && (
                      <p className="text-sm text-gray-500">{s.duracaoMinutos} min</p>
                    )}
                  </div>
                  <p className="font-semibold" style={{ color: cor }}>{fmtPreco(s.preco)}</p>
                </div>
              </button>
            ))}
          </div>
        )}

        {/* Step: Profissional */}
        {bk.step === 'profissional' && (
          <div className="space-y-3">
            <h2 className="font-semibold text-gray-800">Escolha o profissional</h2>
            {bk.profissionais.map(p => (
              <button
                key={p.id}
                onClick={() => void bk.selecionarProfissional(p)}
                className="w-full text-left rounded-xl border-2 border-gray-200 p-4 hover:border-blue-300 transition-colors"
              >
                <p className="font-medium text-gray-900">{p.nome}</p>
              </button>
            ))}
            <button onClick={bk.voltar} className="text-sm text-gray-500 underline">← Voltar</button>
          </div>
        )}

        {/* Step: Data */}
        {bk.step === 'data' && (
          <div className="space-y-3">
            <h2 className="font-semibold text-gray-800">Escolha a data</h2>
            <div className="rounded-xl border bg-white p-4">
              <div className="flex items-center justify-between mb-3">
                <button
                  onClick={() => { if (calMonth === 0) { setCalYear(y => y - 1); setCalMonth(11) } else setCalMonth(m => m - 1) }}
                  className="p-1 hover:bg-gray-100 rounded"
                >‹</button>
                <p className="font-medium text-sm">
                  {new Date(calYear, calMonth).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })}
                </p>
                <button
                  onClick={() => { if (calMonth === 11) { setCalYear(y => y + 1); setCalMonth(0) } else setCalMonth(m => m + 1) }}
                  className="p-1 hover:bg-gray-100 rounded"
                >›</button>
              </div>
              <div className="grid grid-cols-7 gap-1 text-center text-xs text-gray-500 mb-1">
                {['D','S','T','Q','Q','S','S'].map((d, i) => <span key={i}>{d}</span>)}
              </div>
              <div className="grid grid-cols-7 gap-1">
                {cells.map((cell, i) =>
                  cell === null ? <div key={i} /> : (
                    <button
                      key={cell.date}
                      disabled={cell.disabled}
                      onClick={() => void bk.selecionarData(cell.date)}
                      className={`rounded-lg py-2 text-sm font-medium transition-colors ${
                        cell.disabled
                          ? 'text-gray-300 cursor-not-allowed'
                          : 'hover:text-white'
                      } ${bk.dataSelecionada === cell.date ? 'text-white' : ''}`}
                      style={!cell.disabled ? {
                        backgroundColor: bk.dataSelecionada === cell.date ? cor : undefined,
                      } : undefined}
                    >
                      {cell.day}
                    </button>
                  )
                )}
              </div>
            </div>
            <button onClick={bk.voltar} className="text-sm text-gray-500 underline">← Voltar</button>
          </div>
        )}

        {/* Step: Horário */}
        {bk.step === 'horario' && (
          <div className="space-y-3">
            <h2 className="font-semibold text-gray-800">Escolha o horário</h2>
            {bk.slots.length === 0 ? (
              <p className="text-gray-500 text-sm">Nenhum horário disponível para esta data.</p>
            ) : (
              <div className="grid grid-cols-3 gap-2">
                {bk.slots.map(slot => (
                  <button
                    key={slot}
                    onClick={() => bk.selecionarSlot(slot)}
                    className="rounded-lg border-2 py-2 text-sm font-medium transition-colors hover:text-white"
                    style={{ borderColor: cor }}
                    onMouseEnter={e => (e.currentTarget.style.backgroundColor = cor)}
                    onMouseLeave={e => (e.currentTarget.style.backgroundColor = '')}
                  >
                    {fmtHora(slot)}
                  </button>
                ))}
              </div>
            )}
            <button onClick={bk.voltar} className="text-sm text-gray-500 underline">← Voltar</button>
          </div>
        )}

        {/* Step: Dados do cliente */}
        {bk.step === 'dados' && (
          <div className="space-y-4">
            <h2 className="font-semibold text-gray-800">Seus dados</h2>
            <div className="rounded-xl bg-white border p-4 text-sm space-y-1 text-gray-600">
              <p><span className="font-medium">Serviço:</span> {bk.servicoSelecionado?.nome}</p>
              <p><span className="font-medium">Profissional:</span> {bk.profissionalSelecionado?.nome}</p>
              <p><span className="font-medium">Data:</span> {bk.dataSelecionada && fmtData(bk.dataSelecionada + 'T12:00:00')}</p>
              <p><span className="font-medium">Horário:</span> {bk.slotSelecionado && fmtHora(bk.slotSelecionado)}</p>
            </div>
            <form onSubmit={handleSubmit(d => void bk.confirmar(d.nome, d.telefone))} className="space-y-3">
              <div>
                <label className="text-sm font-medium text-gray-700 block mb-1">Nome completo</label>
                <input {...register('nome')} className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2" style={{ '--tw-ring-color': cor } as React.CSSProperties} />
                {errors.nome && <p className="text-xs text-red-500 mt-1">{errors.nome.message}</p>}
              </div>
              <div>
                <label className="text-sm font-medium text-gray-700 block mb-1">Telefone (WhatsApp)</label>
                <input {...register('telefone')} type="tel" placeholder="(11) 99999-0000" className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2" />
                {errors.telefone && <p className="text-xs text-red-500 mt-1">{errors.telefone.message}</p>}
              </div>
              <button
                type="submit"
                disabled={isSubmitting || bk.loading}
                className="w-full py-3 rounded-xl text-white font-semibold transition-opacity disabled:opacity-60"
                style={{ backgroundColor: cor }}
              >
                {bk.loading ? 'Confirmando...' : 'Confirmar Agendamento'}
              </button>
            </form>
            <button onClick={bk.voltar} className="text-sm text-gray-500 underline">← Voltar</button>
          </div>
        )}

        {/* Step: Confirmado */}
        {bk.step === 'confirmado' && bk.confirmado && (
          <div className="text-center space-y-4 py-8">
            <div className="text-5xl">✅</div>
            <h2 className="text-xl font-bold text-gray-800">Solicitação enviada!</h2>
            <p className="text-gray-500 text-sm">
              Seu agendamento foi recebido e está aguardando confirmação da empresa.
              Em breve você receberá um retorno.
            </p>
            <div className="rounded-xl bg-white border p-4 text-sm space-y-1 text-gray-600 text-left">
              <p><span className="font-medium">Serviço:</span> {bk.confirmado.servicoNome}</p>
              <p><span className="font-medium">Profissional:</span> {bk.confirmado.profissionalNome}</p>
              <p><span className="font-medium">Data:</span> {fmtData(bk.confirmado.dataHoraInicio)}</p>
              <p><span className="font-medium">Horário:</span> {fmtHora(bk.confirmado.dataHoraInicio)}</p>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

```

**Atenção:** Mova `import React from 'react'` para o TOPO do arquivo (primeira linha), antes de qualquer outro import.

- [ ] **Step 2: Verificar que o TypeScript compila**

```bash
cd frontend
PATH="/opt/homebrew/Cellar/node/26.0.0/bin:$PATH" npx tsc --noEmit
```

Expected: sem erros.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/agendamento-publico/
git commit -m "feat: página pública de agendamento com wizard em 5 etapas"
```

---

## Task 12: Tela de configuração de branding

**Files:**
- Create: `frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx`

- [ ] **Step 1: Criar tela de configuração**

```tsx
// frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx
import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'

interface BrandingConfig {
  slug: string | null
  corPrimaria: string | null
  descricaoPublica: string | null
  logoUrl: string | null
}

export default function AgendamentoPublicoConfig() {
  const [config, setConfig] = useState<BrandingConfig>({ slug: '', corPrimaria: '#3B82F6', descricaoPublica: '', logoUrl: null })
  const [saving, setSaving] = useState(false)
  const [uploading, setUploading] = useState(false)
  const [copied, setCopied] = useState(false)

  useEffect(() => {
    api.get<{ slug?: string; corPrimaria?: string; descricaoPublica?: string; logoUrl?: string }>('/api/configuracao-empresa')
      .then(data => setConfig({
        slug: data.slug ?? '',
        corPrimaria: data.corPrimaria ?? '#3B82F6',
        descricaoPublica: data.descricaoPublica ?? '',
        logoUrl: data.logoUrl ?? null,
      }))
      .catch(() => {})
  }, [])

  async function handleSave() {
    if (!config.slug) { toast.error('Informe um slug'); return }
    const slugValido = /^[a-z0-9-]+$/.test(config.slug)
    if (!slugValido) { toast.error('Slug deve conter apenas letras minúsculas, números e hífens'); return }
    setSaving(true)
    try {
      await api.put('/api/configuracao-empresa/branding', {
        slug: config.slug,
        corPrimaria: config.corPrimaria,
        descricaoPublica: config.descricaoPublica,
      })
      toast.success('Configurações salvas!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  async function handleLogoUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setUploading(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const token = localStorage.getItem('ga_token')
      const apiBase = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'
      const res = await fetch(`${apiBase}/api/configuracao-empresa/logo`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData,
      })
      if (!res.ok) throw new Error('Erro ao enviar logo')
      const data = await res.json() as { logoUrl: string }
      setConfig(c => ({ ...c, logoUrl: data.logoUrl }))
      toast.success('Logo atualizada!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao enviar logo')
    } finally {
      setUploading(false)
    }
  }

  const link = config.slug ? `${window.location.origin}/agendar/${config.slug}` : null
  const apiBase = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold">Agendamento Online</h1>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Identidade Visual</h2>

        <div className="grid gap-2">
          <Label>Logo</Label>
          <div className="flex items-center gap-4">
            {config.logoUrl && (
              <img src={`${apiBase}${config.logoUrl}`} alt="Logo" className="h-16 w-16 rounded-full object-cover border" />
            )}
            <label className="cursor-pointer">
              <span className="inline-flex items-center px-3 py-1.5 rounded-md border text-sm hover:bg-muted transition-colors">
                {uploading ? 'Enviando...' : 'Escolher arquivo'}
              </span>
              <input type="file" accept=".jpg,.jpeg,.png,.webp" className="hidden" onChange={e => void handleLogoUpload(e)} disabled={uploading} />
            </label>
            <p className="text-xs text-muted-foreground">jpg, png ou webp • máx 2MB</p>
          </div>
        </div>

        <div className="grid gap-2">
          <Label>Cor principal</Label>
          <div className="flex items-center gap-3">
            <input
              type="color"
              value={config.corPrimaria ?? '#3B82F6'}
              onChange={e => setConfig(c => ({ ...c, corPrimaria: e.target.value }))}
              className="h-9 w-16 rounded border cursor-pointer"
            />
            <Input
              value={config.corPrimaria ?? ''}
              onChange={e => setConfig(c => ({ ...c, corPrimaria: e.target.value }))}
              placeholder="#3B82F6"
              className="max-w-28"
            />
          </div>
        </div>

        <div className="grid gap-2">
          <Label>Descrição pública</Label>
          <Input
            value={config.descricaoPublica ?? ''}
            onChange={e => setConfig(c => ({ ...c, descricaoPublica: e.target.value }))}
            placeholder="Ex: Barbearia especializada em cortes modernos"
          />
        </div>
      </div>

      <div className="rounded-md border p-4 space-y-3">
        <h2 className="font-semibold">Link do agendamento</h2>
        <div className="grid gap-2">
          <Label>Slug (identificador único)</Label>
          <div className="flex gap-2 items-center">
            <span className="text-sm text-muted-foreground whitespace-nowrap">/agendar/</span>
            <Input
              value={config.slug ?? ''}
              onChange={e => setConfig(c => ({ ...c, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '') }))}
              placeholder="minha-empresa"
            />
          </div>
          <p className="text-xs text-muted-foreground">Apenas letras minúsculas, números e hífens.</p>
        </div>

        {link && (
          <div className="flex items-center gap-2 bg-muted rounded-md px-3 py-2">
            <p className="text-sm flex-1 truncate font-mono">{link}</p>
            <Button
              size="sm" variant="outline"
              onClick={() => { navigator.clipboard.writeText(link).catch(() => {}); setCopied(true); setTimeout(() => setCopied(false), 2000) }}
            >
              {copied ? 'Copiado!' : 'Copiar'}
            </Button>
          </div>
        )}
      </div>

      <Button onClick={() => void handleSave()} disabled={saving}>
        {saving ? 'Salvando...' : 'Salvar configurações'}
      </Button>
    </div>
  )
}
```

- [ ] **Step 2: Verificar TypeScript**

```bash
cd frontend
PATH="/opt/homebrew/Cellar/node/26.0.0/bin:$PATH" npx tsc --noEmit
```

Expected: sem erros.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx
git commit -m "feat: tela de configuração de branding e link público"
```

---

## Task 13: Router, Sidebar e Agendamentos com pendentes

**Files:**
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`
- Modify: `frontend/src/pages/agendamentos/Agendamentos.tsx`

- [ ] **Step 1: Adicionar rotas no router**

Em `frontend/src/router/index.tsx`, adicione os imports no topo:

```typescript
import AgendamentoPublico from '@/pages/agendamento-publico/AgendamentoPublico'
import AgendamentoPublicoConfig from '@/pages/configuracoes/AgendamentoPublicoConfig'
```

Adicione a rota pública ANTES do grupo com `AppLayout`:

```typescript
  { path: '/agendar/:slug', element: <AgendamentoPublico /> },
```

Adicione a rota de config DENTRO do grupo `AppLayout` (children):

```typescript
      { path: '/configuracoes/agendamento-publico', element: <AgendamentoPublicoConfig /> },
```

- [ ] **Step 2: Adicionar item Configurações na Sidebar**

Em `frontend/src/components/layout/Sidebar.tsx`, localize o array `menuGroups` e adicione um novo grupo ao final, antes do fechamento do array:

```typescript
  {
    label: 'Configurações',
    items: [
      { to: '/configuracoes/agendamento-publico', icon: LinkIcon, label: 'Agendamento Online' },
    ],
  },
```

Adicione o import do ícone no topo (onde estão os outros imports do lucide-react):

```typescript
import { ..., Link as LinkIcon } from 'lucide-react'
```

- [ ] **Step 3: Adicionar seção de pendentes em Agendamentos.tsx**

Em `frontend/src/pages/agendamentos/Agendamentos.tsx`, adicione o status à função `statusClassName`:

```typescript
  if (s === 'AguardandoConfirmacao') return 'bg-yellow-100 text-yellow-700 border-yellow-200'
```

Adicione state e carregamento de pendentes no componente (após os useEffects existentes):

```typescript
  const { recusar, pendentesConfirmacao } = useAgendamentos() // use a separate instance
  const [pendentes, setPendentes] = useState<AgendamentoListItem[]>([])
  const [confirmandoId, setConfirmandoId] = useState<string | null>(null)
  const { confirmar } = useAgendamentos()

  useEffect(() => {
    pendentesConfirmacao().then(setPendentes).catch(() => {})
  }, [pendentesConfirmacao])
```

**Atenção:** `useAgendamentos` tem estado interno — use a instância já existente do hook. Pegue `confirmar`, `recusar` e `pendentesConfirmacao` do mesmo hook `useAgendamentos()` já declarado na linha `const { agendamentos, loading, error, list } = useAgendamentos()`. Ajuste para:

```typescript
  const { agendamentos, loading, error, list, confirmar, recusar, pendentesConfirmacao } = useAgendamentos()
  const [pendentes, setPendentes] = useState<AgendamentoListItem[]>([])
  const [confirmandoId, setConfirmandoId] = useState<string | null>(null)
  const [recusandoId, setRecusandoId] = useState<string | null>(null)

  useEffect(() => {
    pendentesConfirmacao().then(setPendentes).catch(() => {})
  }, [pendentesConfirmacao])
```

Adicione a seção de pendentes ANTES da div com `flex items-center justify-between` (o header):

```tsx
      {pendentes.length > 0 && (
        <div className="rounded-md border border-yellow-300 bg-yellow-50 p-4 space-y-2">
          <h2 className="font-semibold text-yellow-800 text-sm">
            {pendentes.length} agendamento(s) aguardando confirmação
          </h2>
          {pendentes.map(a => (
            <div key={a.id} className="flex items-center justify-between bg-white rounded-md border px-3 py-2 text-sm">
              <div>
                <p className="font-medium">{a.clienteNome}</p>
                <p className="text-muted-foreground text-xs">
                  {a.servicoNome} · {a.profissionalNome} · {new Date(a.dataHoraInicio).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })}
                </p>
              </div>
              <div className="flex gap-2">
                <Button size="sm" disabled={confirmandoId === a.id}
                  onClick={async () => {
                    setConfirmandoId(a.id)
                    try { await confirmar(a.id); setPendentes(p => p.filter(x => x.id !== a.id)) }
                    finally { setConfirmandoId(null) }
                  }}>
                  {confirmandoId === a.id ? '...' : 'Confirmar'}
                </Button>
                <Button size="sm" variant="outline" className="text-destructive hover:text-destructive"
                  disabled={recusandoId === a.id}
                  onClick={async () => {
                    setRecusandoId(a.id)
                    try { await recusar(a.id); setPendentes(p => p.filter(x => x.id !== a.id)) }
                    finally { setRecusandoId(null) }
                  }}>
                  {recusandoId === a.id ? '...' : 'Recusar'}
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}
```

- [ ] **Step 4: Verificar TypeScript**

```bash
cd frontend
PATH="/opt/homebrew/Cellar/node/26.0.0/bin:$PATH" npx tsc --noEmit
```

Expected: sem erros.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/router/index.tsx \
        frontend/src/components/layout/Sidebar.tsx \
        frontend/src/pages/agendamentos/Agendamentos.tsx
git commit -m "feat: rota pública /agendar/:slug, menu Configurações e pendentes na agenda"
```

---

## Task 14: Smoke test e push final

- [ ] **Step 1: Reiniciar backend com todas as mudanças**

```bash
pkill -f "GestorAI.API" 2>/dev/null; sleep 1
cd backend
/usr/local/share/dotnet/dotnet run --project src/GestorAI.API/GestorAI.API.csproj &
sleep 15 && curl -s http://localhost:5297/health
```

Expected: `{"status":"healthy"}`

- [ ] **Step 2: Testar endpoint público (substitua SLUG pelo slug configurado)**

```bash
curl -s http://localhost:5297/public/SLUG/info
```

Expected: JSON com `nome`, `logoUrl`, `corPrimaria`, `descricao`.

- [ ] **Step 3: Verificar frontend em http://localhost:5174/agendar/SLUG**

Acesse a URL no browser. Deve exibir o header com a cor e nome da empresa, e a lista de serviços.

- [ ] **Step 4: Rodar todos os testes**

```bash
cd backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj -v minimal
```

Expected: todos passando.

- [ ] **Step 5: Push final**

```bash
git push origin main
```
