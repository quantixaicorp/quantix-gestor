# Planos de Assinatura por Nicho Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar sistema de planos de assinatura SaaS — cada empresa é associada a um plano que determina quais funcionalidades tem acesso. Planos configuráveis por nicho (ex: Clínica, Salão, Consultório).

**Architecture:** Duas novas entidades (`PlanoSaaS`, `EmpresaPlano`) sem filtro de tenant (são dados globais do SaaS). Um `FeatureService` verifica se uma feature está ativa para o tenant atual. Feature checks são inseridos nos endpoints existentes. Uma página de configurações exibe o plano atual e lista de features.

**Tech Stack:** .NET 10 Minimal APIs, EF Core 10, React + TypeScript

---

## Features disponíveis (enum)

```
assinatura_digital   — ClickSign integration
automacoes_whatsapp  — WhatsApp reminders via Evolution API
asaas_cobrancas      — Asaas PIX/boleto
sinal_reserva        — Booking deposit collection
relatorios_avancados — Advanced reports
nota_fiscal          — NF-e/NFS-e emission
multi_profissional   — More than 1 professional
```

---

## File Map

**Backend — criar:**
- `backend/src/GestorAI.API/Domain/Entities/PlanoSaaS.cs`
- `backend/src/GestorAI.API/Domain/Entities/EmpresaPlano.cs`
- `backend/src/GestorAI.API/Services/PlanoService.cs`
- `backend/src/GestorAI.API/Services/FeatureService.cs`
- `backend/src/GestorAI.API/Endpoints/PlanosEndpoints.cs`
- `backend/src/GestorAI.API/Infrastructure/Data/Migrations/`

**Backend — modificar:**
- `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs` — adicionar DbSets
- `backend/src/GestorAI.API/Program.cs` — registrar serviços

**Frontend — criar:**
- `frontend/src/pages/configuracoes/PlanoAssinatura.tsx`

**Frontend — modificar:**
- `frontend/src/components/layout/Sidebar.tsx` ou router — adicionar link para página de plano

---

### Task 1: Schema — entidades `PlanoSaaS` e `EmpresaPlano`

**Files:**
- Create: `backend/src/GestorAI.API/Domain/Entities/PlanoSaaS.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/EmpresaPlano.cs`
- Modify: `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: Criar `PlanoSaaS.cs`**

```csharp
namespace GestorAI.API.Domain.Entities;

public class PlanoSaaS
{
    public Guid Id { get; set; }
    public required string Nome { get; set; }        // "Básico", "Profissional", "Enterprise"
    public required string Descricao { get; set; }
    public decimal Preco { get; set; }               // preço mensal em R$
    public required string Features { get; set; }   // JSON array: ["asaas_cobrancas","automacoes_whatsapp"]
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<EmpresaPlano> EmpresasPlano { get; set; } = [];
}
```

- [ ] **Step 2: Criar `EmpresaPlano.cs`**

```csharp
namespace GestorAI.API.Domain.Entities;

public class EmpresaPlano
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid PlanoSaaSId { get; set; }
    public DateTime InicioEm { get; set; } = DateTime.UtcNow;
    public DateTime? FimEm { get; set; }
    public bool Ativo { get; set; } = true;
    public PlanoSaaS? Plano { get; set; }
}
```

- [ ] **Step 3: Adicionar DbSets em `AppDbContext.cs`**

Adicionar junto com os outros DbSets:

```csharp
public DbSet<PlanoSaaS> PlanosSaaS => Set<PlanoSaaS>();
public DbSet<EmpresaPlano> EmpresasPlano => Set<EmpresaPlano>();
```

`EmpresaPlano` **não** recebe `HasQueryFilter` — é dado global do SaaS acessado via `IgnoreQueryFilters()` ou diretamente sem filtro (não implementa `ITenantEntity`).

- [ ] **Step 4: Gerar migration**

```bash
cd backend
dotnet ef migrations add AddPlanosSaaS \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API \
  --output-dir Infrastructure/Data/Migrations
