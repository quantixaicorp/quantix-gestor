# Agendamentos Avançados Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Adicionar três funcionalidades avançadas ao módulo de agendamentos: (1) toggle de aprovação manual para bookings públicos, (2) cobrança de sinal/depósito de reserva via Asaas, e (3) política de cancelamento com janela de horas configurável.

**Architecture:** Tudo aditivo: campos novos em `ConfiguracaoEmpresa` e `Agendamento`, lógica extra em `PublicBookingService` e `AgendamentoService`. O status `AguardandoConfirmacao` já existe no enum — basta conectá-lo ao toggle de aprovação manual. O sinal é uma `Cobranca` Asaas vinculada ao agendamento. A política de cancelamento é validada em `CancelarAsync`.

**Tech Stack:** .NET 10, EF Core 10, React + TypeScript, Asaas API (já integrada)

---

## File Map

**Backend — modificar:**
- `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs` — 3 novos campos: `AprovarAutomaticamente`, `ValorSinal`, `HorasLimiteCancelamento`
- `backend/src/GestorAI.API/Domain/Entities/Agendamento.cs` — 2 novos campos: `SinalPago`, `SinalAsaasId`
- `backend/src/GestorAI.API/Services/PublicBooking/PublicBookingService.cs` — usar toggle + criar cobrança sinal
- `backend/src/GestorAI.API/Services/Agendamentos/AgendamentoService.cs` — validar política cancelamento
- `backend/src/GestorAI.API/DTOs/PublicBooking/PublicBookingDto.cs` — incluir `SinalPixQrCode` na resposta
- `backend/src/GestorAI.API/Infrastructure/Data/Migrations/` — migration

**Frontend — modificar:**
- `frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx` — adicionar campos de política avançada
- `frontend/src/pages/agendamento-publico/AgendamentoPublico.tsx` — mostrar QR code do sinal se necessário

---

### Task 1: Schema — adicionar campos em `ConfiguracaoEmpresa` e `Agendamento`

**Files:**
- Modify: `backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs`
- Modify: `backend/src/GestorAI.API/Domain/Entities/Agendamento.cs`

- [ ] **Step 1: Adicionar campos em `ConfiguracaoEmpresa.cs`**

Adicionar após os campos de lembrete (ao final da classe):

```csharp
// Configurações avançadas de agendamento
public bool AprovarAutomaticamente { get; set; } = true;
public decimal? ValorSinal { get; set; }
public int? HorasLimiteCancelamento { get; set; }
```

- [ ] **Step 2: Adicionar campos em `Agendamento.cs`**

Adicionar após `public string? Observacao { get; set; }`:

```csharp
public bool SinalPago { get; set; } = false;
public string? SinalAsaasId { get; set; }
public string? SinalPixQrCode { get; set; }
```

- [ ] **Step 3: Gerar migration**

```bash
cd backend
dotnet ef migrations add AddAgendamentosAvancados \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API \
  --output-dir Infrastructure/Data/Migrations
dotnet ef database update \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API
```

Esperado: migration criada e aplicada.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Entities/ConfiguracaoEmpresa.cs \
        backend/src/GestorAI.API/Domain/Entities/Agendamento.cs \
        backend/src/GestorAI.API/Infrastructure/Data/Migrations/
git commit -m "feat: add advanced booking fields to ConfiguracaoEmpresa and Agendamento"
```

---

### Task 2: Backend — aprovação manual e sinal no fluxo público

**Files:**
- Modify: `backend/src/GestorAI.API/Services/PublicBooking/PublicBookingService.cs`
- Modify: `backend/src/GestorAI.API/DTOs/PublicBooking/PublicBookingDto.cs`

**Lógica para `CriarAgendamentoAsync`:**
1. Busca `ConfiguracaoEmpresa` do tenant
2. Se `AprovarAutomaticamente == false`, status inicial = `AguardandoConfirmacao`; senão `Agendado`
3. Se `ValorSinal > 0` e empresa tem `AsaasApiKey`, cria cobrança Asaas para o sinal (PIX), salva `SinalAsaasId` e `SinalPixQrCode` no agendamento
4. Retorna os dados da reserva incluindo `SinalPixQrCode` se houver

- [ ] **Step 1: Verificar assinatura de `CriarAgendamentoAsync` em `PublicBookingService`**

```bash
grep -n "CriarAgendamentoAsync\|public.*Criar\|AsaasService\|ConfiguracaoEmpresa" \
  backend/src/GestorAI.API/Services/PublicBooking/PublicBookingService.cs | head -20
