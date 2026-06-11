# Billing SaaS + White Label Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Cobrar clientes SaaS mensalmente via Asaas Marketplace e permitir white label por empresa — logo, cor primária e domínio personalizado.

**Architecture:** O billing SaaS usa a mesma `AsaasService` já integrada mas em modo "marketplace" (cobrança recorrente por empresa). White label: `ConfiguracaoEmpresa` já tem `LogoUrl`, `CorPrimaria` e `Slug` — adicionar `DominioCustomizado`. O frontend aplica branding dinamicamente via CSS variables. Roteamento por domínio customizado é feito no proxy (Caddy/nginx) com wildcard DNS — o backend apenas recebe o host e resolve o tenant.

**Tech Stack:** .NET 10, EF Core 10, Asaas Marketplace API, React + TypeScript, Tailwind CSS

---

## Dependências

- Conta Asaas Marketplace (conta "mãe" para cobrar sub-contas)
- Asaas API Key da conta marketplace (diferente da conta do cliente)
- DNS wildcard configurado (ex: `*.gestorai.com.br`)
- Proxy reverso (Caddy ou nginx) com wildcard TLS

---

## File Map

**Backend — criar:**
- `backend/src/GestorAI.API/Services/BillingService.cs`
- `backend/src/GestorAI.API/Endpoints/BillingEndpoints.cs`
- `backend/src/GestorAI.API/Services/TenantResolutionService.cs`

**Backend — modificar:**
- `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs` — adicionar `DominioCustomizado`, `AsaasClienteId`, `AssinaturaAsaasId`
- `backend/src/GestorAI.API/Shared/MultiTenancy/TenantContext.cs` — suporte a resolução por domínio
- `backend/src/GestorAI.API/Program.cs` — registrar serviços, middleware de resolução por host

**Frontend — criar:**
- `frontend/src/pages/configuracoes/WhiteLabel.tsx`

**Frontend — modificar:**
- `frontend/src/App.tsx` — aplicar CSS variables de branding na inicialização
- `frontend/src/services/api.ts` — verificar se já inclui headers de tenant

---

### Task 1: Schema — campos de billing e white label em `ConfiguracaoEmpresa`

**Files:**
- Modify: `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs`

- [ ] **Step 1: Adicionar campos**

Adicionar ao final de `ConfiguracaoEmpresa.cs`:

```csharp
// White label — domínio customizado
public string? DominioCustomizado { get; set; }

// Billing SaaS — Asaas Marketplace
public string? AsaasClienteIdSaaS { get; set; }     // ID do cliente na conta marketplace
public string? AssinaturaAsaasId { get; set; }      // ID da assinatura recorrente na Asaas
public string? StatusAssinatura { get; set; }       // "Ativo" | "Inadimplente" | "Cancelado"
public DateTime? ProximaCobrancaEm { get; set; }
```

- [ ] **Step 2: Gerar migration**

```bash
cd backend
dotnet ef migrations add AddBillingSaaS \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API \
  --output-dir Infrastructure/Data/Migrations
dotnet ef database update \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API
```

- [ ] **Step 3: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs \
        backend/src/GestorAI.API/Infrastructure/Data/Migrations/
git commit -m "feat: add billing and custom domain fields to ConfiguracaoEmpresa"
```

---

### Task 2: Backend — `BillingService` (cobrança recorrente via Asaas Marketplace)

**Files:**
- Create: `backend/src/GestorAI.API/Services/BillingService.cs`

A Asaas Marketplace permite que a conta mãe crie assinaturas recorrentes para clientes. O `BillingService` encapsula:
1. `CriarAssinaturaAsync` — cria cliente + assinatura mensal para uma empresa
2. `CancelarAssinaturaAsync` — cancela assinatura
3. `ProcessarWebhookAsync` — atualiza `StatusAssinatura` baseado em eventos Asaas

A API Key da conta marketplace fica em `appsettings.json` como `Asaas:MarketplaceApiKey` — **não** na `ConfiguracaoEmpresa` (que é a API key do cliente).

- [ ] **Step 1: Adicionar config em `appsettings.json`**

```bash
grep -n "Asaas\|ConnectionStrings\|Jwt" backend/src/GestorAI.API/appsettings.json | head -10
```

Adicionar seção:

```json
"SaaS": {
  "AsaasMarketplaceApiKey": "",
  "AsaasMarketplaceSandbox": true,
  "AsaasMarketplaceBaseUrl": "https://sandbox.asaas.com/api/v3"
}
```

- [ ] **Step 2: Criar `BillingService.cs`**

```csharp
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Asaas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GestorAI.API.Services;

