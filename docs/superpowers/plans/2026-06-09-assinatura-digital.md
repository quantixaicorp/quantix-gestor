# Assinatura Digital (ClickSign) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Permitir que contratos sejam enviados para assinatura digital via ClickSign, com rastreamento de status (Pendente → Assinado) e link para visualização do documento.

**Architecture:** Adicionar 3 campos a `Contrato` (sem nova entidade): `ClickSignDocKey`, `ClickSignStatus`, `ClickSignViewerUrl`. `ClickSignService` encapsula a API REST da ClickSign. Um único endpoint `POST /api/contratos/{id}/enviar-assinatura` inicia o processo. O frontend exibe badge de status e botão de envio em `DetalheContrato`.

**Tech Stack:** .NET 10 Minimal APIs, EF Core 10 migrations, ClickSign REST API, React + TypeScript

---

## Pré-requisitos

Para usar ClickSign é necessário:
1. Conta ClickSign com plano API (sandbox disponível em sandbox.clicksign.com)
2. API Key obtida em Configurações → API
3. A empresa configura a API key em `Configurações → Integrações`

---

## File Map

**Backend — criar:**
- `backend/src/GestorAI.API/Services/Contratos/ClickSignService.cs`

**Backend — modificar:**
- `backend/src/GestorAI.API/Domain/Entities/Contrato.cs` — adicionar 3 campos de assinatura
- `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs` — adicionar `ClickSignApiKey` e `ClickSignSandbox`
- `backend/src/GestorAI.API/DTOs/Contratos/ContratoDto.cs` — adicionar `ClickSignStatus` e `ClickSignViewerUrl` em `ContratoResponse`
- `backend/src/GestorAI.API/Services/Contratos/ContratoService.cs` — incluir campos de assinatura em `ToResponse`
- `backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs` — adicionar endpoint `POST /{id}/enviar-assinatura`
- `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs` — EF migration

**Backend — criar (configuração):**
- `backend/src/GestorAI.API/DTOs/Contratos/ContratoDto.cs` — adicionar `EnviarAssinaturaResponse`

**Frontend — modificar:**
- `frontend/src/pages/contratos/DetalheContrato.tsx` — adicionar botão "Enviar para Assinatura" e badge de status
- `frontend/src/hooks/useContratos.ts` — adicionar `enviarAssinatura`
- `frontend/src/types/contrato.ts` — adicionar campos de assinatura em `ContratoResponse`
- `frontend/src/pages/configuracoes/Integracoes.tsx` — adicionar campos ClickSign

---

### Task 1: Schema — adicionar campos de assinatura em `Contrato` e `ConfiguracaoEmpresa`

**Files:**
- Modify: `backend/src/GestorAI.API/Domain/Entities/Contrato.cs`
- Modify: `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs`

- [ ] **Step 1: Adicionar campos em `Contrato.cs`**

Adicionar após `public string? Observacao { get; set; }`:

```csharp
// Assinatura digital (ClickSign)
public string? ClickSignDocKey { get; set; }
public string? ClickSignStatus { get; set; }   // null | "Pendente" | "Assinado"
public string? ClickSignViewerUrl { get; set; }
```

- [ ] **Step 2: Adicionar campos em `ConfiguracaoEmpresa.cs`**

Adicionar após `public bool AsaasSandbox { get; set; } = true;`:

```csharp
// ClickSign (Assinatura Digital)
public string? ClickSignApiKey { get; set; }
public bool ClickSignSandbox { get; set; } = true;
```

- [ ] **Step 3: Gerar e aplicar migration**

```bash
cd backend
dotnet ef migrations add AddAssinaturaDigital \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API \
  --output-dir Infrastructure/Data/Migrations
dotnet ef database update \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API
```

Esperado: migration criada e aplicada sem erros.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Entities/Contrato.cs \
        backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs \
        backend/src/GestorAI.API/Infrastructure/Data/Migrations/
git commit -m "feat: add ClickSign fields to Contrato and ConfiguracaoEmpresa"
```

---

### Task 2: Backend — `ClickSignService`

**Files:**
- Create: `backend/src/GestorAI.API/Services/Contratos/ClickSignService.cs`

A ClickSign API funciona com JSON. Endpoints relevantes:
- `POST /api/v1/documents` — cria documento a partir de base64
- `POST /api/v1/lists` — cria lista de assinaturas (signatários)
- `POST /api/v1/lists/{list_key}/signers` — adiciona signatário
- `POST /api/v1/lists/{list_key}/notifications` — envia notificação por email

URL sandbox: `https://sandbox.clicksign.com`  
URL produção: `https://app.clicksign.com`

