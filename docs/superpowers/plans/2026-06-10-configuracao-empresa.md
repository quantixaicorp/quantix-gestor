# Configuração da Empresa Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Centralizar todos os dados da empresa em `/configuracoes/empresa`, corrigir o bug onde AprovarAutomaticamente/ValorSinal/HorasLimiteCancelamento nunca eram persistidos, e aplicar identidade visual (logo, cor, nome, endereço) nos PDFs de orçamento e contrato.

**Architecture:** 4 tasks independentes e sequenciais: (1) backend entity + endpoints; (2) helper de PDF + atualizar GetPdfHtmlAsync dos dois serviços; (3) nova página frontend unificada; (4) limpeza das páginas que tinham campos duplicados. O `PUT /api/configuracao-empresa` já usa o padrão `if (req.X is not null) config.X = req.X`, então cada seção da nova página envia apenas seus próprios campos.

**Tech Stack:** .NET 10 Minimal APIs, EF Core 10, PostgreSQL, React + TypeScript + Tailwind CSS, xUnit, InMemoryDatabase para testes

---

## File Map

**Backend — modificar:**
- `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs` — adicionar `Telefone`, `Email`
- `backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs` — estender request/response, novo DTO agendamento
- `backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs` — `AtualizarAsync` + `ToResponse` + novo `SalvarAgendamentoAsync`
- `backend/src/GestorAI.API/Endpoints/FiscalEndpoints.cs` — novo endpoint `PUT /agendamento`
- `backend/src/GestorAI.API/Services/Orcamentos/OrcamentoService.cs` — `GetPdfHtmlAsync` com cabeçalho/rodapé
- `backend/src/GestorAI.API/Services/Contratos/ContratoService.cs` — `GetPdfHtmlAsync` com cabeçalho/rodapé
- `backend/src/GestorAI.API/Endpoints/OrcamentosEndpoints.cs` — pass `apiBase` para o método
- `backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs` — pass `apiBase` para o método
- `backend/src/GestorAI.API/appsettings.json` — adicionar `ApiBase`

**Backend — criar:**
- `backend/src/GestorAI.API/Services/Shared/HtmlDocumentoBase.cs`
- `backend/tests/GestorAI.Tests/Services/ConfiguracaoEmpresaServiceTests.cs`
- `backend/tests/GestorAI.Tests/Services/HtmlDocumentoBaseTests.cs`

**Frontend — criar:**
- `frontend/src/pages/configuracoes/ConfiguracaoEmpresa.tsx`

**Frontend — modificar:**
- `frontend/src/types/fiscal.ts` — adicionar novos campos ao response/request
- `frontend/src/router/index.tsx` — nova rota `/configuracoes/empresa`
- `frontend/src/components/layout/Sidebar.tsx` — link "Empresa"
- `frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx` — remover seções visuais
- `frontend/src/pages/fiscal/ConfiguracaoTab.tsx` — remover seções dados empresa e endereço

---

## Task 1: Backend — Entidade, Migration, Agendamento Endpoint, Response

**Files:**
- Modify: `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs`
- Modify: `backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs`
- Modify: `backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/FiscalEndpoints.cs`
- Create: `backend/tests/GestorAI.Tests/Services/ConfiguracaoEmpresaServiceTests.cs`
- New migration (gerada automaticamente)

- [ ] **Step 1: Adicionar Telefone e Email à entidade**

Em `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs`, adicionar após `public string? Cnpj { get; set; }`:

```csharp
    public string? Telefone { get; set; }
    public string? Email    { get; set; }
```

- [ ] **Step 2: Gerar migration**

```bash
cd backend && /usr/local/share/dotnet/dotnet ef migrations add AddTelefoneEmailEmpresa \
  --project src/GestorAI.API --startup-project src/GestorAI.API \
  --output-dir Infrastructure/Data/Migrations 2>&1
```
Esperado: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 3: Escrever testes falhando**

```csharp
// backend/tests/GestorAI.Tests/Services/ConfiguracaoEmpresaServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Fiscal;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Fiscal;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ConfiguracaoEmpresaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ConfiguracaoEmpresaService svc) Setup()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        return (db, new ConfiguracaoEmpresaService(db, tenant, null!));
    }

    [Fact]
    public async Task AtualizarAsync_PersisteTelefoneEmail()
    {
        var (_, svc) = Setup();
        var req = new AtualizarConfiguracaoEmpresaRequest(
            null, null, null, null, null, null, null, null, null, null, null, null, null,
            null, null, null, null, null, null, null,
            Telefone: "11999990000", Email: "contato@empresa.com");

        var result = await svc.AtualizarAsync(req, default);

        Assert.Equal("11999990000", result.Telefone);
        Assert.Equal("contato@empresa.com", result.Email);
    }

    [Fact]
    public async Task SalvarAgendamentoAsync_PersisteValores()
    {
        var (_, svc) = Setup();
        var req = new SalvarAgendamentoConfigRequest(false, 50.00m, 24);

        await svc.SalvarAgendamentoAsync(req, default);
        var result = await svc.ObterAsync(default);

        Assert.False(result.AprovarAutomaticamente);
        Assert.Equal(50.00m, result.ValorSinal);
        Assert.Equal(24, result.HorasLimiteCancelamento);
    }

    [Fact]
    public async Task ObterAsync_AprovarAutomaticamenteDefaultTrue()
    {
        var (_, svc) = Setup();

        var result = await svc.ObterAsync(default);

        Assert.True(result.AprovarAutomaticamente);
    }
}
```

- [ ] **Step 4: Rodar testes — verificar que falham**

```bash
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests \
  --filter "ConfiguracaoEmpresaServiceTests" --no-build 2>&1 | tail -5
```
Esperado: erro de compilação (tipos não existem ainda)

- [ ] **Step 5: Atualizar ConfiguracaoEmpresaDto.cs**

Substituir o arquivo `backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs` pelo conteúdo abaixo:

```csharp
namespace GestorAI.API.DTOs.Fiscal;

public record ConfiguracaoEmpresaResponse(
    Guid Id,
    string? RazaoSocial,
    string? NomeFantasia,
    string? Cnpj,
    string? InscricaoEstadual,
    string? InscricaoMunicipal,
    string? Telefone,
    string? Email,
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
    string? ClickSignApiKey,
    bool ClickSignSandbox,
    string? EvolutionApiUrl,
    bool TemEvolutionKey,
    string? EvolutionInstance,
    bool Lembrete3dAntes,
    bool Lembrete1dAntes,
    bool LembreteNoDia,
    bool Lembrete1dDepois,
    bool Lembrete3dDepois,
    bool Lembrete7dDepois,
    string? DominioCustomizado,
    bool AprovarAutomaticamente,
    decimal? ValorSinal,
    int? HorasLimiteCancelamento);

public record SalvarIntegracoesRequest(string? AsaasApiKey, bool AsaasSandbox, string? ClickSignApiKey, bool ClickSignSandbox);

public record SalvarWhiteLabelRequest(
    string? Slug,
    string? LogoUrl,
    string? CorPrimaria,
    string? DescricaoPublica,
    string? DominioCustomizado);

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

public record SalvarAgendamentoConfigRequest(
    bool AprovarAutomaticamente,
    decimal? ValorSinal,
    int? HorasLimiteCancelamento);

public record AtualizarConfiguracaoEmpresaRequest(
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
    string? CscId,
    string? CscToken,
    int? Ambiente,
    int? SerieNfe,
    int? SerieNfce,
    string? FocusNfeToken,
    string? Telefone = null,
    string? Email = null);
```