public class BillingService(
    AppDbContext db,
    AsaasService asaasService,
    IConfiguration config)
{
    private string ApiKey => config["SaaS:AsaasMarketplaceApiKey"] ?? "";
    private bool Sandbox => bool.Parse(config["SaaS:AsaasMarketplaceSandbox"] ?? "true");

    public async Task<string> CriarAssinaturaAsync(
        Guid empresaId, string nomeEmpresa, string emailEmpresa,
        decimal valor, CancellationToken ct)
    {
        var configEmpresa = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId, ct)
            ?? throw new Exception("Configuração da empresa não encontrada.");

        // Criar ou recuperar cliente Asaas
        var clienteId = configEmpresa.AsaasClienteIdSaaS
            ?? await asaasService.GetOrCreateCustomerAsync(
                ApiKey, Sandbox, nomeEmpresa, emailEmpresa, ct);

        // Criar assinatura mensal
        var assinaturaId = await CriarAssinaturaAsaasAsync(clienteId, valor, ct);

        configEmpresa.AsaasClienteIdSaaS = clienteId;
        configEmpresa.AssinaturaAsaasId = assinaturaId;
        configEmpresa.StatusAssinatura = "Ativo";
        configEmpresa.ProximaCobrancaEm = DateTime.UtcNow.AddMonths(1);
        await db.SaveChangesAsync(ct);

        return assinaturaId;
    }

    public async Task CancelarAssinaturaAsync(Guid empresaId, CancellationToken ct)
    {
        var configEmpresa = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId, ct);

        if (configEmpresa?.AssinaturaAsaasId is null) return;

        // Chamar Asaas para cancelar assinatura
        // DELETE /subscriptions/{id}
        using var http = new HttpClient();
        var baseUrl = Sandbox ? "https://sandbox.asaas.com/api/v3" : "https://api.asaas.com/v3";
        http.DefaultRequestHeaders.Add("access_token", ApiKey);
        await http.DeleteAsync($"{baseUrl}/subscriptions/{configEmpresa.AssinaturaAsaasId}", ct);

        configEmpresa.StatusAssinatura = "Cancelado";
        await db.SaveChangesAsync(ct);
    }

    public async Task ProcessarWebhookAsync(string assinaturaId, string evento, CancellationToken ct)
    {
        var configEmpresa = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.AssinaturaAsaasId == assinaturaId, ct);

        if (configEmpresa is null) return;

        configEmpresa.StatusAssinatura = evento switch
        {
            "PAYMENT_RECEIVED" or "PAYMENT_CONFIRMED" => "Ativo",
            "PAYMENT_OVERDUE" => "Inadimplente",
            "SUBSCRIPTION_DELETED" => "Cancelado",
            _ => configEmpresa.StatusAssinatura,
        };

        if (evento == "PAYMENT_CONFIRMED")
            configEmpresa.ProximaCobrancaEm = DateTime.UtcNow.AddMonths(1);

        await db.SaveChangesAsync(ct);
    }

    private async Task<string> CriarAssinaturaAsaasAsync(string clienteId, decimal valor, CancellationToken ct)
    {
        using var http = new HttpClient();
        var baseUrl = Sandbox ? "https://sandbox.asaas.com/api/v3" : "https://api.asaas.com/v3";
        http.DefaultRequestHeaders.Add("access_token", ApiKey);

        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            customer = clienteId,
            billingType = "BOLETO",
            value = valor,
            nextDueDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
            cycle = "MONTHLY",
            description = "Assinatura GestorAI",
        });

        var resp = await http.PostAsync($"{baseUrl}/subscriptions",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"), ct);
        resp.EnsureSuccessStatusCode();

        var json = await System.Text.Json.JsonSerializer.DeserializeAsync<System.Text.Json.JsonElement>(
            await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return json.GetProperty("id").GetString()!;
    }
}
```

- [ ] **Step 3: Criar `BillingEndpoints.cs`**

```csharp
using GestorAI.API.Services;

namespace GestorAI.API.Endpoints;