Authentication: `access_token` como query param em todas as requisições.

- [ ] **Step 1: Criar `ClickSignService.cs`**

```csharp
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestorAI.API.Services.Contratos;

public class ClickSignService(IHttpClientFactory httpFactory)
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private HttpClient CreateClient(string apiKey, bool sandbox)
    {
        var baseUrl = sandbox
            ? "https://sandbox.clicksign.com"
            : "https://app.clicksign.com";

        var client = httpFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        // access_token é passado como query param — não como header
        _ = apiKey; // usado na URL
        return client;
    }

    public async Task<ClickSignDocResult> CriarDocumentoAsync(
        string apiKey, bool sandbox,
        string nomeArquivo, byte[] pdfBytes,
        CancellationToken ct)
    {
        var client = CreateClient(apiKey, sandbox);
        var base64 = Convert.ToBase64String(pdfBytes);

        var body = new
        {
            document = new
            {
                path = $"/{nomeArquivo}",
                content_base64 = $"data:application/pdf;base64,{base64}",
                deadline_at = (string?)null,
                auto_close = true,
                locale = "pt-BR",
                sequence_enabled = false,
            }
        };

        var url = $"/api/v1/documents?access_token={Uri.EscapeDataString(apiKey)}";
        var response = await client.PostAsJsonAsync(url, body, _json, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var docKey = json.GetProperty("document").GetProperty("key").GetString()!;
        var docViewerUrl = json.GetProperty("document").GetProperty("url").GetString();

        return new ClickSignDocResult(docKey, docViewerUrl ?? "");
    }

    public async Task AdicionarSignatarioAsync(
        string apiKey, bool sandbox,
        string docKey, string nomeSignatario, string emailSignatario,
        CancellationToken ct)
    {
        var client = CreateClient(apiKey, sandbox);

        // 1. Criar signatário
        var signerBody = new
        {
            signer = new
            {
                email = emailSignatario,
                phone_number = (string?)null,
                auth_type = "email",
                delivery = "email",
                name = nomeSignatario,
                has_documentation = false,
            }
        };

        var createUrl = $"/api/v1/signers?access_token={Uri.EscapeDataString(apiKey)}";
        var signerResp = await client.PostAsJsonAsync(createUrl, signerBody, _json, ct);
        signerResp.EnsureSuccessStatusCode();
        var signerJson = await signerResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var signerKey = signerJson.GetProperty("signer").GetProperty("key").GetString()!;

        // 2. Associar signatário ao documento
        var listBody = new
        {
            list = new
            {
                document_key = docKey,
                signer_key = signerKey,
                sign_as = "sign",
                refusable = false,
                message = "Por favor, assine o contrato.",
            }
        };

        var listUrl = $"/api/v1/lists?access_token={Uri.EscapeDataString(apiKey)}";
        var listResp = await client.PostAsJsonAsync(listUrl, listBody, _json, ct);
        listResp.EnsureSuccessStatusCode();
    }

    public async Task<string> ObterStatusAsync(
        string apiKey, bool sandbox, string docKey, CancellationToken ct)
    {
        var client = CreateClient(apiKey, sandbox);
        var url = $"/api/v1/documents/{docKey}?access_token={Uri.EscapeDataString(apiKey)}";
        var resp = await client.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var status = json.GetProperty("document").GetProperty("status").GetString();
        return status == "closed" ? "Assinado" : "Pendente";
    }
}

public record ClickSignDocResult(string DocKey, string ViewerUrl);
```

- [ ] **Step 2: Registrar `ClickSignService` em `Program.cs`**

```bash
grep -n "AddScoped\|AddHttpClient\|builder.Services" backend/src/GestorAI.API/Program.cs | head -20
```

Adicionar junto com os outros `AddScoped`:

```csharp
builder.Services.AddHttpClient();
builder.Services.AddScoped<ClickSignService>();
```

Se `AddHttpClient()` já estiver registrado, apenas adicionar o `AddScoped`.

- [ ] **Step 3: Build**

```bash
cd backend
dotnet build src/GestorAI.API --no-restore 2>&1 | tail -10
```

Esperado: sem erros.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Services/Contratos/ClickSignService.cs \
        backend/src/GestorAI.API/Program.cs