dotnet ef database update \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API
```

- [ ] **Step 5: Seed dos planos iniciais**

Verificar se existe seed em `AppDbContext.OnModelCreating`. Adicionar seed básico ou usar uma migration de dados:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... HasQueryFilters existentes ...

    var basicoId = new Guid("10000000-0000-0000-0000-000000000001");
    var profId   = new Guid("10000000-0000-0000-0000-000000000002");
    var entId    = new Guid("10000000-0000-0000-0000-000000000003");

    modelBuilder.Entity<PlanoSaaS>().HasData(
        new PlanoSaaS
        {
            Id = basicoId,
            Nome = "Básico",
            Descricao = "Gestão essencial para pequenos negócios",
            Preco = 97m,
            Features = """["asaas_cobrancas","nota_fiscal"]""",
        },
        new PlanoSaaS
        {
            Id = profId,
            Nome = "Profissional",
            Descricao = "Automações e integrações completas",
            Preco = 197m,
            Features = """["asaas_cobrancas","nota_fiscal","automacoes_whatsapp","assinatura_digital","relatorios_avancados"]""",
        },
        new PlanoSaaS
        {
            Id = entId,
            Nome = "Enterprise",
            Descricao = "Multi-profissional, sinal de reserva, tudo incluso",
            Preco = 397m,
            Features = """["asaas_cobrancas","nota_fiscal","automacoes_whatsapp","assinatura_digital","relatorios_avancados","sinal_reserva","multi_profissional"]""",
        }
    );
}
```

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Entities/PlanoSaaS.cs \
        backend/src/GestorAI.API/Domain/Entities/EmpresaPlano.cs \
        backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs \
        backend/src/GestorAI.API/Infrastructure/Data/Migrations/
git commit -m "feat: add PlanoSaaS and EmpresaPlano entities with seed data"
```

---

### Task 2: Backend — `FeatureService` e `PlanoService`

**Files:**
- Create: `backend/src/GestorAI.API/Services/FeatureService.cs`
- Create: `backend/src/GestorAI.API/Services/PlanoService.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar `FeatureService.cs`**

```csharp
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GestorAI.API.Services;

public class FeatureService(AppDbContext db, TenantContext tenantContext)
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public async Task<bool> HasFeatureAsync(string feature, CancellationToken ct = default)
    {
        var planoAtivo = await db.EmpresasPlano
            .Include(ep => ep.Plano)
            .Where(ep => ep.EmpresaId == tenantContext.EmpresaId && ep.Ativo)
            .OrderByDescending(ep => ep.InicioEm)
            .FirstOrDefaultAsync(ct);

        if (planoAtivo?.Plano is null)
            return true; // sem plano configurado = sem restrição (modo trial/dev)

        var features = JsonSerializer.Deserialize<List<string>>(
            planoAtivo.Plano.Features, _json) ?? [];

        return features.Contains(feature);
    }

    public async Task RequireFeatureAsync(string feature, CancellationToken ct = default)
    {
        if (!await HasFeatureAsync(feature, ct))
            throw new GestorAI.API.Shared.Exceptions.AppException(
                $"Esta funcionalidade não está disponível no seu plano atual.", 402);
    }
}
```

- [ ] **Step 2: Criar `PlanoService.cs`**

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services;

public record PlanoSaaSResponse(
    Guid Id, string Nome, string Descricao, decimal Preco, List<string> Features);

public record EmpresaPlanoAtualResponse(
    Guid PlanoId, string PlanoNome, string PlanDescricao,
    decimal Preco, List<string> Features, DateTime InicioEm);