- [ ] **Step 6: Atualizar ConfiguracaoEmpresaService.cs**

Adicionar `SalvarAgendamentoAsync` ao serviço (após `SalvarBrandingAsync`):

```csharp
    public async Task SalvarAgendamentoAsync(SalvarAgendamentoConfigRequest req, CancellationToken ct)
    {
        var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct)
            ?? new ConfiguracaoEmpresa { Id = Guid.NewGuid(), EmpresaId = tenantContext.EmpresaId };
        var isNew = config.Id == Guid.Empty;
        config.AprovarAutomaticamente = req.AprovarAutomaticamente;
        config.ValorSinal = req.ValorSinal;
        config.HorasLimiteCancelamento = req.HorasLimiteCancelamento;
        if (isNew) db.ConfiguracoesEmpresa.Add(config);
        await db.SaveChangesAsync(ct);
    }
```

Atualizar `AtualizarAsync` — adicionar após a linha `if (req.FocusNfeToken is not null) config.FocusNfeToken = req.FocusNfeToken;`:

```csharp
        if (req.Telefone is not null) config.Telefone = req.Telefone;
        if (req.Email is not null) config.Email = req.Email;
```

Substituir o método `ToResponse` pelo seguinte (adiciona os 5 novos campos ao final do construtor):

```csharp
    private static ConfiguracaoEmpresaResponse ToResponse(ConfiguracaoEmpresa c) => new(
        c.Id,
        c.RazaoSocial,
        c.NomeFantasia,
        c.Cnpj,
        c.InscricaoEstadual,
        c.InscricaoMunicipal,
        c.Telefone,
        c.Email,
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
        c.ClickSignApiKey,
        c.ClickSignSandbox,
        c.EvolutionApiUrl,
        c.EvolutionApiKey is not null,
        c.EvolutionInstance,
        c.Lembrete3dAntes,
        c.Lembrete1dAntes,
        c.LembreteNoDia,
        c.Lembrete1dDepois,
        c.Lembrete3dDepois,
        c.Lembrete7dDepois,
        c.DominioCustomizado,
        c.AprovarAutomaticamente,
        c.ValorSinal,
        c.HorasLimiteCancelamento);
```

- [ ] **Step 7: Adicionar endpoint em FiscalEndpoints.cs**

Adicionar antes do `}` final de `MapFiscal`:

```csharp
        group.MapPut("/configuracao-empresa/agendamento", async (
            SalvarAgendamentoConfigRequest req, ConfiguracaoEmpresaService svc, CancellationToken ct) =>
        {
            await svc.SalvarAgendamentoAsync(req, ct);
            return Results.Ok();
        });
```

- [ ] **Step 8: Rodar testes — verificar que passam**

```bash
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests \
  --filter "ConfiguracaoEmpresaServiceTests" 2>&1 | tail -10
```
Esperado: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 9: Build completo**

```bash
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj --no-restore -v quiet 2>&1 | tail -5
```
Esperado: `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`

- [ ] **Step 10: Commit**

```bash
cd ..
git add backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs \
  backend/src/GestorAI.API/DTOs/Fiscal/ConfiguracaoEmpresaDto.cs \
  backend/src/GestorAI.API/Services/Fiscal/ConfiguracaoEmpresaService.cs \
  backend/src/GestorAI.API/Endpoints/FiscalEndpoints.cs \
  backend/src/GestorAI.API/Infrastructure/Data/Migrations/ \
  backend/tests/
git commit -m "feat: add Telefone/Email to ConfiguracaoEmpresa, fix agendamento policy never persisted"
```

---

## Task 2: Backend — HtmlDocumentoBase + PDFs com Identidade Visual

**Files:**
- Create: `backend/src/GestorAI.API/Services/Shared/HtmlDocumentoBase.cs`
- Create: `backend/tests/GestorAI.Tests/Services/HtmlDocumentoBaseTests.cs`
- Modify: `backend/src/GestorAI.API/Services/Orcamentos/OrcamentoService.cs`
- Modify: `backend/src/GestorAI.API/Services/Contratos/ContratoService.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/OrcamentosEndpoints.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs`
- Modify: `backend/src/GestorAI.API/appsettings.json`

- [ ] **Step 1: Escrever testes falhando para HtmlDocumentoBase**

```csharp
// backend/tests/GestorAI.Tests/Services/HtmlDocumentoBaseTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Services.Shared;

namespace GestorAI.Tests.Services;

public class HtmlDocumentoBaseTests
{
    [Fact]
    public void WrapDocument_ComCorPrimaria_ContemCorNoCabecalho()
    {
        var cfg = new ConfiguracaoEmpresa
        {
            NomeFantasia = "Barbearia do João",
            CorPrimaria = "#ff5500",
        };

        var html = HtmlDocumentoBase.WrapDocument("Titulo", "<p>corpo</p>", cfg, "");

        Assert.Contains("#ff5500", html);
    }

    [Fact]
    public void WrapDocument_SemCfg_UsaCorFallback()
    {
        var html = HtmlDocumentoBase.WrapDocument("Titulo", "<p>corpo</p>", null, "");

        Assert.Contains("#2563eb", html);
        Assert.DoesNotContain("NullReference", html);
    }

    [Fact]
    public void WrapDocument_ComEndereco_ContemRodape()
    {
        var cfg = new ConfiguracaoEmpresa
        {
            Logradouro = "Rua das Flores", Numero = "10",
            Municipio = "São Paulo", Uf = "SP", Cep = "01310-100"
        };

        var html = HtmlDocumentoBase.WrapDocument("T", "<p>corpo</p>", cfg, "");

        Assert.Contains("Rua das Flores", html);
        Assert.Contains("São Paulo/SP", html);
    }

    [Fact]
    public void WrapDocument_SemEndereco_SemRodape()
    {
        var cfg = new ConfiguracaoEmpresa { NomeFantasia = "Empresa" };

        var html = HtmlDocumentoBase.WrapDocument("T", "<p>corpo</p>", cfg, "");

        Assert.DoesNotContain("footer", html);
    }

    [Fact]
    public void WrapDocument_ComLogo_ContemImgTag()
    {
        var cfg = new ConfiguracaoEmpresa
        {
            LogoUrl = "/uploads/logos/logo.png",
            NomeFantasia = "Empresa X",
        };

        var html = HtmlDocumentoBase.WrapDocument("T", "<p>corpo</p>", cfg, "https://api.gestorai.com.br");

        Assert.Contains("https://api.gestorai.com.br/uploads/logos/logo.png", html);
    }
}
```

