# Configuração da Empresa — Design Spec

## Objetivo

Centralizar todos os dados da empresa em uma única página (`/configuracoes/empresa`), eliminando duplicações entre `AgendamentoPublicoConfig`, `ConfiguracaoTab` (fiscal) e os campos visuais removidos do `WhiteLabel`. Aplicar identidade visual (logo, cor, nome) nos PDFs de orçamento e contrato com cabeçalho + rodapé padronizados.

---

## O que será alterado antes da implementação

| Item | Ação |
|---|---|
| Campos visuais em `AgendamentoPublicoConfig.tsx` (logo, cor, slug, nome, descrição, preview) | Remover — migram para nova página |
| Seção "Configuração da Empresa" em `ConfiguracaoTab.tsx` (razão social, nome fantasia, CNPJ, regime, ambiente, séries, token Focus) | Remover — migra para nova página |
| Link "Agendamento Online" no Sidebar | Manter — página ainda existe com política de agendamento |

---

## Modelo de Dados

### Campos novos em `ConfiguracaoEmpresa`

```csharp
public string? Telefone { get; set; }
public string? Email    { get; set; }
```

### Migration

`AddTelefoneEmailEmpresa` — adiciona duas colunas nullable em `ConfiguracaoEmpresa`.

---

## Endpoints Backend

### Existentes — sem alteração de rota

| Rota | Uso na nova página | Mudança |
|---|---|---|
| `GET /api/configuracao-empresa` | Carrega todas as seções | Adicionar `telefone`, `email`, `aprovarAutomaticamente`, `valorSinal`, `horasLimiteCancelamento` ao response |
| `PUT /api/configuracao-empresa` | Salva Identificação + Endereço + NF-e | Adicionar `telefone` e `email` em `AtualizarConfiguracaoEmpresaRequest` e no service |
| `PUT /api/configuracao-empresa/branding` | Salva Identidade Visual | Sem alteração |
| `POST /api/configuracao-empresa/logo` | Upload de logo | Sem alteração |

### Endpoint novo

```
PUT /api/configuracao-empresa/agendamento
```

**Motivação:** `AprovarAutomaticamente`, `ValorSinal` e `HorasLimiteCancelamento` existem na entidade mas nunca foram persistidos — o frontend antigo os enviava para `/branding` e o backend os ignorava silenciosamente (bug).

**Request:**
```csharp
public record SalvarAgendamentoConfigRequest(
    bool AprovarAutomaticamente,
    decimal? ValorSinal,
    int? HorasLimiteCancelamento);
```

**Response:** `200 OK` (sem body)

### DTOs a atualizar

**`AtualizarConfiguracaoEmpresaRequest`** — adicionar:
```csharp
string? Telefone
string? Email
```

**`ConfiguracaoEmpresaResponse`** — adicionar:
```csharp
string? Telefone
string? Email
bool AprovarAutomaticamente
decimal? ValorSinal
int? HorasLimiteCancelamento
```

---

## Documentos PDF — Novo Template Base

Ambos `ContratoService.GetPdfHtmlAsync` e `OrcamentoService.GetPdfHtmlAsync` passam a usar um helper `HtmlDocumentoBase` que injeta cabeçalho e rodapé com dados da empresa.

### Estrutura do helper

```csharp
// Services/Shared/HtmlDocumentoBase.cs
public static class HtmlDocumentoBase
{
    public static string Cabecalho(ConfiguracaoEmpresa? cfg, string apiBase) { ... }
    public static string Rodape(ConfiguracaoEmpresa? cfg) { ... }
    public static string WrapDocument(string titulo, string corpo, ConfiguracaoEmpresa? cfg, string apiBase) { ... }
}
```

### Layout do cabeçalho

```html
<!-- Barra colorida (corPrimaria, fallback #2563eb) -->
<div style="background:{cor}; padding:16px 32px; display:flex; align-items:center; justify-content:space-between;">
  <!-- Esquerda: logo (se houver) -->
  <div>
    <img src="{apiBase}{logoUrl}" style="height:48px; object-fit:contain;" />
    <!-- ou, sem logo: -->
    <span style="color:white; font-size:20px; font-weight:bold;">{nomeExibicao}</span>
  </div>
  <!-- Direita: dados da empresa -->
  <div style="color:white; text-align:right; font-size:12px; line-height:1.6;">
    <div style="font-weight:bold; font-size:14px;">{nomeExibicao}</div>
    <div>CNPJ: {cnpj}</div>
    <div>{telefone} | {email}</div>
  </div>
</div>
```

- `nomeExibicao`: `NomeFantasia` se preenchido, senão `RazaoSocial`, senão `"Empresa"`
- CNPJ/telefone/email omitidos da linha se estiverem vazios
- Logo: path relativo prefixado com `apiBase` (lido de `IConfiguration["ApiBase"]` ou fallback configurável)

### Layout do rodapé

```html
<div style="margin-top:40px; border-top:1px solid #ddd; padding-top:8px; text-align:center; font-size:11px; color:#888;">
  {logradouro}, {numero}{complemento} — {bairro} — {municipio}/{uf} — CEP {cep}
</div>
```

Campos omitidos silenciosamente se vazios. Se nenhum campo de endereço estiver preenchido, o rodapé não é renderizado.