```

- [ ] **Step 2: Atualizar `PublicBookingDto.cs` para incluir campo sinal**

Verificar `PublicCriarAgendamentoResponse` (ou nome equivalente) no arquivo:

```bash
cat backend/src/GestorAI.API/DTOs/PublicBooking/PublicBookingDto.cs
```

Adicionar `SinalPixQrCode string?` e `SinalValor decimal?` ao record de resposta do booking público.

- [ ] **Step 3: Modificar `CriarAgendamentoAsync`**

Buscar o config da empresa antes de criar o agendamento:

```csharp
var config = await db.ConfiguracoesEmpresa
    .IgnoreQueryFilters()
    .FirstOrDefaultAsync(c => c.EmpresaId == empresaId, ct);

var statusInicial = config?.AprovarAutomaticamente == false
    ? AgendamentoStatus.AguardandoConfirmacao
    : AgendamentoStatus.Agendado;

var agendamento = new Agendamento
{
    // ... campos existentes ...
    Status = statusInicial,
};
db.Agendamentos.Add(agendamento);
await db.SaveChangesAsync(ct);

string? sinalPixQrCode = null;
decimal? sinalValor = null;

if (config?.ValorSinal > 0 && !string.IsNullOrWhiteSpace(config.AsaasApiKey))
{
    try
    {
        var customerId = await asaasService.GetOrCreateCustomerAsync(
            config.AsaasApiKey, config.AsaasSandbox,
            req.ClienteNome, null, ct);

        var sinalVenc = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(24));
        var result = await asaasService.CreatePaymentAsync(
            config.AsaasApiKey, config.AsaasSandbox,
            customerId, config.ValorSinal.Value, sinalVenc,
            $"Sinal de reserva — {servico.Nome}", "PIX", ct);

        agendamento.SinalAsaasId = result.Id;
        agendamento.SinalPixQrCode = result.PixQrCode?.Payload;
        await db.SaveChangesAsync(ct);

        sinalPixQrCode = result.PixQrCode?.Payload;
        sinalValor = config.ValorSinal;
    }
    catch
    {
        // sinal é opcional — não bloquear booking se Asaas falhar
    }
}
```

Retornar `sinalPixQrCode` e `sinalValor` no response.

- [ ] **Step 4: Build**

```bash
cd backend && dotnet build src/GestorAI.API --no-restore 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Services/PublicBooking/PublicBookingService.cs \
        backend/src/GestorAI.API/DTOs/PublicBooking/PublicBookingDto.cs
git commit -m "feat: add manual approval toggle and booking deposit (sinal) to public booking flow"
```

---

### Task 3: Backend — política de cancelamento em `AgendamentoService.CancelarAsync`

**Files:**
- Modify: `backend/src/GestorAI.API/Services/Agendamentos/AgendamentoService.cs`
- Create: `backend/tests/GestorAI.Tests/Services/AgendamentoCancelamentoTests.cs`

A política: se `HorasLimiteCancelamento` está configurado, cancelamentos dentro da janela (agendamento começa em menos de X horas) são bloqueados com `AppException`. Esta validação aplica apenas ao cancelamento público; a empresa interna pode sempre cancelar.

- [ ] **Step 1: Escrever teste**

Criar `backend/tests/GestorAI.Tests/Services/AgendamentoCancelamentoTests.cs`:

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Agendamentos;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class AgendamentoCancelamentoTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private AppDbContext CreateDb(int? horasLimite = null)
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);

        db.ConfiguracoesEmpresa.Add(new ConfiguracaoEmpresa
        {
            EmpresaId = _empresaId,
            HorasLimiteCancelamento = horasLimite,
        });
        db.SaveChanges();
        return db;
    }

    private async Task<Agendamento> CriarAgendamentoAsync(AppDbContext db, DateTime inicio)
    {
        var profissional = new Profissional
        {
            EmpresaId = _empresaId,
            Nome = "Dr. Carlos",
        };
        db.Profissionais.Add(profissional);

        var servico = new Produto
        {
            EmpresaId = _empresaId,
            Nome = "Consulta",
            Tipo = TipoProduto.Servico,
            Preco = 100m,
            DuracaoMinutos = 30,
        };
        db.Produtos.Add(servico);
        await db.SaveChangesAsync();

        var agendamento = new Agendamento
        {
            EmpresaId = _empresaId,
            ProfissionalId = profissional.Id,
            ServicoId = servico.Id,
            ClienteNome = "Maria",
            ClienteTelefone = "11999990000",
            DataHoraInicio = inicio,
            DataHoraFim = inicio.AddMinutes(30),
            Status = AgendamentoStatus.Agendado,
        };
        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync();
        return agendamento;
    }

    [Fact]
    public async Task CancelarAsync_Bloqueia_DentroJanelaCancelamento()
    {
        var db = CreateDb(horasLimite: 24);
        // Agendamento daqui 2 horas — dentro da janela de 24h
        var inicio = DateTime.UtcNow.AddHours(2);
        var ag = await CriarAgendamentoAsync(db, inicio);

        var svc = new AgendamentoService(db, new TenantContext { EmpresaId = _empresaId });
        await Assert.ThrowsAsync<AppException>(() => svc.CancelarPublicoAsync(ag.Id, default));
    }

    [Fact]
    public async Task CancelarAsync_Permite_ForaDaJanelaCancelamento()
    {
        var db = CreateDb(horasLimite: 24);
        // Agendamento daqui 48 horas — fora da janela de 24h
        var inicio = DateTime.UtcNow.AddHours(48);
        var ag = await CriarAgendamentoAsync(db, inicio);

        var svc = new AgendamentoService(db, new TenantContext { EmpresaId = _empresaId });
        var result = await svc.CancelarPublicoAsync(ag.Id, default);
        Assert.Equal("Cancelado", result.Status);
    }

    [Fact]
    public async Task CancelarAsync_Permite_SemPoliticaConfigurada()
    {
        var db = CreateDb(horasLimite: null);
        var inicio = DateTime.UtcNow.AddHours(1);
        var ag = await CriarAgendamentoAsync(db, inicio);

        var svc = new AgendamentoService(db, new TenantContext { EmpresaId = _empresaId });
        var result = await svc.CancelarPublicoAsync(ag.Id, default);
        Assert.Equal("Cancelado", result.Status);
    }
}
```