git commit -m "feat: add ClickSignService for digital signature integration"
```

---

### Task 3: Backend — endpoint `POST /api/contratos/{id}/enviar-assinatura`

**Files:**
- Modify: `backend/src/GestorAI.API/DTOs/Contratos/ContratoDto.cs`
- Modify: `backend/src/GestorAI.API/Services/Contratos/ContratoService.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs`

O fluxo:
1. Busca `ConfiguracaoEmpresa` para obter `ClickSignApiKey`
2. Gera o HTML do contrato (reutiliza `GetPdfHtmlAsync`)
3. Converte HTML para PDF usando biblioteca (usar `PuppeteerSharp` ou `wkhtmltopdf` via processo externo)
4. Chama `ClickSignService.CriarDocumentoAsync`
5. Chama `ClickSignService.AdicionarSignatarioAsync` com o email do cliente
6. Salva `ClickSignDocKey`, `ClickSignStatus = "Pendente"`, `ClickSignViewerUrl` no contrato

**Nota sobre PDF:** PuppeteerSharp é a opção mais simples para .NET. Adicionar ao projeto como NuGet: `PuppeteerSharp`.

- [ ] **Step 1: Adicionar dependência PuppeteerSharp**

```bash
cd backend
dotnet add src/GestorAI.API package PuppeteerSharp --version 20.*
```

- [ ] **Step 2: Adicionar DTO**

Em `ContratoDto.cs`, adicionar:

```csharp
public record EnviarAssinaturaRequest(string EmailSignatario);

public record EnviarAssinaturaResponse(
    string DocKey,
    string ViewerUrl,
    string Status);
```

Atualizar `ContratoResponse` adicionando os 3 campos de assinatura ao final:

```csharp
public record ContratoResponse(
    Guid Id,
    int Numero,
    string ClienteNome,
    string ClienteWhatsapp,
    string Titulo,
    string Objeto,
    string TipoCobranca,
    decimal Valor,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string Periodicidade,
    int DiaVencimento,
    string Status,
    string? Observacao,
    DateTime CriadoEm,
    List<ContratoItemResponse> Itens,
    decimal Total,
    string? ClickSignStatus,
    string? ClickSignViewerUrl);
```

- [ ] **Step 3: Atualizar `ToResponse` em `ContratoService.cs`**

Modificar o método `ToResponse` para incluir os novos campos:

```csharp
private static ContratoResponse ToResponse(Contrato c) => new(
    c.Id, c.Numero, c.Cliente?.Nome ?? "", c.Cliente?.Whatsapp ?? "",
    c.Titulo, c.Objeto, c.TipoCobranca.ToString(), c.Valor,
    c.DataInicio, c.DataFim, c.Periodicidade.ToString(),
    c.DiaVencimento, c.Status.ToString(), c.Observacao, c.CriadoEm,
    c.Itens.Select(i => new ContratoItemResponse(i.Id, i.Descricao, i.Quantidade, i.ValorUnitario)).ToList(),
    c.Itens.Sum(i => i.Quantidade * i.ValorUnitario),
    c.ClickSignStatus,
    c.ClickSignViewerUrl);
```

- [ ] **Step 4: Adicionar `EnviarAssinaturaAsync` em `ContratoService.cs`**

Atualizar o construtor para injetar `ClickSignService` e `AppDbContext`:

```csharp
public class ContratoService(AppDbContext db, TenantContext tenantContext, ClickSignService? clickSignService = null)
```

Adicionar o método:

```csharp
public async Task<EnviarAssinaturaResponse> EnviarAssinaturaAsync(
    Guid id, EnviarAssinaturaRequest req, CancellationToken ct)
{
    if (clickSignService is null)
        throw new AppException("Serviço ClickSign não configurado.", 500);

    var contrato = await FindAsync(id, ct);
    if (contrato.Status != ContratoStatus.Ativo)
        throw new AppException("Apenas contratos ativos podem ser enviados para assinatura.", 400);

    var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct)
        ?? throw new AppException("Configuração da empresa não encontrada.", 404);

    if (string.IsNullOrWhiteSpace(config.ClickSignApiKey))
        throw new AppException("API Key do ClickSign não configurada. Acesse Configurações → Integrações.", 400);

    var html = await GetPdfHtmlAsync(id, ct);
    var pdfBytes = await GerarPdfAsync(html, ct);

    var nomeArquivo = $"contrato-{contrato.Numero:D3}-{contrato.Titulo.Replace(" ", "_").ToLower()}.pdf";
    var docResult = await clickSignService.CriarDocumentoAsync(
        config.ClickSignApiKey, config.ClickSignSandbox, nomeArquivo, pdfBytes, ct);

    await clickSignService.AdicionarSignatarioAsync(
        config.ClickSignApiKey, config.ClickSignSandbox,
        docResult.DocKey, contrato.Cliente!.Nome, req.EmailSignatario, ct);

    contrato.ClickSignDocKey = docResult.DocKey;
    contrato.ClickSignStatus = "Pendente";
    contrato.ClickSignViewerUrl = docResult.ViewerUrl;
    await db.SaveChangesAsync(ct);

    return new EnviarAssinaturaResponse(docResult.DocKey, docResult.ViewerUrl, "Pendente");
}