public static class BillingEndpoints
{
    public static void MapBilling(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/billing").RequireAuthorization();

        group.MapGet("/status", async (
            GestorAI.API.Shared.MultiTenancy.TenantContext tc,
            GestorAI.API.Infrastructure.Data.AppDbContext db,
            CancellationToken ct) =>
        {
            var cfg = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct);
            return Results.Ok(new
            {
                status = cfg?.StatusAssinatura ?? "SemPlano",
                proximaCobranca = cfg?.ProximaCobrancaEm,
            });
        });

        // Webhook Asaas — sem autenticação
        app.MapPost("/webhook/asaas-billing", async (
            System.Text.Json.JsonElement body,
            BillingService svc, CancellationToken ct) =>
        {
            var assinaturaId = body.GetProperty("subscription").GetProperty("id").GetString() ?? "";
            var evento = body.GetProperty("event").GetString() ?? "";
            await svc.ProcessarWebhookAsync(assinaturaId, evento, ct);
            return Results.Ok();
        });
    }
}
```

**Nota:** O endpoint do webhook (`/webhook/asaas-billing`) não usa `.RequireAuthorization()` pois é chamado pelo Asaas. Adicionar verificação de IP de origem do Asaas em produção.

- [ ] **Step 4: Registrar em `Program.cs`**

```csharp
builder.Services.AddScoped<BillingService>();
```

E adicionar `app.MapBilling();`.

- [ ] **Step 5: Build**

```bash
cd backend && dotnet build src/GestorAI.API --no-restore 2>&1 | tail -15
```

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Services/BillingService.cs \
        backend/src/GestorAI.API/Endpoints/BillingEndpoints.cs \
        backend/src/GestorAI.API/Program.cs \
        backend/src/GestorAI.API/appsettings.json
git commit -m "feat: add BillingService for Asaas Marketplace subscription management"
```

---

### Task 3: Backend — resolução de tenant por domínio customizado

**Files:**
- Create: `backend/src/GestorAI.API/Services/TenantResolutionService.cs`
- Modify: `backend/src/GestorAI.API/Shared/MultiTenancy/TenantContext.cs` (verificar estrutura atual)

O tenant hoje é resolvido via JWT. Para white label, quando o cliente acessa `minha-clinica.gestorai.com.br`, o frontend aponta para a mesma API mas o host indica o tenant. O middleware atual deve já ter um fallback; esta task adiciona resolução por `X-Tenant-Domain` header ou `Host` header.

- [ ] **Step 1: Verificar implementação atual do tenant middleware**

```bash
cat backend/src/GestorAI.API/Shared/MultiTenancy/TenantContext.cs
grep -n "TenantMiddleware\|EmpresaId\|X-Tenant\|Host" backend/src/GestorAI.API/Program.cs | head -20
grep -rn "TenantMiddleware\|ITenantResolver" backend/src/GestorAI.API/ | head -10
```

- [ ] **Step 2: Criar `TenantResolutionService.cs`**

```csharp
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services;

public class TenantResolutionService(AppDbContext db)
{
    public async Task<Guid?> ResolveByDomainAsync(string host, CancellationToken ct)
    {
        // Remove porta e normaliza
        var dominio = host.Split(':')[0].ToLowerInvariant();

        var config = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.DominioCustomizado == dominio ||
                c.Slug == dominio.Replace(".gestorai.com.br", ""), ct);

        return config?.EmpresaId;
    }
}
```

- [ ] **Step 3: Integrar resolução por domínio no middleware de tenant**

Verificar como o `TenantContext.EmpresaId` é populado atualmente (provavelmente via JWT claim). Adicionar fallback: se o JWT não tem `EmpresaId`, tentar resolver pelo `Host` header usando `TenantResolutionService`.

No middleware ou no início de cada request (via `IMiddleware` ou antes da autenticação):

```csharp
// Em Program.cs, adicionar antes de app.UseAuthorization():
app.Use(async (ctx, next) =>
{
    if (ctx.User.Identity?.IsAuthenticated == true)
    {
        await next();
        return;
    }
    // Para rotas públicas com domínio customizado
    var host = ctx.Request.Host.Host;
    var resolver = ctx.RequestServices.GetRequiredService<TenantResolutionService>();
    var empresaId = await resolver.ResolveByDomainAsync(host, ctx.RequestAborted);
    if (empresaId.HasValue)
    {
        var tenantCtx = ctx.RequestServices.GetRequiredService<GestorAI.API.Shared.MultiTenancy.TenantContext>();
        tenantCtx.EmpresaId = empresaId.Value;
    }
    await next();
});
```