### Integração com os serviços

`OrcamentoService` e `ContratoService` passam a injetar `IConfiguration config` e fazem:

```csharp
var cfg = await db.ConfiguracoesEmpresa
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(c => c.EmpresaId == tenantContext.EmpresaId, ct);
var apiBase = config["ApiBase"] ?? "";
```

O HTML gerado passa a ser:

```csharp
return HtmlDocumentoBase.WrapDocument(titulo, corpoHtml, cfg, apiBase);
```

---

## Frontend — Nova Página `ConfiguracaoEmpresa.tsx`

**Rota:** `/configuracoes/empresa`
**Arquivo:** `frontend/src/pages/configuracoes/ConfiguracaoEmpresa.tsx`

### Estrutura da página

```
/configuracoes/empresa
├── Seção: Identificação
│   ├── Razão Social, Nome Fantasia
│   ├── CNPJ, Inscrição Estadual, Inscrição Municipal
│   ├── Telefone (novo), E-mail (novo)
│   └── [Salvar]  →  PUT /api/configuracao-empresa
│
├── Seção: Endereço
│   ├── Logradouro, Número, Complemento
│   ├── Bairro, Cidade, UF, CEP
│   └── [Salvar]  →  PUT /api/configuracao-empresa
│
├── Seção: Identidade Visual
│   ├── Upload de logo  →  POST /api/configuracao-empresa/logo
│   ├── Cor primária (color picker + hex)
│   ├── Nome de exibição público, Slug, Descrição pública
│   ├── Preview ao vivo do cabeçalho
│   └── [Salvar Visual]  →  PUT /api/configuracao-empresa/branding
│
├── Seção: Emissão de Notas Fiscais
│   ├── Regime Tributário, Ambiente (Produção/Homologação)
│   ├── Série NF-e, Série NFC-e
│   ├── Token Focus NF-e, CSC Id, CSC Token
│   └── [Salvar NF-e]  →  PUT /api/configuracao-empresa
│
└── Seção: Agendamento Online
    ├── Aprovar automaticamente (toggle)
    ├── Valor do sinal (R$)
    ├── Horas mínimas para cancelamento
    └── [Salvar]  →  PUT /api/configuracao-empresa/agendamento  ← endpoint novo (corrige bug)
```

### Carregamento

Um único `GET /api/configuracao-empresa` no `useEffect` inicial popula todos os estados das seções. Cada seção gerencia seu próprio estado local e botão de salvar independente.

---

## Frontend — Páginas Simplificadas

### `AgendamentoPublicoConfig.tsx`

Remove: seções "Identidade Visual", "Link do agendamento" e "Pré-visualização" (migram para empresa).

Mantém: seção "Política de Agendamento" (aprovar auto, valor sinal, horas cancelamento) com link de conveniência: *"Configure logo e cor em [Configuração da Empresa]"*.

### `ConfiguracaoTab.tsx` (dentro de `Fiscal.tsx`)

Remove: seção "Configuração da Empresa" (razão social, nome fantasia, CNPJ, regime, ambiente, séries, token Focus).

Mantém: apenas os campos técnicos de emissão (CSC Id, CSC Token) se ainda necessários fora do novo endpoint, ou remove tudo e fica vazio (tab pode ser eliminada).

---

## Sidebar

Adicionar no grupo **Configurações**:

```tsx
{ icon: Building2, label: 'Empresa', path: '/configuracoes/empresa' }
```

`Building2` já está disponível no lucide-react. Posicionar como primeiro item do grupo.

---

## `appsettings.json`

Adicionar:

```json
"ApiBase": "https://api.gestorai.com.br"
```

Usado pelo `HtmlDocumentoBase` para prefixar URLs relativas de logo nos PDFs.

---

## Decomposição em Tarefas

### Task 1 — Backend: migration + endpoint agendamento + response
- Adicionar `Telefone` e `Email` em `ConfiguracaoEmpresa`; migration `AddTelefoneEmailEmpresa`
- Estender `AtualizarConfiguracaoEmpresaRequest` e service com `Telefone`/`Email`
- Criar `PUT /api/configuracao-empresa/agendamento` com `SalvarAgendamentoConfigRequest` (corrige bug onde esses campos nunca eram persistidos)
- Atualizar `ConfiguracaoEmpresaResponse` com os 5 novos campos

### Task 2 — Backend: HtmlDocumentoBase + PDFs
- Criar `Services/Shared/HtmlDocumentoBase.cs`
- Atualizar `OrcamentoService.GetPdfHtmlAsync` para usar o helper
- Atualizar `ContratoService.GetPdfHtmlAsync` para usar o helper
- Testes: verificar que HTML gerado contém nome da empresa, que funciona sem dados configurados

### Task 3 — Frontend: ConfiguracaoEmpresa.tsx
- Nova página com 5 seções
- Carrega tudo em um GET, salva por seção
- Preview ao vivo na seção de identidade visual (igual ao existente em `AgendamentoPublicoConfig`)

### Task 4 — Frontend: Limpeza e Sidebar
- Remover campos visuais de `AgendamentoPublicoConfig.tsx`
- Remover seção empresa de `ConfiguracaoTab.tsx`
- Adicionar link "Empresa" no Sidebar
- TypeScript check