- [ ] **Step 2: Rodar testes — verificar que falham**

```bash
cd backend && /usr/local/share/dotnet/dotnet test tests/GestorAI.Tests \
  --filter "HtmlDocumentoBaseTests" --no-build 2>&1 | tail -5
```
Esperado: erro de compilação

- [ ] **Step 3: Criar HtmlDocumentoBase.cs**

```csharp
// backend/src/GestorAI.API/Services/Shared/HtmlDocumentoBase.cs
using GestorAI.API.Domain.Entities;

namespace GestorAI.API.Services.Shared;

public static class HtmlDocumentoBase
{
    public static string WrapDocument(
        string titulo, string corpo, ConfiguracaoEmpresa? cfg, string apiBase)
    {
        var cor = cfg?.CorPrimaria ?? "#2563eb";
        var nome = cfg?.NomeFantasia ?? cfg?.RazaoSocial ?? "Empresa";
        var cabecalho = BuildCabecalho(cfg, cor, nome, apiBase);
        var rodape = BuildRodape(cfg);

        return $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8">
            <style>
              * { box-sizing: border-box; margin: 0; padding: 0; }
              body { font-family: sans-serif; color: #111; }
              .content { padding: 32px; }
              h1 { font-size: 20px; margin-bottom: 4px; }
              .meta { color: #555; font-size: 13px; margin-bottom: 24px; margin-top: 8px; }
              table { width: 100%; border-collapse: collapse; margin-bottom: 16px; }
              th, td { border: 1px solid #ddd; padding: 8px; text-align: left; font-size: 13px; }
              th { background: #f5f5f5; }
              .total { text-align: right; font-weight: bold; font-size: 15px; margin-top: 8px; }
              .objeto { background: #f9f9f9; border: 1px solid #ddd; padding: 16px; margin: 16px 0; font-size: 13px; white-space: pre-wrap; }
              .assinatura { margin-top: 60px; display: flex; gap: 60px; }
              .assinatura div { border-top: 1px solid #333; padding-top: 8px; font-size: 12px; min-width: 200px; }
              .obs { margin-top: 16px; font-size: 13px; color: #555; }
            </style>
            </head>
            <body>
              {{cabecalho}}
              <div class="content">
                {{corpo}}
              </div>
              {{rodape}}
            </body>
            </html>
            """;
    }

    private static string BuildCabecalho(
        ConfiguracaoEmpresa? cfg, string cor, string nome, string apiBase)
    {
        var logoHtml = "";
        if (!string.IsNullOrWhiteSpace(cfg?.LogoUrl))
        {
            var src = cfg.LogoUrl.StartsWith("http")
                ? cfg.LogoUrl
                : $"{apiBase}{cfg.LogoUrl}";
            logoHtml = $"""<img src="{src}" style="height:48px;object-fit:contain;margin-right:12px;" />""";
        }

        var linhasDireita = new List<string>();
        if (!string.IsNullOrWhiteSpace(cfg?.Cnpj))
            linhasDireita.Add($"CNPJ: {cfg.Cnpj}");
        var contato = string.Join(" | ", new[] { cfg?.Telefone, cfg?.Email }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!));
        if (!string.IsNullOrWhiteSpace(contato))
            linhasDireita.Add(contato);

        var direita = linhasDireita.Count > 0
            ? $"""
              <div style="color:white;text-align:right;font-size:12px;line-height:1.7;">
                <div style="font-weight:bold;font-size:14px;">{nome}</div>
                {string.Join("", linhasDireita.Select(l => $"<div>{l}</div>"))}
              </div>
              """
            : $"""<div style="color:white;font-weight:bold;font-size:14px;">{nome}</div>""";

        return $"""
            <div style="background:{cor};padding:16px 32px;display:flex;align-items:center;justify-content:space-between;">
              <div style="display:flex;align-items:center;">
                {logoHtml}
                {(linhasDireita.Count > 0 ? "" : $"""<span style="color:white;font-size:20px;font-weight:bold;">{nome}</span>""")}
              </div>
              {(linhasDireita.Count > 0 ? direita : "")}
            </div>
            """;
    }

    private static string BuildRodape(ConfiguracaoEmpresa? cfg)
    {
        if (cfg is null) return "";

        var partes = new List<string>();
        var end = string.Join(", ", new[]
        {
            cfg.Logradouro,
            string.IsNullOrWhiteSpace(cfg.Numero) ? null : $"nº {cfg.Numero}",
            cfg.Complemento,
            cfg.Bairro
        }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!));

        if (!string.IsNullOrWhiteSpace(end)) partes.Add(end);

        var cidadeUf = string.Join("/", new[] { cfg.Municipio, cfg.Uf }
            .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!));
        if (!string.IsNullOrWhiteSpace(cidadeUf)) partes.Add(cidadeUf);
        if (!string.IsNullOrWhiteSpace(cfg.Cep)) partes.Add($"CEP {cfg.Cep}");

        if (partes.Count == 0) return "";

        return $"""
            <div class="footer" style="margin-top:40px;border-top:1px solid #ddd;padding-top:8px;text-align:center;font-size:11px;color:#888;">
              {string.Join(" — ", partes)}
            </div>
            """;
    }
}
```

- [ ] **Step 4: Rodar testes — verificar que passam**

```bash
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests \
  --filter "HtmlDocumentoBaseTests" 2>&1 | tail -10
```
Esperado: `Passed! - Failed: 0, Passed: 5`

- [ ] **Step 5: Atualizar OrcamentoService.GetPdfHtmlAsync**

Substituir o método `GetPdfHtmlAsync` em `backend/src/GestorAI.API/Services/Orcamentos/OrcamentoService.cs`:

Adicionar using no topo do arquivo:
```csharp
using GestorAI.API.Services.Shared;
```

Substituir o método completo:

```csharp
    public async Task<string> GetPdfHtmlAsync(Guid id, string apiBase, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        await ExpireIfNeededAsync([o], ct);

        var cfg = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.EmpresaId == tenantContext.EmpresaId, ct);

        var total = o.Itens.Sum(i => i.Quantidade * i.ValorUnitario);
        var linhas = string.Join("", o.Itens.Select(i =>
            $"<tr><td>{i.Descricao}</td><td>{i.Quantidade:N2}</td>" +
            $"<td>R$ {i.ValorUnitario:N2}</td><td>R$ {i.Quantidade * i.ValorUnitario:N2}</td></tr>"));
        var clienteHtml = o.Cliente != null ? $"Cliente: {o.Cliente.Nome}<br>" : "";
        var obsHtml = o.Observacao != null ? $"<div class='obs'>Obs: {o.Observacao}</div>" : "";

        var corpo = $$"""
            <h1>ORC-{{o.Numero:D3}} — {{o.Titulo}}</h1>
            <div class="meta">
              {{clienteHtml}}
              Válido até: {{o.DataValidade:dd/MM/yyyy}} | Status: {{o.Status}}
            </div>
            <table>
              <thead><tr><th>Descrição</th><th>Qtd</th><th>Unit.</th><th>Total</th></tr></thead>
              <tbody>{{linhas}}</tbody>
            </table>
            <div class="total">Total: R$ {{total:N2}}</div>
            {{obsHtml}}
            """;

        return HtmlDocumentoBase.WrapDocument($"ORC-{o.Numero:D3}", corpo, cfg, apiBase);
    }
```

- [ ] **Step 6: Atualizar ContratoService.GetPdfHtmlAsync**

Adicionar using no topo do arquivo `backend/src/GestorAI.API/Services/Contratos/ContratoService.cs`:
```csharp
using GestorAI.API.Services.Shared;
```

Substituir o método `GetPdfHtmlAsync`:

```csharp
    public async Task<string> GetPdfHtmlAsync(Guid id, string apiBase, CancellationToken ct)
    {
        var c = await db.Contratos
            .Include(c => c.Cliente)
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contrato não encontrado.", 404);

        var cfg = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.EmpresaId == tenantContext.EmpresaId, ct);

        var total = c.Itens.Sum(i => i.Quantidade * i.ValorUnitario);
        var linhas = string.Join("", c.Itens.Select(i =>
            $"<tr><td>{i.Descricao}</td><td>{i.Quantidade:N2}</td>" +
            $"<td>R$ {i.ValorUnitario:N2}</td><td>R$ {i.Quantidade * i.ValorUnitario:N2}</td></tr>"));

        var corpo = $$"""
            <h1>CONTRATO {{c.Numero:D3}} — {{c.Titulo}}</h1>
            <div class="meta">
              Cliente: {{c.Cliente?.Nome}}<br>
              Início: {{c.DataInicio:dd/MM/yyyy}}{{(c.DataFim.HasValue ? $" | Término: {c.DataFim:dd/MM/yyyy}" : "")}}<br>
              Tipo: {{c.TipoCobranca}} | Periodicidade: {{c.Periodicidade}} | Valor: R$ {{c.Valor:N2}}
            </div>
            <h2 style="font-size:14px">Objeto</h2>
            <div class="objeto">{{c.Objeto}}</div>
            <h2 style="font-size:14px">Itens</h2>
            <table>
              <thead><tr><th>Descrição</th><th>Qtd</th><th>Unit.</th><th>Total</th></tr></thead>
              <tbody>{{linhas}}</tbody>
            </table>
            <div class="total">Total: R$ {{total:N2}}</div>
            <div class="assinatura">
              <div>Contratante<br>{{c.Cliente?.Nome}}</div>
              <div>Contratada<br>{{cfg?.NomeFantasia ?? cfg?.RazaoSocial ?? ""}}</div>
            </div>
            """;

        return HtmlDocumentoBase.WrapDocument($"CONTRATO {c.Numero:D3}", corpo, cfg, apiBase);
    }
```

- [ ] **Step 7: Atualizar endpoints para passar apiBase**

Em `backend/src/GestorAI.API/Endpoints/OrcamentosEndpoints.cs`, substituir o endpoint de PDF:

```csharp
        group.MapGet("/{id:guid}/pdf", async (
            Guid id, OrcamentoService svc, IConfiguration config, CancellationToken ct) =>
        {
            var html = await svc.GetPdfHtmlAsync(id, config["ApiBase"] ?? "", ct);
            return Results.Content(html, "text/html");
        });
```

Em `backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs`, substituir o endpoint de PDF:

```csharp
        group.MapGet("/{id:guid}/pdf", async (
            Guid id, ContratoService svc, IConfiguration config, CancellationToken ct) =>
        {
            var html = await svc.GetPdfHtmlAsync(id, config["ApiBase"] ?? "", ct);
            return Results.Content(html, "text/html");
        });
```

- [ ] **Step 8: Verificar se GetPdfHtmlAsync é chamado em algum outro lugar (ClickSign)**

```bash
grep -rn "GetPdfHtmlAsync" /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend/src/ 2>/dev/null
```

Se aparecer alguma chamada além dos endpoints (ex: em ClickSign), atualizar para passar `""` como `apiBase` (o logo já estaria absoluto ou não será usado em contexto de ClickSign).

- [ ] **Step 9: Adicionar ApiBase ao appsettings.json**

Em `backend/src/GestorAI.API/appsettings.json`, adicionar após `"AdminFixKey"`:

```json
  "ApiBase": "http://localhost:5002",
```

- [ ] **Step 10: Build completo e todos os testes**

```bash
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj --no-restore -v quiet 2>&1 | tail -5
```
Esperado: `Compilação com êxito. 0 Aviso(s) 0 Erro(s)`

```bash
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests 2>&1 | tail -5
```
Esperado: `Passed! - Failed: 0`

- [ ] **Step 11: Commit**

```bash
cd ..
git add backend/src/GestorAI.API/Services/Shared/ \
  backend/src/GestorAI.API/Services/Orcamentos/OrcamentoService.cs \
  backend/src/GestorAI.API/Services/Contratos/ContratoService.cs \
  backend/src/GestorAI.API/Endpoints/OrcamentosEndpoints.cs \
  backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs \
  backend/src/GestorAI.API/appsettings.json \
  backend/tests/
git commit -m "feat: branded PDF documents with company logo, color, and footer"
```

---

## Task 3: Frontend — Página ConfiguracaoEmpresa.tsx

**Files:**
- Modify: `frontend/src/types/fiscal.ts`
- Create: `frontend/src/pages/configuracoes/ConfiguracaoEmpresa.tsx`
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Atualizar types/fiscal.ts**

Em `frontend/src/types/fiscal.ts`, substituir a interface `ConfiguracaoEmpresaResponse` e `AtualizarConfiguracaoEmpresaRequest`:

```typescript
export interface ConfiguracaoEmpresaResponse {
  id: string
  razaoSocial: string | null
  nomeFantasia: string | null
  cnpj: string | null
  inscricaoEstadual: string | null
  inscricaoMunicipal: string | null
  telefone: string | null
  email: string | null
  logradouro: string | null
  numero: string | null
  complemento: string | null
  bairro: string | null
  codigoMunicipio: string | null
  municipio: string | null
  uf: string | null
  cep: string | null
  regimeTributario: number | null
  ambiente: number | null
  serieNfe: number | null
  serieNfce: number | null
  temToken: boolean
  slug: string | null
  logoUrl: string | null
  corPrimaria: string | null
  descricaoPublica: string | null
  asaasApiKey: string | null
  asaasSandbox: boolean
  clickSignApiKey: string | null
  clickSignSandbox: boolean
  evolutionApiUrl: string | null
  temEvolutionKey: boolean
  evolutionInstance: string | null
  lembrete3dAntes: boolean
  lembrete1dAntes: boolean
  lembreteNoDia: boolean
  lembrete1dDepois: boolean
  lembrete3dDepois: boolean
  lembrete7dDepois: boolean
  dominioCustomizado: string | null
  aprovarAutomaticamente: boolean
  valorSinal: number | null
  horasLimiteCancelamento: number | null
}

export interface AtualizarConfiguracaoEmpresaRequest {
  razaoSocial?: string
  nomeFantasia?: string
  cnpj?: string
  inscricaoEstadual?: string
  inscricaoMunicipal?: string
  telefone?: string
  email?: string
  logradouro?: string
  numero?: string
  complemento?: string
  bairro?: string
  codigoMunicipio?: string
  municipio?: string
  uf?: string
  cep?: string
  regimeTributario?: number
  cscId?: string
  cscToken?: string
  ambiente?: number
  serieNfe?: number
  serieNfce?: number
  focusNfeToken?: string
}
```

- [ ] **Step 2: Criar ConfiguracaoEmpresa.tsx**