- [ ] **Step 2: Rodar para confirmar falha**

```bash
cd backend
dotnet test tests/GestorAI.Tests --filter "AgendamentoCancelamentoTests" --no-build 2>&1 | tail -20
```

Esperado: falha com `AgendamentoService does not contain definition for CancelarPublicoAsync`.

- [ ] **Step 3: Adicionar `CancelarPublicoAsync` em `AgendamentoService.cs`**

O método existente `CancelarAsync` é usado internamente (sem restrição). Adicionar método separado para fluxo público:

```csharp
public async Task<AgendamentoResponse> CancelarPublicoAsync(Guid id, CancellationToken ct)
{
    var a = await FindAsync(id, ct);
    if (a.Status == AgendamentoStatus.Cancelado)
        throw new AppException("Agendamento já está cancelado.", 400);
    if (a.Status == AgendamentoStatus.Concluido)
        throw new AppException("Agendamento já concluído não pode ser cancelado.", 400);

    var config = await db.ConfiguracoesEmpresa
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(c => c.EmpresaId == a.EmpresaId, ct);

    if (config?.HorasLimiteCancelamento is int horas)
    {
        var horasAteAgendamento = (a.DataHoraInicio - DateTime.UtcNow).TotalHours;
        if (horasAteAgendamento < horas)
            throw new AppException(
                $"Cancelamentos devem ser feitos com pelo menos {horas} hora(s) de antecedência.", 400);
    }

    a.Status = AgendamentoStatus.Cancelado;
    await db.SaveChangesAsync(ct);
    return await GetAsync(id, ct);
}
```

- [ ] **Step 4: Expor `CancelarPublicoAsync` no endpoint público**

Em `PublicBookingEndpoints.cs`, adicionar:

```csharp
group.MapPost("/agendamentos/{id:guid}/cancelar", async (
    string slug, Guid id,
    PublicBookingService svc, AgendamentoService agendamentoSvc, CancellationToken ct) =>
{
    await svc.ResolveEmpresaAsync(slug, ct); // valida que o slug existe
    return Results.Ok(await agendamentoSvc.CancelarPublicoAsync(id, ct));
});
```

**Nota:** `AgendamentoService` já usa `TenantContext` que é resolvido automaticamente pelo multi-tenant middleware.

- [ ] **Step 5: Build e testes**

```bash
cd backend
dotnet build src/GestorAI.API --no-restore 2>&1 | tail -10
dotnet test tests/GestorAI.Tests --filter "AgendamentoCancelamentoTests" 2>&1 | tail -20
```

Esperado: 3 testes passando.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Services/Agendamentos/AgendamentoService.cs \
        backend/src/GestorAI.API/Endpoints/PublicBookingEndpoints.cs \
        backend/tests/GestorAI.Tests/Services/AgendamentoCancelamentoTests.cs