private static async Task<byte[]> GerarPdfAsync(string html, CancellationToken ct)
{
    using var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new PuppeteerSharp.LaunchOptions
    {
        Headless = true,
        Args = ["--no-sandbox", "--disable-setuid-sandbox"],
    });
    using var page = await browser.NewPageAsync();
    await page.SetContentAsync(html);
    return await page.PdfDataAsync(new PuppeteerSharp.PdfOptions
    {
        Format = PuppeteerSharp.Media.PaperFormat.A4,
        PrintBackground = true,
    });
}
```

- [ ] **Step 5: Adicionar endpoint em `ContratosEndpoints.cs`**

```csharp
group.MapPost("/{id:guid}/enviar-assinatura", async (
    Guid id, EnviarAssinaturaRequest req, ContratoService svc, CancellationToken ct) =>
    Results.Ok(await svc.EnviarAssinaturaAsync(id, req, ct)));
```

- [ ] **Step 6: Build**

```bash
cd backend
dotnet build src/GestorAI.API --no-restore 2>&1 | tail -15
```

Se `PuppeteerSharp.LaunchAsync` reclamar de Chromium não encontrado em build time, apenas confirmar que o código compila (o download do Chromium é feito em runtime na primeira execução).

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Contratos/ContratoDto.cs \
        backend/src/GestorAI.API/Services/Contratos/ContratoService.cs \
        backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs
git commit -m "feat: add POST /api/contratos/{id}/enviar-assinatura endpoint"
```

---

### Task 4: Frontend — UI de assinatura em `DetalheContrato`

**Files:**
- Modify: `frontend/src/types/contrato.ts`
- Modify: `frontend/src/hooks/useContratos.ts`
- Modify: `frontend/src/pages/contratos/DetalheContrato.tsx`
- Modify: `frontend/src/pages/configuracoes/Integracoes.tsx`

- [ ] **Step 1: Atualizar `types/contrato.ts`**

Verificar o arquivo e adicionar os campos em `ContratoResponse`:

```bash
grep -n "interface ContratoResponse\|clickSign\|assinatura" frontend/src/types/contrato.ts
```

Adicionar ao `ContratoResponse`:

```typescript
clickSignStatus: string | null
clickSignViewerUrl: string | null
```

- [ ] **Step 2: Adicionar `enviarAssinatura` em `useContratos.ts`**

```typescript
const enviarAssinatura = useCallback(async (id: string, emailSignatario: string) => {
  return api.post<{ docKey: string; viewerUrl: string; status: string }>(
    `/api/contratos/${id}/enviar-assinatura`,
    { emailSignatario }
  )
}, [])
```

Adicionar ao `return`.

- [ ] **Step 3: Adicionar UI em `DetalheContrato.tsx`**

Atualizar desestruturação do hook:

```typescript
const { contrato, loading, error, get, ativar, encerrar, cancelar, gerarCobrancas, downloadPdf, renovar, enviarAssinatura } = useContratos()
```

Adicionar estado do modal de assinatura:

```typescript
const [modalAssinatura, setModalAssinatura] = useState(false)
const [emailAssinatura, setEmailAssinatura] = useState('')
const [enviandoAss, setEnviandoAss] = useState(false)
```

Adicionar função:

```typescript
async function handleEnviarAssinatura() {
  if (!id) return
  setEnviandoAss(true)
  setActionError('')
  try {
    await enviarAssinatura(c.id, emailAssinatura)
    setModalAssinatura(false)
    void get(id)
  } catch (e) {
    setActionError(e instanceof Error ? e.message : 'Erro ao enviar para assinatura')
  } finally {
    setEnviandoAss(false)
  }
}
```

Adicionar badge de status de assinatura próximo ao status do contrato (no header):

```tsx
{c.clickSignStatus && (
  <span className={cn(
    'px-2 py-0.5 rounded-full text-xs font-medium',
    c.clickSignStatus === 'Assinado'
      ? 'bg-green-100 text-green-700'
      : 'bg-amber-100 text-amber-700'
  )}>
    Assinatura: {c.clickSignStatus}
  </span>
)}
{c.clickSignViewerUrl && (
  <a href={c.clickSignViewerUrl} target="_blank" rel="noreferrer"
    className="text-xs text-primary underline">
    Ver documento
  </a>
)}
```