```tsx
// frontend/src/pages/configuracoes/ConfiguracaoEmpresa.tsx
import { useEffect, useState } from 'react'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { ConfiguracaoEmpresaResponse } from '@/types/fiscal'

const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'

function Section({ title, children, onSave, saving }: {
  title: string
  children: React.ReactNode
  onSave: () => void
  saving: boolean
}) {
  return (
    <div className="rounded-xl border bg-card p-5 space-y-4">
      <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">{title}</p>
      {children}
      <Button onClick={onSave} disabled={saving} size="sm">
        {saving ? 'Salvando...' : 'Salvar'}
      </Button>
    </div>
  )
}

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <Label className="text-xs text-muted-foreground">{label}</Label>
      {children}
    </div>
  )
}

const REGIMES = [
  { value: 1, label: '1 — Simples Nacional' },
  { value: 2, label: '2 — Simples Nacional — Excesso' },
  { value: 3, label: '3 — Regime Normal' },
]
const AMBIENTES = [
  { value: 1, label: '1 — Produção' },
  { value: 2, label: '2 — Homologação (testes)' },
]

export default function ConfiguracaoEmpresa() {
  const [loading, setLoading] = useState(true)
  const [temToken, setTemToken] = useState(false)

  // Identificação
  const [ident, setIdent] = useState({ razaoSocial: '', nomeFantasia: '', cnpj: '', inscricaoEstadual: '', inscricaoMunicipal: '', telefone: '', email: '' })
  const [savingIdent, setSavingIdent] = useState(false)

  // Endereço
  const [end, setEnd] = useState({ logradouro: '', numero: '', complemento: '', bairro: '', codigoMunicipio: '', municipio: '', uf: '', cep: '' })
  const [savingEnd, setSavingEnd] = useState(false)

  // Identidade Visual
  const [visual, setVisual] = useState({ slug: '', nomeExibicao: '', corPrimaria: '#2563eb', descricaoPublica: '', logoUrl: '' })
  const [savingVisual, setSavingVisual] = useState(false)
  const [uploading, setUploading] = useState(false)

  // NF-e
  const [nfe, setNfe] = useState({ regimeTributario: 1, ambiente: 2, serieNfe: 1, serieNfce: 1 })
  const [focusNfeToken, setFocusNfeToken] = useState('')
  const [savingNfe, setSavingNfe] = useState(false)

  // Agendamento
  const [agend, setAgend] = useState({ aprovarAutomaticamente: true, valorSinal: '', horasLimiteCancelamento: '' })
  const [savingAgend, setSavingAgend] = useState(false)

  useEffect(() => {
    api.get<ConfiguracaoEmpresaResponse>('/api/configuracao-empresa')
      .then(c => {
        setIdent({ razaoSocial: c.razaoSocial ?? '', nomeFantasia: c.nomeFantasia ?? '', cnpj: c.cnpj ?? '', inscricaoEstadual: c.inscricaoEstadual ?? '', inscricaoMunicipal: c.inscricaoMunicipal ?? '', telefone: c.telefone ?? '', email: c.email ?? '' })
        setEnd({ logradouro: c.logradouro ?? '', numero: c.numero ?? '', complemento: c.complemento ?? '', bairro: c.bairro ?? '', codigoMunicipio: c.codigoMunicipio ?? '', municipio: c.municipio ?? '', uf: c.uf ?? '', cep: c.cep ?? '' })
        setVisual({ slug: c.slug ?? '', nomeExibicao: c.nomeFantasia ?? '', corPrimaria: c.corPrimaria ?? '#2563eb', descricaoPublica: c.descricaoPublica ?? '', logoUrl: c.logoUrl ?? '' })
        setNfe({ regimeTributario: c.regimeTributario ?? 1, ambiente: c.ambiente ?? 2, serieNfe: c.serieNfe ?? 1, serieNfce: c.serieNfce ?? 1 })
        setAgend({ aprovarAutomaticamente: c.aprovarAutomaticamente, valorSinal: c.valorSinal != null ? String(c.valorSinal) : '', horasLimiteCancelamento: c.horasLimiteCancelamento != null ? String(c.horasLimiteCancelamento) : '' })
        setTemToken(c.temToken)
      })
      .catch(() => toast.error('Erro ao carregar configurações'))
      .finally(() => setLoading(false))
  }, [])

  async function saveIdent() {
    setSavingIdent(true)
    try {
      await api.put('/api/configuracao-empresa', { ...ident })
      toast.success('Identificação salva!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingIdent(false) }
  }

  async function saveEnd() {
    setSavingEnd(true)
    try {
      await api.put('/api/configuracao-empresa', { ...end })
      toast.success('Endereço salvo!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingEnd(false) }
  }

  async function saveVisual() {
    setSavingVisual(true)
    try {
      await api.put('/api/configuracao-empresa/branding', { slug: visual.slug, nomeExibicao: visual.nomeExibicao || null, corPrimaria: visual.corPrimaria, descricaoPublica: visual.descricaoPublica || null })
      toast.success('Identidade visual salva!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingVisual(false) }
  }

  async function saveNfe() {
    setSavingNfe(true)
    try {
      const req: Record<string, unknown> = { ...nfe }
      if (focusNfeToken.trim()) req.focusNfeToken = focusNfeToken.trim()
      await api.put('/api/configuracao-empresa', req)
      setFocusNfeToken('')
      toast.success('Configuração NF-e salva!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingNfe(false) }
  }

  async function saveAgend() {
    setSavingAgend(true)
    try {
      await api.put('/api/configuracao-empresa/agendamento', {
        aprovarAutomaticamente: agend.aprovarAutomaticamente,
        valorSinal: agend.valorSinal ? parseFloat(agend.valorSinal) : null,
        horasLimiteCancelamento: agend.horasLimiteCancelamento ? parseInt(agend.horasLimiteCancelamento) : null,
      })
      toast.success('Configuração de agendamento salva!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro') }
    finally { setSavingAgend(false) }
  }

  async function handleLogoUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setUploading(true)
    try {
      const formData = new FormData()
      formData.append('file', file)
      const token = localStorage.getItem('ga_token')
      const res = await fetch(`${API_BASE}/api/configuracao-empresa/logo`, {
        method: 'POST',
        headers: token ? { Authorization: `Bearer ${token}` } : {},
        body: formData,
      })
      if (!res.ok) throw new Error('Erro ao enviar logo')
      const data = await res.json() as { logoUrl: string }
      setVisual(v => ({ ...v, logoUrl: data.logoUrl }))
      toast.success('Logo atualizada!')
    } catch (e) { toast.error(e instanceof Error ? e.message : 'Erro ao enviar logo') }
    finally { setUploading(false) }
  }

  if (loading) return <div className="flex h-48 items-center justify-center text-muted-foreground">Carregando...</div>

  return (
    <div className="space-y-6 max-w-2xl">
      <h1 className="text-2xl font-bold">Configuração da Empresa</h1>

      {/* Identificação */}
      <Section title="Identificação" onSave={() => void saveIdent()} saving={savingIdent}>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Razão Social"><Input value={ident.razaoSocial} onChange={e => setIdent(v => ({ ...v, razaoSocial: e.target.value }))} /></Field>
          <Field label="Nome Fantasia"><Input value={ident.nomeFantasia} onChange={e => setIdent(v => ({ ...v, nomeFantasia: e.target.value }))} /></Field>
          <Field label="CNPJ"><Input value={ident.cnpj} placeholder="00.000.000/0000-00" onChange={e => setIdent(v => ({ ...v, cnpj: e.target.value }))} /></Field>
          <Field label="Inscrição Estadual"><Input value={ident.inscricaoEstadual} onChange={e => setIdent(v => ({ ...v, inscricaoEstadual: e.target.value }))} /></Field>
          <Field label="Inscrição Municipal"><Input value={ident.inscricaoMunicipal} onChange={e => setIdent(v => ({ ...v, inscricaoMunicipal: e.target.value }))} /></Field>
          <Field label="Telefone"><Input value={ident.telefone} placeholder="(11) 99999-0000" onChange={e => setIdent(v => ({ ...v, telefone: e.target.value }))} /></Field>
          <Field label="E-mail"><Input type="email" value={ident.email} placeholder="contato@empresa.com" onChange={e => setIdent(v => ({ ...v, email: e.target.value }))} /></Field>
        </div>
      </Section>

      {/* Endereço */}
      <Section title="Endereço" onSave={() => void saveEnd()} saving={savingEnd}>
        <div className="grid grid-cols-2 gap-4">
          <div className="col-span-2"><Field label="Logradouro"><Input value={end.logradouro} onChange={e => setEnd(v => ({ ...v, logradouro: e.target.value }))} /></Field></div>
          <Field label="Número"><Input value={end.numero} onChange={e => setEnd(v => ({ ...v, numero: e.target.value }))} /></Field>
          <Field label="Complemento"><Input value={end.complemento} onChange={e => setEnd(v => ({ ...v, complemento: e.target.value }))} /></Field>
          <Field label="Bairro"><Input value={end.bairro} onChange={e => setEnd(v => ({ ...v, bairro: e.target.value }))} /></Field>
          <Field label="Código Município (IBGE)"><Input value={end.codigoMunicipio} onChange={e => setEnd(v => ({ ...v, codigoMunicipio: e.target.value }))} /></Field>
          <Field label="Município"><Input value={end.municipio} onChange={e => setEnd(v => ({ ...v, municipio: e.target.value }))} /></Field>
          <Field label="UF"><Input value={end.uf} maxLength={2} className="uppercase" onChange={e => setEnd(v => ({ ...v, uf: e.target.value.toUpperCase() }))} /></Field>
          <Field label="CEP"><Input value={end.cep} placeholder="00000-000" onChange={e => setEnd(v => ({ ...v, cep: e.target.value }))} /></Field>
        </div>
      </Section>

      {/* Identidade Visual */}
      <Section title="Identidade Visual" onSave={() => void saveVisual()} saving={savingVisual}>
        <Field label="Logo">
          <div className="flex items-center gap-4">
            {visual.logoUrl && (
              <img src={visual.logoUrl.startsWith('http') ? visual.logoUrl : `${API_BASE}${visual.logoUrl}`}
                alt="Logo" className="h-16 w-16 rounded-full object-cover border" />
            )}
            <label className="cursor-pointer">
              <span className="inline-flex items-center px-3 py-1.5 rounded-md border text-sm hover:bg-muted transition-colors">
                {uploading ? 'Enviando...' : 'Escolher arquivo'}
              </span>
              <input type="file" accept=".jpg,.jpeg,.png,.webp" className="hidden"
                onChange={e => void handleLogoUpload(e)} disabled={uploading} />
            </label>
            <p className="text-xs text-muted-foreground">jpg, png ou webp • máx 2MB</p>
          </div>
        </Field>
        <Field label="Nome de exibição público">
          <Input value={visual.nomeExibicao} onChange={e => setVisual(v => ({ ...v, nomeExibicao: e.target.value }))} placeholder="Ex: Barbearia do João" />
        </Field>
        <Field label="Cor primária">
          <div className="flex items-center gap-3">
            <input type="color" value={visual.corPrimaria}
              onChange={e => setVisual(v => ({ ...v, corPrimaria: e.target.value }))}
              className="h-9 w-16 rounded border cursor-pointer" />
            <Input value={visual.corPrimaria} placeholder="#2563eb" className="max-w-28"
              onChange={e => setVisual(v => ({ ...v, corPrimaria: e.target.value }))} />
          </div>
        </Field>
        <Field label="Slug (URL pública)">
          <div className="flex items-center gap-2">
            <span className="text-sm text-muted-foreground whitespace-nowrap">/agendar/</span>
            <Input value={visual.slug} placeholder="minha-empresa"
              onChange={e => setVisual(v => ({ ...v, slug: e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '') }))} />
          </div>
        </Field>
        <Field label="Descrição pública">
          <Input value={visual.descricaoPublica} placeholder="Ex: Barbearia especializada em cortes modernos"
            onChange={e => setVisual(v => ({ ...v, descricaoPublica: e.target.value }))} />
        </Field>
        {/* Preview */}
        <div>
          <p className="text-xs text-muted-foreground mb-2">Pré-visualização</p>
          <div className="rounded-lg overflow-hidden" style={{ backgroundColor: visual.corPrimaria }}>
            <div className="px-4 py-4 flex items-center gap-3">
              {visual.logoUrl
                ? <img src={visual.logoUrl.startsWith('http') ? visual.logoUrl : `${API_BASE}${visual.logoUrl}`}
                    alt="Logo" className="h-10 w-10 rounded-full object-cover border-2 border-white/30" />
                : <div className="h-10 w-10 rounded-full bg-white/20 flex items-center justify-center text-white text-lg font-bold">
                    {(visual.nomeExibicao?.[0] ?? 'E').toUpperCase()}
                  </div>
              }
              <div>
                <p className="text-white font-bold text-base leading-tight">{visual.nomeExibicao || 'Nome da empresa'}</p>
                {visual.descricaoPublica && <p className="text-white/80 text-xs">{visual.descricaoPublica}</p>}
              </div>
            </div>
          </div>
        </div>
      </Section>

      {/* NF-e */}
      <Section title="Emissão de Notas Fiscais" onSave={() => void saveNfe()} saving={savingNfe}>
        <div className="grid grid-cols-2 gap-4">
          <Field label="Regime Tributário">
            <select value={nfe.regimeTributario}
              onChange={e => setNfe(v => ({ ...v, regimeTributario: Number(e.target.value) }))}
              className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
              {REGIMES.map(r => <option key={r.value} value={r.value}>{r.label}</option>)}
            </select>
          </Field>
          <Field label="Ambiente">
            <select value={nfe.ambiente}
              onChange={e => setNfe(v => ({ ...v, ambiente: Number(e.target.value) }))}
              className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
              {AMBIENTES.map(a => <option key={a.value} value={a.value}>{a.label}</option>)}
            </select>
          </Field>
          <Field label="Série NF-e">
            <Input type="number" min="1" value={nfe.serieNfe}
              onChange={e => setNfe(v => ({ ...v, serieNfe: Number(e.target.value) }))} />
          </Field>
          <Field label="Série NFC-e">
            <Input type="number" min="1" value={nfe.serieNfce}
              onChange={e => setNfe(v => ({ ...v, serieNfce: Number(e.target.value) }))} />
          </Field>
        </div>
        <Field label={`Token Focus NF-e${temToken ? ' (configurado)' : ''}`}>
          <Input type="password" value={focusNfeToken} onChange={e => setFocusNfeToken(e.target.value)}
            placeholder={temToken ? 'Deixe em branco para manter o atual' : 'Cole o token da API Focus NFe'} />
        </Field>
      </Section>

      {/* Agendamento Online */}
      <Section title="Agendamento Online" onSave={() => void saveAgend()} saving={savingAgend}>
        <div className="space-y-4">
          <div className="flex items-center gap-2">
            <input type="checkbox" id="aprovarAuto" checked={agend.aprovarAutomaticamente}
              onChange={e => setAgend(v => ({ ...v, aprovarAutomaticamente: e.target.checked }))}
              className="h-4 w-4" />
            <Label htmlFor="aprovarAuto">Confirmar agendamentos automaticamente</Label>
          </div>
          <p className="text-xs text-muted-foreground -mt-2">
            Quando desmarcado, novos agendamentos ficam aguardando confirmação manual.
          </p>
          <Field label="Valor do sinal de reserva (R$)">
            <Input type="number" min="0" step="0.01" value={agend.valorSinal}
              onChange={e => setAgend(v => ({ ...v, valorSinal: e.target.value }))}
              placeholder="0,00 (sem sinal)" />
          </Field>
          <Field label="Horas mínimas para cancelamento">
            <Input type="number" min="0" value={agend.horasLimiteCancelamento}
              onChange={e => setAgend(v => ({ ...v, horasLimiteCancelamento: e.target.value }))}
              placeholder="Ex: 24 (sem restrição se vazio)" />
          </Field>
        </div>
      </Section>
    </div>
  )
}
```