- [ ] **Step 4: Registrar e build**

```csharp
builder.Services.AddScoped<TenantResolutionService>();
```

```bash
cd backend && dotnet build src/GestorAI.API --no-restore 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Services/TenantResolutionService.cs \
        backend/src/GestorAI.API/Program.cs
git commit -m "feat: add tenant resolution by custom domain for white label"
```

---

### Task 4: Frontend — white label e página `WhiteLabel.tsx`

**Files:**
- Create: `frontend/src/pages/configuracoes/WhiteLabel.tsx`
- Modify: `frontend/src/App.tsx` — aplicar CSS variables de branding

- [ ] **Step 1: Criar `WhiteLabel.tsx`**

```tsx
import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'

interface WhiteLabelConfig {
  slug: string
  logoUrl: string
  corPrimaria: string
  descricaoPublica: string
  dominioCustomizado: string
}

export default function WhiteLabel() {
  const [config, setConfig] = useState<WhiteLabelConfig>({
    slug: '', logoUrl: '', corPrimaria: '#2563eb',
    descricaoPublica: '', dominioCustomizado: '',
  })
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    api.get<Partial<WhiteLabelConfig>>('/api/configuracao-empresa')
      .then(d => setConfig({
        slug: d.slug ?? '',
        logoUrl: d.logoUrl ?? '',
        corPrimaria: d.corPrimaria ?? '#2563eb',
        descricaoPublica: d.descricaoPublica ?? '',
        dominioCustomizado: d.dominioCustomizado ?? '',
      }))
      .catch(() => {})
  }, [])

  async function handleSave() {
    setSaving(true)
    try {
      await api.put('/api/configuracao-empresa/white-label', config)
      toast.success('Configurações salvas!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold">White Label</h1>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Identidade Visual</h2>

        <div className="grid gap-2">
          <Label>Slug público (URL da agenda)</Label>
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground">gestorai.com.br/</span>
            <Input
              value={config.slug}
              onChange={e => setConfig(c => ({ ...c, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '') }))}
              placeholder="minha-empresa"
            />
          </div>
        </div>

        <div className="grid gap-2">
          <Label>URL da Logo</Label>
          <Input
            value={config.logoUrl}
            onChange={e => setConfig(c => ({ ...c, logoUrl: e.target.value }))}
            placeholder="https://..."
          />
          {config.logoUrl && (
            <img src={config.logoUrl} alt="Logo" className="h-12 object-contain" />
          )}
        </div>

        <div className="grid gap-2">
          <Label>Cor Primária</Label>
          <div className="flex items-center gap-2">
            <input
              type="color"
              value={config.corPrimaria}
              onChange={e => setConfig(c => ({ ...c, corPrimaria: e.target.value }))}
              className="h-9 w-16 rounded border cursor-pointer"
            />
            <Input
              value={config.corPrimaria}
              onChange={e => setConfig(c => ({ ...c, corPrimaria: e.target.value }))}
              className="w-32"
              placeholder="#2563eb"
            />
          </div>
        </div>

        <div className="grid gap-2">
          <Label>Descrição pública</Label>
          <Input
            value={config.descricaoPublica}
            onChange={e => setConfig(c => ({ ...c, descricaoPublica: e.target.value }))}
            placeholder="Clínica de estética e bem-estar..."
          />
        </div>
      </div>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Domínio Customizado</h2>
        <p className="text-sm text-muted-foreground">
          Configure um domínio próprio para o painel e página pública de agendamentos.
          Após configurar, aponte um CNAME para <code className="bg-muted px-1 rounded">gestorai.com.br</code>.
        </p>
        <div className="grid gap-2">
          <Label>Domínio</Label>
          <Input
            value={config.dominioCustomizado}
            onChange={e => setConfig(c => ({ ...c, dominioCustomizado: e.target.value }))}
            placeholder="agenda.minha-clinica.com.br"
          />
        </div>
      </div>

      <Button onClick={() => void handleSave()} disabled={saving}>
        {saving ? 'Salvando...' : 'Salvar'}
      </Button>
    </div>
  )
}
```

- [ ] **Step 2: Adicionar endpoint `PUT /api/configuracao-empresa/white-label`**

Verificar `ConfiguracaoEmpresaEndpoints.cs` ou equivalente:

```bash
grep -n "white-label\|PUT.*configuracao\|integracoes" backend/src/GestorAI.API/Endpoints/*.cs | head -20
```