public class PlanoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<PlanoSaaSResponse>> ListPlanosAsync(CancellationToken ct)
    {
        var planos = await db.PlanosSaaS
            .Where(p => p.Ativo)
            .OrderBy(p => p.Preco)
            .ToListAsync(ct);

        return planos.Select(p => new PlanoSaaSResponse(
            p.Id, p.Nome, p.Descricao, p.Preco,
            System.Text.Json.JsonSerializer.Deserialize<List<string>>(p.Features) ?? []
        )).ToList();
    }

    public async Task<EmpresaPlanoAtualResponse?> GetPlanoAtualAsync(CancellationToken ct)
    {
        var ep = await db.EmpresasPlano
            .Include(e => e.Plano)
            .Where(e => e.EmpresaId == tenantContext.EmpresaId && e.Ativo)
            .OrderByDescending(e => e.InicioEm)
            .FirstOrDefaultAsync(ct);

        if (ep?.Plano is null) return null;

        return new EmpresaPlanoAtualResponse(
            ep.PlanoSaaSId, ep.Plano.Nome, ep.Plano.Descricao, ep.Plano.Preco,
            System.Text.Json.JsonSerializer.Deserialize<List<string>>(ep.Plano.Features) ?? [],
            ep.InicioEm);
    }

    public async Task AtivarPlanoAsync(Guid planoId, CancellationToken ct)
    {
        _ = await db.PlanosSaaS.FirstOrDefaultAsync(p => p.Id == planoId && p.Ativo, ct)
            ?? throw new GestorAI.API.Shared.Exceptions.AppException("Plano não encontrado.", 404);

        // Desativar plano atual
        var atual = await db.EmpresasPlano
            .Where(ep => ep.EmpresaId == tenantContext.EmpresaId && ep.Ativo)
            .ToListAsync(ct);
        foreach (var a in atual)
        {
            a.Ativo = false;
            a.FimEm = DateTime.UtcNow;
        }

        db.EmpresasPlano.Add(new EmpresaPlano
        {
            EmpresaId = tenantContext.EmpresaId,
            PlanoSaaSId = planoId,
        });

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 3: Registrar em `Program.cs`**

```csharp
builder.Services.AddScoped<FeatureService>();
builder.Services.AddScoped<PlanoService>();
```

- [ ] **Step 4: Criar `PlanosEndpoints.cs`**

```csharp
using GestorAI.API.Services;

namespace GestorAI.API.Endpoints;

public static class PlanosEndpoints
{
    public static void MapPlanos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/planos").RequireAuthorization();

        group.MapGet("/", async (PlanoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListPlanosAsync(ct)));

        group.MapGet("/atual", async (PlanoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetPlanoAtualAsync(ct)));

        group.MapPost("/ativar/{planoId:guid}", async (
            Guid planoId, PlanoService svc, CancellationToken ct) =>
        {
            await svc.AtivarPlanoAsync(planoId, ct);
            return Results.Ok(new { sucesso = true });
        });
    }
}
```

- [ ] **Step 5: Registrar em `Program.cs`**

```bash
grep -n "MapContratos\|MapAgendamentos\|MapOrcamentos" backend/src/GestorAI.API/Program.cs | head -5
```

Adicionar `app.MapPlanos();` junto com os outros `Map*`.

- [ ] **Step 6: Build**

```bash
cd backend && dotnet build src/GestorAI.API --no-restore 2>&1 | tail -10
```

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/Services/FeatureService.cs \
        backend/src/GestorAI.API/Services/PlanoService.cs \
        backend/src/GestorAI.API/Endpoints/PlanosEndpoints.cs \
        backend/src/GestorAI.API/Program.cs
git commit -m "feat: add FeatureService, PlanoService, and planos endpoints"
```

---

### Task 3: Backend — feature gates nos endpoints existentes

**Files:**
- Modify: `backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs` — gate `assinatura_digital`
- Modify: `backend/src/GestorAI.API/Endpoints/CobrancasEndpoints.cs` — gate `asaas_cobrancas`
- Modify: `backend/src/GestorAI.API/Services/Automacao/AutomacaoHostedService.cs` — gate `automacoes_whatsapp`

- [ ] **Step 1: Gate em `POST /api/contratos/{id}/enviar-assinatura`**

Em `ContratosEndpoints.cs`, modificar o endpoint de assinatura:

```csharp
group.MapPost("/{id:guid}/enviar-assinatura", async (
    Guid id, EnviarAssinaturaRequest req,
    ContratoService svc, FeatureService features, CancellationToken ct) =>
{
    await features.RequireFeatureAsync("assinatura_digital", ct);
    return Results.Ok(await svc.EnviarAssinaturaAsync(id, req, ct));
});
```

- [ ] **Step 2: Gate em `POST /api/cobrancas/{id}/enviar-asaas`**

Em `CobrancasEndpoints.cs`:

```csharp
group.MapPost("/{id:guid}/enviar-asaas", async (
    Guid id, EnviarAsaasRequest req,
    CobrancaService svc, FeatureService features, CancellationToken ct) =>
{
    await features.RequireFeatureAsync("asaas_cobrancas", ct);
    return Results.Ok(await svc.EnviarAsaasAsync(id, req, ct));
});
```

- [ ] **Step 3: Build**

```bash
cd backend && dotnet build src/GestorAI.API --no-restore 2>&1 | tail -10
```

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs \
        backend/src/GestorAI.API/Endpoints/CobrancasEndpoints.cs
git commit -m "feat: add feature gates to assinatura_digital and asaas_cobrancas endpoints"
```

---

### Task 4: Frontend — página `PlanoAssinatura.tsx`

**Files:**
- Create: `frontend/src/pages/configuracoes/PlanoAssinatura.tsx`
- Modify: router (App.tsx ou Routes) — adicionar rota `/configuracoes/plano`

- [ ] **Step 1: Criar `PlanoAssinatura.tsx`**

```tsx
import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { toast } from '@/hooks/useToast'
import { Check } from 'lucide-react'
import { cn } from '@/lib/utils'

interface PlanoSaaS {
  id: string
  nome: string
  descricao: string
  preco: number
  features: string[]
}

interface PlanoAtual {
  planoId: string
  planoNome: string
  preco: number
  features: string[]
  inicioEm: string
}

const FEATURE_LABELS: Record<string, string> = {
  asaas_cobrancas:      'Cobranças PIX/Boleto (Asaas)',
  automacoes_whatsapp:  'Automações via WhatsApp',
  assinatura_digital:   'Assinatura Digital (ClickSign)',
  sinal_reserva:        'Sinal de Reserva em Agendamentos',
  relatorios_avancados: 'Relatórios Avançados',
  nota_fiscal:          'Emissão de NF-e/NFS-e',
  multi_profissional:   'Múltiplos Profissionais',
}

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function PlanoAssinatura() {
  const [planos, setPlanos] = useState<PlanoSaaS[]>([])
  const [atual, setAtual] = useState<PlanoAtual | null>(null)
  const [ativando, setAtivando] = useState<string | null>(null)

  useEffect(() => {
    void api.get<PlanoSaaS[]>('/api/planos').then(setPlanos).catch(() => {})
    void api.get<PlanoAtual | null>('/api/planos/atual').then(setAtual).catch(() => {})
  }, [])

  async function handleAtivar(planoId: string) {
    setAtivando(planoId)
    try {
      await api.post(`/api/planos/ativar/${planoId}`, {})
      toast.success('Plano atualizado!')
      setAtual(planos.find(p => p.id === planoId) as unknown as PlanoAtual)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao ativar plano')
    } finally {
      setAtivando(null)
    }
  }

  return (
    <div className="space-y-6 max-w-4xl">
      <div>
        <h1 className="text-2xl font-bold">Plano de Assinatura</h1>
        {atual && (
          <p className="text-muted-foreground mt-1">
            Plano atual: <strong>{atual.planoNome}</strong> — {fmt(atual.preco)}/mês
          </p>
        )}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {planos.map(plano => {
          const isAtual = atual?.planoId === plano.id
          return (
            <div key={plano.id} className={cn(
              'rounded-xl border p-6 flex flex-col gap-4',
              isAtual && 'border-primary ring-2 ring-primary/30'
            )}>
              <div>
                <h2 className="text-lg font-bold">{plano.nome}</h2>
                <p className="text-sm text-muted-foreground">{plano.descricao}</p>
                <p className="text-2xl font-bold mt-2">{fmt(plano.preco)}<span className="text-sm font-normal text-muted-foreground">/mês</span></p>
              </div>

              <ul className="space-y-1 flex-1">
                {Object.entries(FEATURE_LABELS).map(([key, label]) => (
                  <li key={key} className={cn('flex items-center gap-2 text-sm',
                    !plano.features.includes(key) && 'text-muted-foreground/50')}>
                    <Check className={cn('h-4 w-4 shrink-0',
                      plano.features.includes(key) ? 'text-green-500' : 'text-muted-foreground/30')} />
                    {label}
                  </li>
                ))}
              </ul>

              <Button
                className="w-full"
                variant={isAtual ? 'outline' : 'default'}
                disabled={isAtual || ativando !== null}
                onClick={() => void handleAtivar(plano.id)}
              >
                {isAtual ? 'Plano Atual' : ativando === plano.id ? 'Ativando...' : 'Escolher Plano'}
              </Button>
            </div>
          )
        })}
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Adicionar rota**

```bash
grep -n "configuracoes\|Integracoes\|Route" frontend/src/App.tsx | head -20
```

Adicionar rota `/configuracoes/plano` → `<PlanoAssinatura />` junto com as outras rotas de configurações.

- [ ] **Step 3: Build TypeScript**

```bash
cd frontend && npx tsc --noEmit 2>&1 | tail -20
```

- [ ] **Step 4: Commit**

```bash
git add frontend/src/pages/configuracoes/PlanoAssinatura.tsx \
        frontend/src/App.tsx
git commit -m "feat: add subscription plan page with feature comparison"
```

---

## Checklist final

- [ ] `PlanoSaaS` e `EmpresaPlano` criados e migração aplicada
- [ ] Seed dos 3 planos (Básico, Profissional, Enterprise)
- [ ] `GET /api/planos` retorna lista de planos ativos
- [ ] `GET /api/planos/atual` retorna plano do tenant atual
- [ ] `POST /api/planos/ativar/{id}` troca o plano do tenant
- [ ] `FeatureService.RequireFeatureAsync` retorna HTTP 402 quando feature não disponível
- [ ] Feature gates em `enviar-assinatura` e `enviar-asaas`
- [ ] Página `PlanoAssinatura` exibe os 3 planos com comparativo de features
- [ ] Plano atual destacado visualmente
- [ ] `dotnet build` e `npx tsc --noEmit` sem erros