- [ ] **Step 3: Adicionar rota em router/index.tsx**

Adicionar import:
```tsx
import ConfiguracaoEmpresa from '@/pages/configuracoes/ConfiguracaoEmpresa'
```

Adicionar rota no bloco de rotas autenticadas (dentro de `children` do AppLayout):
```tsx
      { path: '/configuracoes/empresa', element: <ConfiguracaoEmpresa /> },
```

- [ ] **Step 4: Adicionar link no Sidebar.tsx**

Adicionar `Building2` aos imports do lucide-react:
```tsx
  Building2,
```

Adicionar como primeiro item do grupo `Configurações`:
```tsx
      { icon: Building2, label: 'Empresa', path: '/configuracoes/empresa' },
```

- [ ] **Step 5: TypeScript check**

```bash
cd frontend && PATH="/opt/homebrew/opt/node/bin:$PATH" ./node_modules/.bin/tsc --noEmit 2>&1
```
Esperado: sem output

- [ ] **Step 6: Commit**

```bash
cd ..
git add frontend/src/
git commit -m "feat: unified company config page with 5 sections at /configuracoes/empresa"
```

---

## Task 4: Frontend — Limpeza das Páginas com Campos Duplicados

**Files:**
- Modify: `frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx`
- Modify: `frontend/src/pages/fiscal/ConfiguracaoTab.tsx`

- [ ] **Step 1: Simplificar AgendamentoPublicoConfig.tsx**

Substituir o conteúdo completo do arquivo pelo seguinte (mantém apenas a seção de política de agendamento + link para empresa):