Adicionar botão "Enviar para Assinatura" nas ações do contrato `Ativo` (junto ao botão de Renovar):

```tsx
{c.status === 'Ativo' && !c.clickSignStatus && (
  <Button variant="outline" size="sm" onClick={() => setModalAssinatura(true)}>
    Enviar para Assinatura
  </Button>
)}
```

Adicionar modal:

```tsx
{modalAssinatura && (
  <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center">
    <div className="bg-background rounded-xl border p-6 w-full max-w-sm flex flex-col gap-4">
      <h2 className="font-bold">Enviar para Assinatura Digital</h2>
      <p className="text-sm text-muted-foreground">
        O contrato será enviado via ClickSign para o email informado.
      </p>
      <div>
        <label className="block text-sm mb-1">Email do signatário</label>
        <input
          type="email"
          value={emailAssinatura}
          onChange={e => setEmailAssinatura(e.target.value)}
          placeholder="cliente@email.com"
          className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm"
        />
      </div>
      <div className="flex gap-2">
        <Button onClick={handleEnviarAssinatura} disabled={enviandoAss || !emailAssinatura}>
          {enviandoAss ? 'Enviando...' : 'Enviar'}
        </Button>
        <Button variant="outline" onClick={() => setModalAssinatura(false)}>Cancelar</Button>
      </div>
    </div>
  </div>
)}
```

- [ ] **Step 4: Adicionar campos ClickSign em `Integracoes.tsx`**

Adicionar novo bloco após o bloco do Asaas:

```tsx
<div className="rounded-md border p-4 space-y-4">
  <h2 className="font-semibold">ClickSign — Assinatura Digital</h2>
  <p className="text-sm text-muted-foreground">
    Conecte sua conta ClickSign para enviar contratos para assinatura digital.
    Obtenha a API Key em app.clicksign.com → Configurações → API.
  </p>
  <div className="grid gap-2">
    <Label>API Key</Label>
    <Input
      type="password"
      value={config.clickSignApiKey}
      onChange={e => setConfig(c => ({ ...c, clickSignApiKey: e.target.value }))}
      placeholder="sua-api-key-clicksign"
    />
  </div>
  <div className="flex items-center gap-2">
    <input
      type="checkbox"
      id="clickSignSandbox"
      checked={config.clickSignSandbox}
      onChange={e => setConfig(c => ({ ...c, clickSignSandbox: e.target.checked }))}
    />
    <Label htmlFor="clickSignSandbox">Modo Sandbox (testes)</Label>
  </div>
</div>
```

Atualizar interface `ConfigIntegracoes` para incluir os novos campos:

```typescript
interface ConfigIntegracoes {
  asaasApiKey: string
  asaasSandbox: boolean
  clickSignApiKey: string
  clickSignSandbox: boolean
}
```

Atualizar o `setConfig` do `useEffect` inicial e o PUT de save para incluir `clickSignApiKey` e `clickSignSandbox`.

Verificar o endpoint `PUT /api/configuracao-empresa/integracoes` — pode ser necessário atualizar o backend para aceitar os novos campos.

- [ ] **Step 5: Verificar e atualizar endpoint de configuração**

```bash
grep -n "clickSign\|ClickSign\|integracoes" backend/src/GestorAI.API/Endpoints/*.cs
```

Se não houver suporte para `ClickSign` no endpoint de integrações, adicionar no handler correspondente.

- [ ] **Step 6: Build TypeScript**

```bash
cd frontend && npx tsc --noEmit 2>&1 | tail -20
```

- [ ] **Step 7: Commit**

```bash
git add frontend/src/types/contrato.ts \
        frontend/src/hooks/useContratos.ts \
        frontend/src/pages/contratos/DetalheContrato.tsx \
        frontend/src/pages/configuracoes/Integracoes.tsx
git commit -m "feat: add digital signature UI in DetalheContrato and ClickSign config"
```

---

## Checklist final

- [ ] Campos `ClickSignDocKey`, `ClickSignStatus`, `ClickSignViewerUrl` na entidade `Contrato`
- [ ] `ClickSignApiKey` e `ClickSignSandbox` em `ConfiguracaoEmpresa`
- [ ] Migration aplicada sem erros
- [ ] `ClickSignService` compilando
- [ ] `POST /api/contratos/{id}/enviar-assinatura` funcional
- [ ] Badge de status de assinatura visível em `DetalheContrato`
- [ ] Botão "Enviar para Assinatura" visível em contratos Ativos
- [ ] Campos ClickSign configuráveis em Integrações
- [ ] `npx tsc --noEmit` sem erros