Se o endpoint `PUT /api/configuracao-empresa/integracoes` já existe com um padrão, criar `PUT /api/configuracao-empresa/white-label` com o mesmo padrão, aceitando: `slug`, `logoUrl`, `corPrimaria`, `descricaoPublica`, `dominioCustomizado`.

- [ ] **Step 3: Aplicar CSS variables de branding em `App.tsx`**

No início de `App.tsx`, após autenticação, buscar configuração de branding e aplicar:

```typescript
useEffect(() => {
  api.get<{ corPrimaria?: string; logoUrl?: string }>('/api/configuracao-empresa')
    .then(cfg => {
      if (cfg.corPrimaria) {
        document.documentElement.style.setProperty('--color-primary', cfg.corPrimaria)
        // Converter hex para HSL para Tailwind CSS variables (simplificado)
      }
    })
    .catch(() => {})
}, [isAuthenticated])
```

**Nota:** Tailwind usa variáveis HSL (`--primary: 217 91% 60%`). Para aplicar cor customizada corretamente, converter o hex da `corPrimaria` para HSL. Criar função `hexToHsl(hex: string): string` ou usar uma biblioteca.

Função de conversão:

```typescript
function hexToHsl(hex: string): string {
  const r = parseInt(hex.slice(1, 3), 16) / 255
  const g = parseInt(hex.slice(3, 5), 16) / 255
  const b = parseInt(hex.slice(5, 7), 16) / 255
  const max = Math.max(r, g, b), min = Math.min(r, g, b)
  let h = 0, s = 0
  const l = (max + min) / 2
  if (max !== min) {
    const d = max - min
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min)
    h = max === r ? ((g - b) / d + (g < b ? 6 : 0)) / 6
      : max === g ? ((b - r) / d + 2) / 6
      : ((r - g) / d + 4) / 6
  }
  return `${Math.round(h * 360)} ${Math.round(s * 100)}% ${Math.round(l * 100)}%`
}
```

- [ ] **Step 4: Adicionar rota `/configuracoes/white-label`**

Adicionar rota em `App.tsx` ou no arquivo de rotas do projeto.

- [ ] **Step 5: Build TypeScript**

```bash
cd frontend && npx tsc --noEmit 2>&1 | tail -20
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/pages/configuracoes/WhiteLabel.tsx \
        frontend/src/App.tsx
git commit -m "feat: add white label configuration page with branding and custom domain"
```

---

## Notas de Infraestrutura (fora do código)

Para white label com domínio customizado funcionar em produção:

1. **DNS Wildcard**: `*.gestorai.com.br` → IP do servidor
2. **Proxy reverso (Caddy)**: configurar `wildcard TLS` + `header_up Host {host}` para preservar o host original
3. **Certificado TLS wildcard**: Let's Encrypt via Caddy ACME DNS challenge
4. **Domínio customizado do cliente**: o cliente cria um CNAME `agenda.clinica.com.br → gestorai.com.br`; o proxy aceita qualquer hostname e roteia para a mesma instância, passando o `Host` original ao backend

Exemplo de `Caddyfile` para multi-tenant:
```
*.gestorai.com.br {
  tls {
    dns cloudflare {env.CF_TOKEN}
  }
  reverse_proxy localhost:5000
}
```

---

## Checklist final

- [ ] Campos `DominioCustomizado`, `AsaasClienteIdSaaS`, `AssinaturaAsaasId`, `StatusAssinatura` adicionados e migração aplicada
- [ ] `BillingService.CriarAssinaturaAsync` cria assinatura mensal na Asaas Marketplace
- [ ] `BillingService.ProcessarWebhookAsync` atualiza status ao receber evento Asaas
- [ ] Webhook `POST /webhook/asaas-billing` funcional (sem auth)
- [ ] `TenantResolutionService.ResolveByDomainAsync` resolve tenant por domínio/slug
- [ ] Middleware aplica tenant resolution por host para rotas públicas
- [ ] `GET /api/billing/status` retorna status da assinatura do tenant
- [ ] Página `WhiteLabel.tsx` salva logo, cor primária, slug e domínio customizado
- [ ] CSS variable `--primary` aplicada dinamicamente no `App.tsx`
- [ ] Endpoint `PUT /api/configuracao-empresa/white-label` funcional
- [ ] `dotnet build` e `npx tsc --noEmit` sem erros