git commit -m "feat: add cancellation policy and public cancel endpoint for bookings"
```

---

### Task 4: Frontend — configuração e exibição

**Files:**
- Modify: `frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx`
- Modify: `frontend/src/pages/agendamento-publico/AgendamentoPublico.tsx`

- [ ] **Step 1: Verificar estado atual de `AgendamentoPublicoConfig.tsx`**

```bash
cat frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx
```

- [ ] **Step 2: Adicionar campos avançados em `AgendamentoPublicoConfig.tsx`**

Após os campos já existentes (slug, logo, cor, descrição), adicionar nova seção:

```tsx
<div className="rounded-md border p-4 space-y-4">
  <h2 className="font-semibold">Política de Agendamento</h2>

  <div className="flex items-center gap-2">
    <input
      type="checkbox"
      id="aprovarAuto"
      checked={config.aprovarAutomaticamente}
      onChange={e => setConfig(c => ({ ...c, aprovarAutomaticamente: e.target.checked }))}
    />
    <Label htmlFor="aprovarAuto">Confirmar agendamentos automaticamente</Label>
  </div>
  <p className="text-xs text-muted-foreground">
    Quando desmarcado, novos agendamentos ficam "Aguardando Confirmação" até você aprovar manualmente.
  </p>

  <div className="grid gap-2">
    <Label>Valor do sinal de reserva (R$)</Label>
    <Input
      type="number"
      min="0"
      step="0.01"
      value={config.valorSinal ?? ''}
      onChange={e => setConfig(c => ({ ...c, valorSinal: e.target.value ? Number(e.target.value) : null }))}
      placeholder="0,00 (sem sinal)"
    />
    <p className="text-xs text-muted-foreground">
      Valor cobrado via PIX no momento do agendamento. Requer configuração do Asaas.
    </p>
  </div>

  <div className="grid gap-2">
    <Label>Horas mínimas para cancelamento</Label>
    <Input
      type="number"
      min="0"
      value={config.horasLimiteCancelamento ?? ''}
      onChange={e => setConfig(c => ({ ...c, horasLimiteCancelamento: e.target.value ? Number(e.target.value) : null }))}
      placeholder="Ex: 24 (sem restrição se vazio)"
    />
    <p className="text-xs text-muted-foreground">
      Clientes não poderão cancelar agendamentos com menos de X horas de antecedência.
    </p>
  </div>
</div>
```

Atualizar a interface de config e o PUT de salvar para incluir os novos campos (`aprovarAutomaticamente`, `valorSinal`, `horasLimiteCancelamento`).

Verificar qual endpoint PUT recebe as configurações de agendamento público:

```bash
grep -n "agendamento-publico\|ConfigAgendamento\|PUT.*configuracao" \
  backend/src/GestorAI.API/Endpoints/ConfiguracaoEndpoints.cs 2>/dev/null | head -10
```

- [ ] **Step 3: Atualizar backend para aceitar novos campos no PUT de config de agendamento**

Verificar o DTO e handler de configuração de agendamento público, adicionar os 3 novos campos.

- [ ] **Step 4: Mostrar QR code do sinal em `AgendamentoPublico.tsx`**

Na tela de confirmação do agendamento, verificar se a resposta tem `sinalPixQrCode`. Se sim, mostrar:

```tsx
{resultado.sinalPixQrCode && (
  <div className="rounded-md border border-amber-300 bg-amber-50 p-4 space-y-2">
    <p className="font-semibold text-amber-800">Pagamento de sinal necessário</p>
    <p className="text-sm text-amber-700">
      Copie o código PIX abaixo para confirmar sua reserva:
    </p>
    <div className="flex items-center gap-2">
      <code className="flex-1 bg-white border rounded px-2 py-1 text-xs break-all">
        {resultado.sinalPixQrCode}
      </code>
      <button
        className="text-xs text-primary underline"
        onClick={() => void navigator.clipboard.writeText(resultado.sinalPixQrCode!)}
      >
        Copiar
      </button>
    </div>
    {resultado.sinalValor && (
      <p className="text-sm font-medium text-amber-800">
        Valor: {resultado.sinalValor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
      </p>
    )}
  </div>
)}
```

- [ ] **Step 5: Build TypeScript**

```bash
cd frontend && npx tsc --noEmit 2>&1 | tail -20
```

- [ ] **Step 6: Commit**

```bash
git add frontend/src/pages/configuracoes/AgendamentoPublicoConfig.tsx \
        frontend/src/pages/agendamento-publico/AgendamentoPublico.tsx
git commit -m "feat: add advanced booking policy settings and sinal QR code display"
```

---

## Checklist final

- [ ] Toggle `AprovarAutomaticamente` afeta status inicial de bookings públicos
- [ ] `ValorSinal > 0` gera cobrança Asaas PIX e retorna QR code
- [ ] QR code do sinal exibido na tela de confirmação pública
- [ ] `HorasLimiteCancelamento` bloqueia cancelamentos dentro da janela
- [ ] Endpoint público de cancelamento (`POST /public/{slug}/agendamentos/{id}/cancelar`) funcional
- [ ] Campos configuráveis em `AgendamentoPublicoConfig`
- [ ] 3 testes de cancelamento passando
- [ ] `dotnet build` e `npx tsc --noEmit` sem erros