```tsx
// frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '@/services/api'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { ConfiguracaoEmpresaResponse } from '@/types/fiscal'

export default function AgendamentoPublicoConfig() {
  const [aprovarAutomaticamente, setAprovarAutomaticamente] = useState(true)
  const [valorSinal, setValorSinal] = useState('')
  const [horasLimiteCancelamento, setHorasLimiteCancelamento] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    api.get<ConfiguracaoEmpresaResponse>('/api/configuracao-empresa')
      .then(c => {
        setAprovarAutomaticamente(c.aprovarAutomaticamente)
        setValorSinal(c.valorSinal != null ? String(c.valorSinal) : '')
        setHorasLimiteCancelamento(c.horasLimiteCancelamento != null ? String(c.horasLimiteCancelamento) : '')
      })
      .catch(() => {})
  }, [])

  async function handleSave() {
    setSaving(true)
    try {
      await api.put('/api/configuracao-empresa/agendamento', {
        aprovarAutomaticamente,
        valorSinal: valorSinal ? parseFloat(valorSinal) : null,
        horasLimiteCancelamento: horasLimiteCancelamento ? parseInt(horasLimiteCancelamento) : null,
      })
      toast.success('Configurações salvas!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="space-y-6 max-w-lg">
      <h1 className="text-2xl font-bold">Agendamento Online</h1>

      <p className="text-sm text-muted-foreground">
        Configure logo, cor e slug da página pública em{' '}
        <Link to="/configuracoes/empresa" className="underline text-primary">
          Configuração da Empresa
        </Link>.
      </p>

      <div className="rounded-md border p-4 space-y-4">
        <h2 className="font-semibold">Política de Agendamento</h2>

        <div className="space-y-1.5">
          <div className="flex items-center gap-2">
            <input type="checkbox" id="aprovarAuto" checked={aprovarAutomaticamente}
              onChange={e => setAprovarAutomaticamente(e.target.checked)} className="h-4 w-4" />
            <Label htmlFor="aprovarAuto">Confirmar agendamentos automaticamente</Label>
          </div>
          <p className="text-xs text-muted-foreground">
            Quando desmarcado, novos agendamentos ficam "Aguardando Confirmação".
          </p>
        </div>

        <div className="grid gap-2">
          <Label>Valor do sinal de reserva (R$)</Label>
          <Input type="number" min="0" step="0.01" value={valorSinal}
            onChange={e => setValorSinal(e.target.value)} placeholder="0,00 (sem sinal)" />
          <p className="text-xs text-muted-foreground">
            Cobrado via PIX no momento do agendamento. Requer Asaas configurado.
          </p>
        </div>

        <div className="grid gap-2">
          <Label>Horas mínimas para cancelamento</Label>
          <Input type="number" min="0" value={horasLimiteCancelamento}
            onChange={e => setHorasLimiteCancelamento(e.target.value)}
            placeholder="Ex: 24 (sem restrição se vazio)" />
        </div>
      </div>

      <Button onClick={() => void handleSave()} disabled={saving}>
        {saving ? 'Salvando...' : 'Salvar configurações'}
      </Button>
    </div>
  )
}
```

- [ ] **Step 2: Simplificar ConfiguracaoTab.tsx**

Substituir o conteúdo completo de `frontend/src/pages/fiscal/ConfiguracaoTab.tsx`:

```tsx
// frontend/src/pages/fiscal/ConfiguracaoTab.tsx
import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
import { useConfiguracaoEmpresa } from '@/hooks/useConfiguracaoEmpresa'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import type { AtualizarConfiguracaoEmpresaRequest } from '@/types/fiscal'

const AMBIENTES = [
  { value: 1, label: '1 — Produção' },
  { value: 2, label: '2 — Homologação (testes)' },
]

function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="space-y-1.5">
      <Label className="text-xs text-muted-foreground">{label}</Label>
      {children}
    </div>
  )
}

export default function ConfiguracaoTab() {
  const { config, loading, error, obter, atualizar } = useConfiguracaoEmpresa()
  const [form, setForm] = useState<AtualizarConfiguracaoEmpresaRequest>({})
  const [salvando, setSalvando] = useState(false)
  const [focusNfeToken, setFocusNfeToken] = useState('')

  useEffect(() => {
    void obter().then(c => {
      if (!c) return
      setForm({
        ambiente: c.ambiente ?? 2,
        serieNfe: c.serieNfe ?? 1,
        serieNfce: c.serieNfce ?? 1,
      })
    })
  }, [obter])

  async function handleSalvar() {
    setSalvando(true)
    try {
      const req: AtualizarConfiguracaoEmpresaRequest = { ...form }
      if (focusNfeToken.trim()) req.focusNfeToken = focusNfeToken.trim()
      await atualizar(req)
      setFocusNfeToken('')
      toast.success('Configuração NF-e salva!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSalvando(false)
    }
  }

  if (loading) return <div className="flex h-48 items-center justify-center text-muted-foreground">Carregando...</div>
  if (error) return <div className="flex h-48 items-center justify-center text-destructive">{error}</div>

  return (
    <div className="space-y-6 max-w-2xl">
      <div className="rounded-xl border bg-card p-4">
        <p className="text-sm text-muted-foreground">
          Dados da empresa (razão social, CNPJ, endereço) agora estão em{' '}
          <Link to="/configuracoes/empresa" className="underline text-primary">
            Configuração da Empresa
          </Link>.
        </p>
      </div>

      <h2 className="text-lg font-semibold">Configuração NF-e</h2>

      <div className="rounded-xl border bg-card p-5 space-y-4">
        <div className="grid grid-cols-2 gap-4">
          <Field label="Ambiente">
            <select value={form.ambiente ?? 2}
              onChange={e => setForm(p => ({ ...p, ambiente: Number(e.target.value) }))}
              className="flex h-9 w-full rounded-lg border border-input bg-transparent px-3 py-1 text-sm">
              {AMBIENTES.map(a => <option key={a.value} value={a.value}>{a.label}</option>)}
            </select>
          </Field>
          <Field label="Série NF-e">
            <Input type="number" min="1" value={form.serieNfe ?? 1}
              onChange={e => setForm(p => ({ ...p, serieNfe: Number(e.target.value) }))} />
          </Field>
          <Field label="Série NFC-e">
            <Input type="number" min="1" value={form.serieNfce ?? 1}
              onChange={e => setForm(p => ({ ...p, serieNfce: Number(e.target.value) }))} />
          </Field>
        </div>
        <div className="space-y-1.5">
          <Label className="text-xs text-muted-foreground">
            Token Focus NFe{config?.temToken && <span className="text-green-600 font-medium"> (configurado)</span>}
          </Label>
          <Input type="password" value={focusNfeToken} onChange={e => setFocusNfeToken(e.target.value)}
            placeholder={config?.temToken ? 'Deixe em branco para manter o atual' : 'Cole o token da API Focus NFe'} />
        </div>
      </div>

      <Button onClick={() => void handleSalvar()} disabled={salvando}>
        {salvando ? 'Salvando...' : 'Salvar Configuração NF-e'}
      </Button>
    </div>
  )
}
```

- [ ] **Step 3: TypeScript check final**

```bash
cd frontend && PATH="/opt/homebrew/opt/node/bin:$PATH" ./node_modules/.bin/tsc --noEmit 2>&1
```
Esperado: sem output

- [ ] **Step 4: Rodar todos os testes backend**

```bash
cd ../backend && /usr/local/share/dotnet/dotnet test tests/GestorAI.Tests 2>&1 | tail -5
```
Esperado: `Passed! - Failed: 0`

- [ ] **Step 5: Commit final**

```bash
cd ..
git add frontend/src/
git commit -m "refactor: simplify AgendamentoPublicoConfig and ConfiguracaoTab — data moved to /configuracoes/empresa"
```
