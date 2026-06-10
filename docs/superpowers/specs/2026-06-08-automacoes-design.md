# Automações — Lembretes de Cobrança e Geração Automática

## Goal

Automatizar dois processos hoje manuais:
1. Enviar lembretes de cobrança via WhatsApp (Evolution API) nos momentos certos — antes e após o vencimento.
2. Gerar cobranças mensais automaticamente a partir de contratos ativos, no dia 1 de cada mês.

## Architecture

Um único `AutomacaoHostedService` (BackgroundService .NET) que acorda a cada hora e, ao detectar que são 07h00 UTC e que ainda não rodou hoje, executa dois serviços:

- **`LembreteCobrancaService`** — envia lembretes WhatsApp para cobranças nos pontos de disparo configurados
- **`GeracaoCobrancaService`** — cria cobranças do mês corrente a partir de contratos ativos (apenas no dia 1)

Ambos os serviços iteram todos os tenants via `IgnoreQueryFilters()` e usam `IServiceScopeFactory` para resolver dependências scoped.

**`EvolutionApiService`** é o wrapper HTTP para a Evolution API, seguindo o mesmo padrão do `AsaasService` existente.

## Tech Stack

- .NET 10 Minimal APIs + `BackgroundService` + `PeriodicTimer`
- `IHttpClientFactory` para Evolution API
- EF Core + PostgreSQL (multi-tenant via `HasQueryFilter`)
- React + TypeScript + Tailwind (frontend)

---

## Data Model

### Nova entidade `AutomacaoLog`

```csharp
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

**Propósito:** deduplicação (checar se já enviou antes de enviar) + auditoria (o tenant vê o histórico de envios).

**Índice:** `HasIndex(l => new { l.CobrancaId, l.TipoEvento })` no `OnModelCreating` para eficiência das queries de deduplicação.

### Novos campos em `ConfiguracaoEmpresa`

```csharp
// Evolution API
public string? EvolutionApiUrl { get; set; }
public string? EvolutionApiKey { get; set; }
public string? EvolutionInstance { get; set; }

// Toggles de lembrete (defaults)
public bool Lembrete3dAntes { get; set; } = true;
public bool Lembrete1dAntes { get; set; } = true;
public bool LembreteNoDia { get; set; } = true;
public bool Lembrete1dDepois { get; set; } = true;
public bool Lembrete3dDepois { get; set; } = false;
public bool Lembrete7dDepois { get; set; } = false;
```

### Deduplicação de cobranças geradas

Sem campo extra. Antes de criar uma cobrança para o contrato X no mês M, verifica:
```csharp
db.Cobrancas.Any(c => c.ContratoId == contrato.Id
    && c.DataVencimento.Year == ano
    && c.DataVencimento.Month == mes)
```
Se existir qualquer cobrança naquele mês para aquele contrato, pula.

---

## Backend

### `EvolutionApiService`

Wrapper HTTP seguindo o padrão do `AsaasService`:

```csharp
public class EvolutionApiService(IHttpClientFactory factory)
{
    public async Task<bool> EnviarMensagemAsync(
        string apiUrl, string apiKey, string instance,
        string phone, string text, CancellationToken ct)
    // POST {apiUrl}/message/sendText/{instance}
    // Header: apikey: {apiKey}
    // Body: { "number": "55{phone}", "text": "{text}" }
    // Retorna true se HTTP 2xx
}

public async Task<bool> TestarConexaoAsync(
    string apiUrl, string apiKey, CancellationToken ct)
    // GET {apiUrl}/instance/fetchInstances
    // Retorna true se HTTP 2xx
```

### `LembreteCobrancaService`

Lógica de envio de lembretes. Processa todos os tenants:

1. Carrega todas as `ConfiguracaoEmpresa` com `EvolutionApiUrl` não nulo via `IgnoreQueryFilters()`
2. Para cada tenant, determina os offsets ativos (ex: `-3, -1, 0, +1, +3, +7` dias)
3. Busca cobranças `Status = Pendente` com `DataVencimento` correspondendo a cada offset
4. Para cada cobrança + offset:
   - Checa `AutomacaoLog` — se já existe `(CobrancaId, TipoEvento)`, pula
   - Monta mensagem: `"Olá {Nome}, sua cobrança de R$ {Valor} ({Referencia}) vence em {Data}. ..."`
   - Chama `EvolutionApiService.EnviarMensagemAsync`
   - Grava `AutomacaoLog` com `Sucesso` e `ErroMsg`

### `GeracaoCobrancaService`

Executa apenas quando `DateTime.UtcNow.Day == 1`:

1. Carrega todos os contratos com `Status = Ativo` via `IgnoreQueryFilters()`
2. Para cada contrato:
   - Calcula `DataVencimento = new DateOnly(anoAtual, mesAtual, contrato.DiaVencimento)`
   - Verifica deduplicação (query acima)
   - Se não existe: cria `Cobranca` com `Referencia = $"Mensalidade {mes:00}/{ano}"` (ex: "Mensalidade 07/2026"), `ClienteId`, `ContratoId`, `Valor = contrato.Valor`
   - Grava `AutomacaoLog` com `TipoEvento = CobrancaGerada`

### `AutomacaoHostedService`

```csharp
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

### Novos endpoints

**`FiscalEndpoints.cs`** (grupo `/api`, já existente):
```
PUT  /api/configuracao-empresa/automacao   — salva Evolution config + toggles de lembrete
```

**`AutomacaoEndpoints.cs`** (novo arquivo, grupo `/api/automacao`):
```
GET  /api/automacao/log                    — lista AutomacaoLog do tenant (últimos 100)
POST /api/automacao/testar-conexao         — testa Evolution API connection
```

### Migrations

- `AddAutomacaoLog` — nova tabela + HasQueryFilter
- `AddAutomacaoConfigFields` — 9 campos em ConfiguracaoEmpresa

---

## Frontend

### Página `/configuracoes/automacao`

**Bloco 1 — Integração Evolution API**
- Input: URL da instância
- Input password: API Key
- Input: Nome da instância
- Botão "Testar conexão" → chama `POST /api/automacao/testar-conexao`, exibe ✅/❌
- Botão "Salvar"

**Bloco 2 — Lembretes ativos**
- 6 checkboxes com labels amigáveis:
  - ☑ 3 dias antes do vencimento
  - ☑ 1 dia antes do vencimento
  - ☑ No dia do vencimento
  - ☑ 1 dia após vencimento
  - ☐ 3 dias após vencimento
  - ☐ 7 dias após vencimento
- Botão "Salvar"

### Página `/automacao/log`

Tabela com colunas: Data/hora | Cliente | Referência | Evento | Status | Erro

Filtros: Todos / Apenas falhas

### Sidebar

Novo grupo **"Automação"**:
- `Configurações` → `/configuracoes/automacao`
- `Log de envios` → `/automacao/log`

---

## Error Handling

- Falha no envio Evolution API: grava `Sucesso = false, ErroMsg = ex.Message` e continua para o próximo. Não interrompe o job.
- Cobrança sem WhatsApp do cliente: pula silenciosamente (sem gravar log).
- Evolution não configurado para o tenant: pula o tenant inteiro.

## Testing

- Testes unitários para `LembreteCobrancaService`: cobre deduplicação, offset correto, tenant isolation
- Testes unitários para `GeracaoCobrancaService`: cobre deduplicação, dia 1 only, DateOnly calculation
- `AutomacaoHostedService` não precisa de teste unitário (só wiring)
