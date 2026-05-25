# Agendamentos — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement the Agendamentos module (Sprint 1 — internal ERP) covering professional management, weekly availability templates, exception blocks, appointment scheduling with conflict validation, and conversion to Venda on completion.

**Architecture:** Four new backend entities (Profissional, DisponibilidadeSemanal, BloqueioAgenda, Agendamento) + `DuracaoMinutos` field on Produto. Backend follows the existing Service + Endpoint + DTO pattern. Frontend adds two hooks, five pages, and a test file, wired into the existing router and sidebar.

**Tech Stack:** .NET 10 Minimal API, EF Core 9 + Npgsql, FluentValidation, React 18 + TypeScript strict, shadcn/ui, Vitest + Testing Library.

---

## File Map

### Backend — create
- `Domain/Entities/Profissional.cs`
- `Domain/Entities/DisponibilidadeSemanal.cs`
- `Domain/Entities/BloqueioAgenda.cs`
- `Domain/Entities/Agendamento.cs`
- `Domain/Enums/AgendamentoStatus.cs`
- `DTOs/Agendamentos/AgendamentoDto.cs`
- `DTOs/Agendamentos/ProfissionalDto.cs`
- `Services/Agendamentos/ProfissionalService.cs`
- `Services/Agendamentos/AgendamentoService.cs`
- `Services/Agendamentos/CriarAgendamentoValidator.cs`
- `Endpoints/ProfissionaisEndpoints.cs`
- `Endpoints/AgendamentosEndpoints.cs`

### Backend — modify
- `Domain/Entities/Produto.cs` — add `DuracaoMinutos int?`
- `DTOs/Estoque/ProdutoDto.cs` — add `DuracaoMinutos int?` to response + update request
- `Services/Estoque/ProdutoService.cs` — propagate DuracaoMinutos in ToResponse + UpdateAsync
- `Infrastructure/Data/AppDbContext.cs` — add DbSets + HasQueryFilters
- `Program.cs` — register services + map endpoints

### Frontend — create
- `src/types/agendamento.ts`
- `src/hooks/useAgendamentos.ts`
- `src/hooks/useProfissionais.ts`
- `src/pages/agendamentos/Agendamentos.tsx`
- `src/pages/agendamentos/NovoAgendamento.tsx`
- `src/pages/agendamentos/DetalheAgendamento.tsx`
- `src/pages/profissionais/Profissionais.tsx`
- `src/pages/profissionais/DisponibilidadeProfissional.tsx`
- `src/test/agendamentos.test.tsx`

### Frontend — modify
- `src/types/estoque.ts` — add `duracaoMinutos?: number` to ProdutoResponse and UpdateProdutoRequest
- `src/components/layout/Sidebar.tsx`
- `src/router/index.tsx`

---

## Task 1: Domain entities and enum

**Files:**
- Create: `backend/src/GestorAI.API/Domain/Entities/Profissional.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/DisponibilidadeSemanal.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/BloqueioAgenda.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/Agendamento.cs`
- Create: `backend/src/GestorAI.API/Domain/Enums/AgendamentoStatus.cs`

- [ ] **Step 1: Create the enum**

```csharp
// backend/src/GestorAI.API/Domain/Enums/AgendamentoStatus.cs
namespace GestorAI.API.Domain.Enums;

public enum AgendamentoStatus
{
    Agendado,
    Confirmado,
    Concluido,
    Cancelado,
}
```

- [ ] **Step 2: Create Profissional entity**

```csharp
// backend/src/GestorAI.API/Domain/Entities/Profissional.cs
namespace GestorAI.API.Domain.Entities;

public class Profissional : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public string? Telefone { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<DisponibilidadeSemanal> Disponibilidades { get; set; } = [];
    public ICollection<Agendamento> Agendamentos { get; set; } = [];
}
```

- [ ] **Step 3: Create DisponibilidadeSemanal entity**

Note: does NOT implement `ITenantEntity` — always accessed through ProfissionalId FK, which is already tenant-scoped.

```csharp
// backend/src/GestorAI.API/Domain/Entities/DisponibilidadeSemanal.cs
namespace GestorAI.API.Domain.Entities;

public class DisponibilidadeSemanal
{
    public Guid Id { get; set; }
    public Guid ProfissionalId { get; set; }
    public int DiaSemana { get; set; }  // 0=Dom … 6=Sab
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFim { get; set; }
    public Profissional? Profissional { get; set; }
}
```

- [ ] **Step 4: Create BloqueioAgenda entity**

```csharp
// backend/src/GestorAI.API/Domain/Entities/BloqueioAgenda.cs
namespace GestorAI.API.Domain.Entities;

public class BloqueioAgenda : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? ProfissionalId { get; set; }  // null = bloqueia todos
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string? Motivo { get; set; }
    public Profissional? Profissional { get; set; }
}
```

- [ ] **Step 5: Create Agendamento entity**

```csharp
// backend/src/GestorAI.API/Domain/Entities/Agendamento.cs
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Agendamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ProfissionalId { get; set; }
    public required string ClienteNome { get; set; }
    public required string ClienteTelefone { get; set; }
    public Guid? ClienteId { get; set; }
    public Guid ServicoId { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public AgendamentoStatus Status { get; set; } = AgendamentoStatus.Agendado;
    public string? Observacao { get; set; }
    public Guid? VendaId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Profissional? Profissional { get; set; }
    public Produto? Servico { get; set; }
    public Cliente? Cliente { get; set; }
}
```

- [ ] **Step 6: Build to verify compilation**

```bash
cd backend && dotnet build src/GestorAI.API/GestorAI.API.csproj
```

Expected: Build succeeded with 0 errors.

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/Domain/
git commit -m "feat: add Agendamentos domain entities and enum"
```

---

## Task 2: DTOs

**Files:**
- Create: `backend/src/GestorAI.API/DTOs/Agendamentos/AgendamentoDto.cs`
- Create: `backend/src/GestorAI.API/DTOs/Agendamentos/ProfissionalDto.cs`

- [ ] **Step 1: Create AgendamentoDto.cs**

```csharp
// backend/src/GestorAI.API/DTOs/Agendamentos/AgendamentoDto.cs
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Agendamentos;

public record CriarAgendamentoRequest(
    Guid ProfissionalId,
    string ClienteNome,
    string ClienteTelefone,
    Guid? ClienteId,
    Guid ServicoId,
    DateTime DataHoraInicio,
    string? Observacao);

public record AtualizarAgendamentoRequest(string? Observacao);

public record AgendamentoListItem(
    Guid Id,
    Guid ProfissionalId,
    string ProfissionalNome,
    string ClienteNome,
    string ServicoNome,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    AgendamentoStatus Status);

public record AgendamentoResponse(
    Guid Id,
    string ProfissionalNome,
    string ClienteNome,
    string ClienteTelefone,
    Guid? ClienteId,
    string ServicoNome,
    int DuracaoMinutos,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    AgendamentoStatus Status,
    string? Observacao,
    Guid? VendaId,
    DateTime CriadoEm);

public record ConcluirResponse(Guid VendaId);
```

- [ ] **Step 2: Create ProfissionalDto.cs**

```csharp
// backend/src/GestorAI.API/DTOs/Agendamentos/ProfissionalDto.cs
namespace GestorAI.API.DTOs.Agendamentos;

public record CriarProfissionalRequest(string Nome, string? Telefone);

public record AtualizarProfissionalRequest(string Nome, string? Telefone, bool Ativo);

public record ProfissionalResponse(Guid Id, string Nome, string? Telefone, bool Ativo);

public record DisponibilidadeItem(int DiaSemana, string HoraInicio, string HoraFim);

public record SalvarDisponibilidadeRequest(List<DisponibilidadeItem> Faixas);

public record CriarBloqueioRequest(
    Guid? ProfissionalId,
    DateTime DataInicio,
    DateTime DataFim,
    string? Motivo);

public record BloqueioResponse(
    Guid Id,
    Guid? ProfissionalId,
    string? ProfissionalNome,
    DateTime DataInicio,
    DateTime DataFim,
    string? Motivo);
```

- [ ] **Step 3: Build to verify**

```bash
cd backend && dotnet build src/GestorAI.API/GestorAI.API.csproj
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Agendamentos/
git commit -m "feat: add Agendamentos DTOs"
```

---

## Task 3: Add DuracaoMinutos to Produto

**Files:**
- Modify: `backend/src/GestorAI.API/Domain/Entities/Produto.cs`
- Modify: `backend/src/GestorAI.API/DTOs/Estoque/ProdutoDto.cs`
- Modify: `backend/src/GestorAI.API/Services/Estoque/ProdutoService.cs`
- Modify: `frontend/src/types/estoque.ts`

- [ ] **Step 1: Add field to entity**

In `backend/src/GestorAI.API/Domain/Entities/Produto.cs`, add after `AtualizadoEm`:

```csharp
public int? DuracaoMinutos { get; set; }
```

- [ ] **Step 2: Update ProdutoResponse and UpdateProdutoRequest DTOs**

In `backend/src/GestorAI.API/DTOs/Estoque/ProdutoDto.cs`, replace the file with:

```csharp
namespace GestorAI.API.DTOs.Estoque;

public record ProdutoResponse(
    Guid Id,
    Guid CategoriaId,
    string CategoriaNome,
    string Nome,
    string? Descricao,
    decimal PrecoVenda,
    decimal CustoMedio,
    decimal EstoqueAtual,
    decimal EstoqueMinimo,
    string? CodigoBarras,
    bool Ativo,
    bool EstoqueBaixo,
    int? DuracaoMinutos);

public record CreateProdutoRequest(
    Guid CategoriaId,
    string Nome,
    string? Descricao,
    decimal PrecoVenda,
    decimal CustoMedio,
    decimal EstoqueAtual,
    decimal EstoqueMinimo,
    string? CodigoBarras);

public record UpdateProdutoRequest(
    Guid CategoriaId,
    string Nome,
    string? Descricao,
    decimal PrecoVenda,
    decimal EstoqueMinimo,
    string? CodigoBarras,
    bool Ativo,
    int? DuracaoMinutos);

public record EntradaEstoqueRequest(
    Guid ProdutoId,
    decimal Quantidade,
    decimal? CustoUnitario,
    string? Observacao);
```

- [ ] **Step 3: Update ProdutoService.ToResponse and UpdateAsync**

In `backend/src/GestorAI.API/Services/Estoque/ProdutoService.cs`:

Replace `ToResponse` static method:

```csharp
private static ProdutoResponse ToResponse(Produto p) => new(
    p.Id, p.CategoriaId, p.Categoria?.Nome ?? "",
    p.Nome, p.Descricao, p.PrecoVenda, p.CustoMedio,
    p.EstoqueAtual, p.EstoqueMinimo, p.CodigoBarras,
    p.Ativo, p.EstoqueAtual <= p.EstoqueMinimo, p.DuracaoMinutos);
```

Add `produto.DuracaoMinutos = req.DuracaoMinutos;` inside `UpdateAsync`, after `produto.Ativo = req.Ativo;`.

- [ ] **Step 4: Update frontend types/estoque.ts**

Add `duracaoMinutos?: number | null` to `ProdutoResponse`:

```typescript
export interface ProdutoResponse {
  id: string
  categoriaId: string
  categoriaNome: string
  nome: string
  descricao: string | null
  precoVenda: number
  custoMedio: number
  estoqueAtual: number
  estoqueMinimo: number
  codigoBarras: string | null
  ativo: boolean
  estoqueBaixo: boolean
  duracaoMinutos: number | null
}

export interface UpdateProdutoRequest {
  categoriaId: string
  nome: string
  descricao?: string
  precoVenda: number
  estoqueMinimo: number
  codigoBarras?: string
  ativo: boolean
  duracaoMinutos?: number | null
}
```

Keep the rest of the file unchanged.

- [ ] **Step 5: Build backend**

```bash
cd backend && dotnet build src/GestorAI.API/GestorAI.API.csproj
```

Expected: Build succeeded.

- [ ] **Step 6: Build frontend**

```bash
cd frontend && npm run build
```

Expected: Build succeeded, no TypeScript errors.

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Entities/Produto.cs \
        backend/src/GestorAI.API/DTOs/Estoque/ProdutoDto.cs \
        backend/src/GestorAI.API/Services/Estoque/ProdutoService.cs \
        frontend/src/types/estoque.ts
git commit -m "feat: add DuracaoMinutos to Produto for service configuration"
```

---

## Task 4: ProfissionalService

**Files:**
- Create: `backend/src/GestorAI.API/Services/Agendamentos/ProfissionalService.cs`

- [ ] **Step 1: Create ProfissionalService.cs**

```csharp
// backend/src/GestorAI.API/Services/Agendamentos/ProfissionalService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Agendamentos;

public class ProfissionalService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ProfissionalResponse>> ListAsync(CancellationToken ct) =>
        await db.Profissionais
            .OrderBy(p => p.Nome)
            .Select(p => new ProfissionalResponse(p.Id, p.Nome, p.Telefone, p.Ativo))
            .ToListAsync(ct);

    public async Task<ProfissionalResponse> CreateAsync(CriarProfissionalRequest req, CancellationToken ct)
    {
        var p = new Profissional
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            Telefone = req.Telefone,
        };
        db.Profissionais.Add(p);
        await db.SaveChangesAsync(ct);
        return new ProfissionalResponse(p.Id, p.Nome, p.Telefone, p.Ativo);
    }

    public async Task<ProfissionalResponse> UpdateAsync(Guid id, AtualizarProfissionalRequest req, CancellationToken ct)
    {
        var p = await db.Profissionais.FindAsync([id], ct)
            ?? throw new AppException("Profissional não encontrado.", 404);
        p.Nome = req.Nome;
        p.Telefone = req.Telefone;
        p.Ativo = req.Ativo;
        await db.SaveChangesAsync(ct);
        return new ProfissionalResponse(p.Id, p.Nome, p.Telefone, p.Ativo);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Profissionais.FindAsync([id], ct)
            ?? throw new AppException("Profissional não encontrado.", 404);
        var temAgendamentos = await db.Agendamentos
            .AnyAsync(a => a.ProfissionalId == id && a.Status != Domain.Enums.AgendamentoStatus.Cancelado, ct);
        if (temAgendamentos)
            throw new AppException("Profissional possui agendamentos ativos e não pode ser excluído.", 400);
        db.Profissionais.Remove(p);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<DisponibilidadeItem>> GetDisponibilidadeAsync(Guid id, CancellationToken ct)
    {
        _ = await db.Profissionais.FindAsync([id], ct)
            ?? throw new AppException("Profissional não encontrado.", 404);
        return await db.DisponibilidadeSemanais
            .Where(d => d.ProfissionalId == id)
            .OrderBy(d => d.DiaSemana).ThenBy(d => d.HoraInicio)
            .Select(d => new DisponibilidadeItem(
                d.DiaSemana,
                $"{d.HoraInicio.Hours:D2}:{d.HoraInicio.Minutes:D2}",
                $"{d.HoraFim.Hours:D2}:{d.HoraFim.Minutes:D2}"))
            .ToListAsync(ct);
    }

    public async Task SalvarDisponibilidadeAsync(Guid id, SalvarDisponibilidadeRequest req, CancellationToken ct)
    {
        _ = await db.Profissionais.FindAsync([id], ct)
            ?? throw new AppException("Profissional não encontrado.", 404);

        var existentes = await db.DisponibilidadeSemanais
            .Where(d => d.ProfissionalId == id)
            .ToListAsync(ct);
        db.DisponibilidadeSemanais.RemoveRange(existentes);

        foreach (var faixa in req.Faixas)
        {
            if (!TimeSpan.TryParseExact(faixa.HoraInicio, @"hh\:mm", null, out var inicio))
                throw new AppException($"HoraInicio inválida: {faixa.HoraInicio}");
            if (!TimeSpan.TryParseExact(faixa.HoraFim, @"hh\:mm", null, out var fim))
                throw new AppException($"HoraFim inválida: {faixa.HoraFim}");
            if (fim <= inicio)
                throw new AppException("HoraFim deve ser posterior a HoraInicio.");

            db.DisponibilidadeSemanais.Add(new DisponibilidadeSemanal
            {
                ProfissionalId = id,
                DiaSemana = faixa.DiaSemana,
                HoraInicio = inicio,
                HoraFim = fim,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<List<BloqueioResponse>> ListBloqueiosAsync(DateTime de, DateTime ate, CancellationToken ct) =>
        await db.BloqueiosAgenda
            .Include(b => b.Profissional)
            .Where(b => b.DataInicio < ate && b.DataFim > de)
            .OrderBy(b => b.DataInicio)
            .Select(b => new BloqueioResponse(
                b.Id, b.ProfissionalId, b.Profissional != null ? b.Profissional.Nome : null,
                b.DataInicio, b.DataFim, b.Motivo))
            .ToListAsync(ct);

    public async Task<BloqueioResponse> CriarBloqueioAsync(CriarBloqueioRequest req, CancellationToken ct)
    {
        if (req.DataFim <= req.DataInicio)
            throw new AppException("DataFim deve ser posterior a DataInicio.");

        var b = new BloqueioAgenda
        {
            EmpresaId = tenantContext.EmpresaId,
            ProfissionalId = req.ProfissionalId,
            DataInicio = req.DataInicio,
            DataFim = req.DataFim,
            Motivo = req.Motivo,
        };
        db.BloqueiosAgenda.Add(b);
        await db.SaveChangesAsync(ct);

        string? nomeProfissional = null;
        if (req.ProfissionalId.HasValue)
        {
            var prof = await db.Profissionais.FindAsync([req.ProfissionalId.Value], ct);
            nomeProfissional = prof?.Nome;
        }
        return new BloqueioResponse(b.Id, b.ProfissionalId, nomeProfissional, b.DataInicio, b.DataFim, b.Motivo);
    }

    public async Task DeleteBloqueioAsync(Guid id, CancellationToken ct)
    {
        var b = await db.BloqueiosAgenda.FindAsync([id], ct)
            ?? throw new AppException("Bloqueio não encontrado.", 404);
        db.BloqueiosAgenda.Remove(b);
        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
cd backend && dotnet build src/GestorAI.API/GestorAI.API.csproj
```

Expected: Build succeeded. (Note: `db.Profissionais`, `db.DisponibilidadeSemanais`, `db.BloqueiosAgenda` will show CS1061 errors until AppDbContext is updated in Task 7 — that is expected at this stage.)

- [ ] **Step 3: Commit**

```bash
git add backend/src/GestorAI.API/Services/Agendamentos/ProfissionalService.cs
git commit -m "feat: add ProfissionalService with availability and block management"
```

---

## Task 5: AgendamentoService

**Files:**
- Create: `backend/src/GestorAI.API/Services/Agendamentos/AgendamentoService.cs`
- Create: `backend/src/GestorAI.API/Services/Agendamentos/CriarAgendamentoValidator.cs`

- [ ] **Step 1: Create CriarAgendamentoValidator.cs**

```csharp
// backend/src/GestorAI.API/Services/Agendamentos/CriarAgendamentoValidator.cs
using FluentValidation;
using GestorAI.API.DTOs.Agendamentos;

namespace GestorAI.API.Services.Agendamentos;

public class CriarAgendamentoValidator : AbstractValidator<CriarAgendamentoRequest>
{
    public CriarAgendamentoValidator()
    {
        RuleFor(x => x.ProfissionalId).NotEmpty();
        RuleFor(x => x.ClienteNome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ClienteTelefone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.ServicoId).NotEmpty();
        RuleFor(x => x.DataHoraInicio).GreaterThan(DateTime.UtcNow.AddMinutes(-5))
            .WithMessage("DataHoraInicio deve ser no futuro.");
    }
}
```

- [ ] **Step 2: Create AgendamentoService.cs**

```csharp
// backend/src/GestorAI.API/Services/Agendamentos/AgendamentoService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Agendamentos;

public class AgendamentoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<AgendamentoListItem>> ListAsync(DateOnly data, CancellationToken ct)
    {
        var inicio = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = inicio.AddDays(1);

        return await db.Agendamentos
            .Include(a => a.Profissional)
            .Include(a => a.Servico)
            .Where(a => a.DataHoraInicio >= inicio && a.DataHoraInicio < fim)
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
    }

    public async Task<AgendamentoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var a = await db.Agendamentos
            .Include(a => a.Profissional)
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Agendamento não encontrado.", 404);
        return ToResponse(a);
    }

    public async Task<AgendamentoResponse> CriarAsync(CriarAgendamentoRequest req, CancellationToken ct)
    {
        _ = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == req.ProfissionalId, ct)
            ?? throw new AppException("Profissional não encontrado.", 404);

        var servico = await db.Produtos
            .FirstOrDefaultAsync(p => p.Id == req.ServicoId && p.DuracaoMinutos != null, ct)
            ?? throw new AppException("Serviço não encontrado ou sem duração configurada.", 404);

        var dataHoraFim = req.DataHoraInicio.AddMinutes(servico.DuracaoMinutos!.Value);

        var diaSemana = (int)req.DataHoraInicio.DayOfWeek;
        var horaInicio = req.DataHoraInicio.TimeOfDay;
        var horaFim = dataHoraFim.TimeOfDay;

        var dentroDoHorario = await db.DisponibilidadeSemanais
            .AnyAsync(d => d.ProfissionalId == req.ProfissionalId
                && d.DiaSemana == diaSemana
                && d.HoraInicio <= horaInicio
                && d.HoraFim >= horaFim, ct);

        if (!dentroDoHorario)
            throw new AppException("Horário fora da disponibilidade do profissional.", 400);

        var bloqueado = await db.BloqueiosAgenda
            .AnyAsync(b => b.DataInicio < dataHoraFim && b.DataFim > req.DataHoraInicio
                && (b.ProfissionalId == null || b.ProfissionalId == req.ProfissionalId), ct);

        if (bloqueado)
            throw new AppException("Horário bloqueado para o profissional.", 400);

        var conflito = await db.Agendamentos
            .AnyAsync(a => a.ProfissionalId == req.ProfissionalId
                && a.Status != AgendamentoStatus.Cancelado
                && a.DataHoraInicio < dataHoraFim && a.DataHoraFim > req.DataHoraInicio, ct);

        if (conflito)
            throw new AppException("Conflito de horário com outro agendamento.", 400);

        var agendamento = new Agendamento
        {
            EmpresaId = tenantContext.EmpresaId,
            ProfissionalId = req.ProfissionalId,
            ClienteNome = req.ClienteNome,
            ClienteTelefone = req.ClienteTelefone,
            ClienteId = req.ClienteId,
            ServicoId = req.ServicoId,
            DataHoraInicio = req.DataHoraInicio,
            DataHoraFim = dataHoraFim,
            Status = AgendamentoStatus.Agendado,
            Observacao = req.Observacao,
        };

        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync(ct);
        return await GetAsync(agendamento.Id, ct);
    }

    public async Task<AgendamentoResponse> AtualizarAsync(Guid id, AtualizarAgendamentoRequest req, CancellationToken ct)
    {
        var a = await db.Agendamentos
            .Include(a => a.Profissional)
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Agendamento não encontrado.", 404);

        if (a.Status == AgendamentoStatus.Concluido || a.Status == AgendamentoStatus.Cancelado)
            throw new AppException("Agendamentos concluídos ou cancelados não podem ser editados.", 400);

        a.Observacao = req.Observacao;
        await db.SaveChangesAsync(ct);
        return ToResponse(a);
    }

    public async Task<AgendamentoResponse> ConfirmarAsync(Guid id, CancellationToken ct)
    {
        var a = await FindAsync(id, ct);
        if (a.Status != AgendamentoStatus.Agendado)
            throw new AppException("Apenas agendamentos no status Agendado podem ser confirmados.", 400);
        a.Status = AgendamentoStatus.Confirmado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<ConcluirResponse> ConcluirAsync(Guid id, CancellationToken ct)
    {
        var a = await db.Agendamentos
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Agendamento não encontrado.", 404);

        if (a.Status != AgendamentoStatus.Agendado && a.Status != AgendamentoStatus.Confirmado)
            throw new AppException("Apenas agendamentos ativos podem ser concluídos.", 400);

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        var preco = a.Servico!.PrecoVenda;

        var venda = new Venda
        {
            EmpresaId = tenantContext.EmpresaId,
            ClienteId = a.ClienteId,
            Status = StatusVenda.Aberta,
            Subtotal = preco,
            Desconto = 0,
            Total = preco,
            FormaPagamento = FormaPagamento.Outro,
            Observacao = $"Gerado do agendamento de {a.ClienteNome}",
        };
        db.Vendas.Add(venda);

        db.ItensVenda.Add(new ItemVenda
        {
            VendaId = venda.Id,
            ProdutoId = a.ServicoId,
            Quantidade = 1,
            PrecoUnitario = preco,
            Desconto = 0,
            Total = preco,
        });

        a.Status = AgendamentoStatus.Concluido;
        a.VendaId = venda.Id;

        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return new ConcluirResponse(venda.Id);
    }

    public async Task<AgendamentoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var a = await FindAsync(id, ct);
        if (a.Status == AgendamentoStatus.Concluido || a.Status == AgendamentoStatus.Cancelado)
            throw new AppException("Agendamento já está concluído ou cancelado.", 400);
        a.Status = AgendamentoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<List<DateTime>> SlotsAsync(
        Guid profissionalId, DateOnly data, Guid servicoId, CancellationToken ct)
    {
        var diaSemana = (int)data.DayOfWeek;

        var faixas = await db.DisponibilidadeSemanais
            .Where(d => d.ProfissionalId == profissionalId && d.DiaSemana == diaSemana)
            .ToListAsync(ct);

        if (faixas.Count == 0) return [];

        var servico = await db.Produtos
            .FirstOrDefaultAsync(p => p.Id == servicoId && p.DuracaoMinutos != null, ct)
            ?? throw new AppException("Serviço não encontrado.", 404);

        var duracao = TimeSpan.FromMinutes(servico.DuracaoMinutos!.Value);
        var inicioDia = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimDia = inicioDia.AddDays(1);

        var bloqueios = await db.BloqueiosAgenda
            .Where(b => b.DataInicio < fimDia && b.DataFim > inicioDia
                && (b.ProfissionalId == null || b.ProfissionalId == profissionalId))
            .ToListAsync(ct);

        var ocupados = await db.Agendamentos
            .Where(a => a.ProfissionalId == profissionalId
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

    private async Task<Agendamento> FindAsync(Guid id, CancellationToken ct) =>
        await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Agendamento não encontrado.", 404);

    private static AgendamentoResponse ToResponse(Agendamento a) => new(
        a.Id,
        a.Profissional?.Nome ?? "",
        a.ClienteNome,
        a.ClienteTelefone,
        a.ClienteId,
        a.Servico?.Nome ?? "",
        a.Servico?.DuracaoMinutos ?? 0,
        a.DataHoraInicio,
        a.DataHoraFim,
        a.Status,
        a.Observacao,
        a.VendaId,
        a.CriadoEm);
}
```

- [ ] **Step 3: Build to verify (will still show CS1061 until AppDbContext updated)**

```bash
cd backend && dotnet build src/GestorAI.API/GestorAI.API.csproj 2>&1 | grep -E "^Build|error CS"
```

Expected: Errors about `db.Profissionais`, `db.DisponibilidadeSemanais`, `db.BloqueiosAgenda`, `db.Agendamentos` not found — this is expected until Task 7.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Services/Agendamentos/
git commit -m "feat: add AgendamentoService with slot calculation and status transitions"
```

---

## Task 6: Endpoints

**Files:**
- Create: `backend/src/GestorAI.API/Endpoints/ProfissionaisEndpoints.cs`
- Create: `backend/src/GestorAI.API/Endpoints/AgendamentosEndpoints.cs`

- [ ] **Step 1: Create ProfissionaisEndpoints.cs**

```csharp
// backend/src/GestorAI.API/Endpoints/ProfissionaisEndpoints.cs
using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.Services.Agendamentos;

namespace GestorAI.API.Endpoints;

public static class ProfissionaisEndpoints
{
    public static void MapProfissionais(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profissionais").RequireAuthorization();

        group.MapGet("/", async (ProfissionalService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPost("/", async (
            CriarProfissionalRequest req, ProfissionalService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/profissionais/{result.Id}", result);
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}", async (
            Guid id, AtualizarProfissionalRequest req, ProfissionalService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)))
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:guid}", async (
            Guid id, ProfissionalService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/{id:guid}/disponibilidade", async (
            Guid id, ProfissionalService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetDisponibilidadeAsync(id, ct)));

        group.MapPut("/{id:guid}/disponibilidade", async (
            Guid id, SalvarDisponibilidadeRequest req, ProfissionalService svc, CancellationToken ct) =>
        {
            await svc.SalvarDisponibilidadeAsync(id, req, ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        var bloqueiosGroup = app.MapGroup("/api/agenda/bloqueios").RequireAuthorization();

        bloqueiosGroup.MapGet("/", async (
            DateTime de, DateTime ate, ProfissionalService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListBloqueiosAsync(de, ate, ct)));

        bloqueiosGroup.MapPost("/", async (
            CriarBloqueioRequest req, ProfissionalService svc, CancellationToken ct) =>
        {
            var result = await svc.CriarBloqueioAsync(req, ct);
            return Results.Created($"/api/agenda/bloqueios/{result.Id}", result);
        });

        bloqueiosGroup.MapDelete("/{id:guid}", async (
            Guid id, ProfissionalService svc, CancellationToken ct) =>
        {
            await svc.DeleteBloqueioAsync(id, ct);
            return Results.NoContent();
        });
    }
}
```

- [ ] **Step 2: Create AgendamentosEndpoints.cs**

```csharp
// backend/src/GestorAI.API/Endpoints/AgendamentosEndpoints.cs
using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.Services.Agendamentos;

namespace GestorAI.API.Endpoints;

public static class AgendamentosEndpoints
{
    public static void MapAgendamentos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agendamentos").RequireAuthorization();

        group.MapGet("/slots", async (
            Guid profissionalId, DateOnly data, Guid servicoId,
            AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.SlotsAsync(profissionalId, data, servicoId, ct)));

        group.MapGet("/", async (
            DateOnly data, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(data, ct)));

        group.MapGet("/{id:guid}", async (
            Guid id, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (
            CriarAgendamentoRequest req, AgendamentoService svc, CancellationToken ct) =>
        {
            var result = await svc.CriarAsync(req, ct);
            return Results.Created($"/api/agendamentos/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (
            Guid id, AtualizarAgendamentoRequest req, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.AtualizarAsync(id, req, ct)));

        group.MapPost("/{id:guid}/confirmar", async (
            Guid id, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConfirmarAsync(id, ct)));

        group.MapPost("/{id:guid}/concluir", async (
            Guid id, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConcluirAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (
            Guid id, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
cd backend && dotnet build src/GestorAI.API/GestorAI.API.csproj 2>&1 | grep -E "^Build|error CS"
```

Expected: Still the same CS1061 errors on AppDbContext DbSet properties (until Task 7).

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Endpoints/ProfissionaisEndpoints.cs \
        backend/src/GestorAI.API/Endpoints/AgendamentosEndpoints.cs
git commit -m "feat: add Profissionais and Agendamentos endpoint registration"
```

---

## Task 7: AppDbContext, Program.cs, and EF Migration

**Files:**
- Modify: `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

- [ ] **Step 1: Update AppDbContext.cs**

Replace the full file with:

```csharp
// backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, TenantContext tenantContext)
    : DbContext(options)
{
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Lancamento> Lancamentos => Set<Lancamento>();
    public DbSet<Orcamento> Orcamentos => Set<Orcamento>();
    public DbSet<OrcamentoItem> OrcamentoItens => Set<OrcamentoItem>();
    public DbSet<Profissional> Profissionais => Set<Profissional>();
    public DbSet<DisponibilidadeSemanal> DisponibilidadeSemanais => Set<DisponibilidadeSemanal>();
    public DbSet<BloqueioAgenda> BloqueiosAgenda => Set<BloqueioAgenda>();
    public DbSet<Agendamento> Agendamentos => Set<Agendamento>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Cliente>()
            .HasIndex(c => new { c.EmpresaId, c.Whatsapp })
            .IsUnique();

        modelBuilder.Entity<Venda>()
            .HasOne(v => v.Lancamento)
            .WithOne(l => l.Venda)
            .HasForeignKey<Lancamento>(l => l.VendaId);

        modelBuilder.Entity<Produto>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Categoria>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<MovimentacaoEstoque>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Venda>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Cliente>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Lancamento>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Orcamento>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Profissional>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<BloqueioAgenda>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Agendamento>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
    }
}
```

Note: `DisponibilidadeSemanal` intentionally has no `HasQueryFilter` — it has no `EmpresaId` and is always accessed via `ProfissionalId` which is already tenant-scoped.

- [ ] **Step 2: Update Program.cs — add service registrations and endpoint mappings**

Add after `builder.Services.AddScoped<OrcamentoService>();`:

```csharp
// Services — Agendamentos
builder.Services.AddScoped<ProfissionalService>();
builder.Services.AddScoped<AgendamentoService>();
builder.Services.AddScoped<IValidator<CriarAgendamentoRequest>, CriarAgendamentoValidator>();
```

Add the using imports at the top:

```csharp
using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.Services.Agendamentos;
```

Add after `app.MapOrcamentos();`:

```csharp
app.MapProfissionais();
app.MapAgendamentos();
```

- [ ] **Step 3: Build — should now succeed with 0 errors**

```bash
cd backend && dotnet build src/GestorAI.API/GestorAI.API.csproj
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Generate EF migration**

```bash
cd backend/src/GestorAI.API && PATH="/usr/local/share/dotnet:$PATH" ~/.dotnet/tools/dotnet-ef migrations add AddAgendamentos
```

Expected: Migration file created at `Migrations/YYYYMMDDHHMMSS_AddAgendamentos.cs` containing table creation for `Profissionais`, `DisponibilidadeSemanais`, `BloqueiosAgenda`, `Agendamentos`, and the new `DuracaoMinutos` column on `Produtos`.

- [ ] **Step 5: Apply migration (if running PostgreSQL locally)**

```bash
cd backend/src/GestorAI.API && PATH="/usr/local/share/dotnet:$PATH" ~/.dotnet/tools/dotnet-ef database update
```

If not running PostgreSQL locally, skip this step — migration runs on next `docker-compose up`.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs \
        backend/src/GestorAI.API/Program.cs \
        backend/src/GestorAI.API/Migrations/
git commit -m "feat: register Agendamentos infrastructure, add EF migration"
```

---

## Task 8: Frontend types

**Files:**
- Create: `frontend/src/types/agendamento.ts`

- [ ] **Step 1: Create types/agendamento.ts**

```typescript
// frontend/src/types/agendamento.ts

export type AgendamentoStatus = 'Agendado' | 'Confirmado' | 'Concluido' | 'Cancelado'

export interface AgendamentoListItem {
  id: string
  profissionalNome: string
  clienteNome: string
  serviçoNome: string
  dataHoraInicio: string
  dataHoraFim: string
  status: AgendamentoStatus
}

export interface AgendamentoResponse {
  id: string
  profissionalNome: string
  clienteNome: string
  clienteTelefone: string
  clienteId: string | null
  servicoNome: string
  duracaoMinutos: number
  dataHoraInicio: string
  dataHoraFim: string
  status: AgendamentoStatus
  observacao: string | null
  vendaId: string | null
  criadoEm: string
}

export interface CriarAgendamentoRequest {
  profissionalId: string
  clienteNome: string
  clienteTelefone: string
  clienteId?: string
  servicoId: string
  dataHoraInicio: string
  observacao?: string
}

export interface ConcluirResponse {
  vendaId: string
}

export interface ProfissionalResponse {
  id: string
  nome: string
  telefone: string | null
  ativo: boolean
}

export interface DisponibilidadeItem {
  diaSemana: number
  horaInicio: string
  horaFim: string
}

export interface BloqueioResponse {
  id: string
  profissionalId: string | null
  profissionalNome: string | null
  dataInicio: string
  dataFim: string
  motivo: string | null
}
```

Note: `serviçoNome` uses the ç character intentionally to match the JSON key `serviçoNome` from the backend. Wait — actually the backend uses `ServicoNome` in the DTO which serializes as `servicoNome` (camelCase). Let me fix that:

Actually, the backend's `AgendamentoListItem` record has `ServicoNome` which becomes `servicoNome` in JSON. The type should use `servicoNome` not `serviçoNome`.

Replace the `serviçoNome` field in `AgendamentoListItem` with `servicoNome`.

```typescript
export interface AgendamentoListItem {
  id: string
  profissionalId: string
  profissionalNome: string
  clienteNome: string
  servicoNome: string
  dataHoraInicio: string
  dataHoraFim: string
  status: AgendamentoStatus
}
```

- [ ] **Step 2: Run TypeScript check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/types/agendamento.ts
git commit -m "feat: add Agendamentos frontend types"
```

---

## Task 9: useAgendamentos hook

**Files:**
- Create: `frontend/src/hooks/useAgendamentos.ts`

- [ ] **Step 1: Create useAgendamentos.ts**

```typescript
// frontend/src/hooks/useAgendamentos.ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  AgendamentoListItem,
  AgendamentoResponse,
  CriarAgendamentoRequest,
  ConcluirResponse,
} from '@/types/agendamento'

export function useAgendamentos() {
  const [agendamentos, setAgendamentos] = useState<AgendamentoListItem[]>([])
  const [agendamento, setAgendamento] = useState<AgendamentoResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (data: string) => {
    setLoading(true)
    try {
      const items = await api.get<AgendamentoListItem[]>(`/api/agendamentos?data=${data}`)
      setAgendamentos(items)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar agendamentos')
    } finally {
      setLoading(false)
    }
  }, [])

  const get = useCallback(async (id: string) => {
    setLoading(true)
    try {
      const data = await api.get<AgendamentoResponse>(`/api/agendamentos/${id}`)
      setAgendamento(data)
      return data
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar agendamento')
      return null
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CriarAgendamentoRequest) => {
    return api.post<AgendamentoResponse>('/api/agendamentos', req)
  }, [])

  const confirmar = useCallback(async (id: string) => {
    const result = await api.post<AgendamentoResponse>(`/api/agendamentos/${id}/confirmar`, {})
    setAgendamento(result)
  }, [])

  const concluir = useCallback(async (id: string) => {
    return api.post<ConcluirResponse>(`/api/agendamentos/${id}/concluir`, {})
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<AgendamentoResponse>(`/api/agendamentos/${id}/cancelar`, {})
    setAgendamento(result)
  }, [])

  const slots = useCallback(async (profissionalId: string, data: string, servicoId: string) => {
    return api.get<string[]>(
      `/api/agendamentos/slots?profissionalId=${profissionalId}&data=${data}&servicoId=${servicoId}`
    )
  }, [])

  return {
    agendamentos,
    agendamento,
    loading,
    error,
    list,
    get,
    create,
    confirmar,
    concluir,
    cancelar,
    slots,
  }
}
```

- [ ] **Step 2: TypeScript check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/hooks/useAgendamentos.ts
git commit -m "feat: add useAgendamentos hook"
```

---

## Task 10: useProfissionais hook

**Files:**
- Create: `frontend/src/hooks/useProfissionais.ts`

- [ ] **Step 1: Create useProfissionais.ts**

```typescript
// frontend/src/hooks/useProfissionais.ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { ProfissionalResponse, DisponibilidadeItem, BloqueioResponse } from '@/types/agendamento'

interface CriarProfissionalRequest { nome: string; telefone?: string }
interface AtualizarProfissionalRequest { nome: string; telefone?: string; ativo: boolean }
interface SalvarDisponibilidadeRequest { faixas: DisponibilidadeItem[] }
interface CriarBloqueioRequest {
  profissionalId?: string
  dataInicio: string
  dataFim: string
  motivo?: string
}

export function useProfissionais() {
  const [profissionais, setProfissionais] = useState<ProfissionalResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async () => {
    setLoading(true)
    try {
      const data = await api.get<ProfissionalResponse[]>('/api/profissionais')
      setProfissionais(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar profissionais')
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CriarProfissionalRequest) => {
    const result = await api.post<ProfissionalResponse>('/api/profissionais', req)
    setProfissionais(prev => [...prev, result])
    return result
  }, [])

  const update = useCallback(async (id: string, req: AtualizarProfissionalRequest) => {
    const result = await api.put<ProfissionalResponse>(`/api/profissionais/${id}`, req)
    setProfissionais(prev => prev.map(p => p.id === id ? result : p))
    return result
  }, [])

  const remove = useCallback(async (id: string) => {
    await api.delete(`/api/profissionais/${id}`)
    setProfissionais(prev => prev.filter(p => p.id !== id))
  }, [])

  const getDisponibilidade = useCallback(async (id: string) => {
    return api.get<DisponibilidadeItem[]>(`/api/profissionais/${id}/disponibilidade`)
  }, [])

  const saveDisponibilidade = useCallback(async (id: string, faixas: DisponibilidadeItem[]) => {
    const req: SalvarDisponibilidadeRequest = { faixas }
    await api.put(`/api/profissionais/${id}/disponibilidade`, req)
  }, [])

  const listBloqueios = useCallback(async (de: string, ate: string) => {
    return api.get<BloqueioResponse[]>(`/api/agenda/bloqueios?de=${de}&ate=${ate}`)
  }, [])

  const criarBloqueio = useCallback(async (req: CriarBloqueioRequest) => {
    return api.post<BloqueioResponse>('/api/agenda/bloqueios', req)
  }, [])

  const deleteBloqueio = useCallback(async (id: string) => {
    await api.delete(`/api/agenda/bloqueios/${id}`)
  }, [])

  return {
    profissionais,
    loading,
    error,
    list,
    create,
    update,
    remove,
    getDisponibilidade,
    saveDisponibilidade,
    listBloqueios,
    criarBloqueio,
    deleteBloqueio,
  }
}
```

- [ ] **Step 2: TypeScript check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/hooks/useProfissionais.ts
git commit -m "feat: add useProfissionais hook"
```

---

## Task 11: Profissionais page

**Files:**
- Create: `frontend/src/pages/profissionais/Profissionais.tsx`

- [ ] **Step 1: Create Profissionais.tsx**

```tsx
// frontend/src/pages/profissionais/Profissionais.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useProfissionais } from '@/hooks/useProfissionais'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { ProfissionalResponse } from '@/types/agendamento'

export default function Profissionais() {
  const { profissionais, loading, error, list, create, update, remove } = useProfissionais()
  const navigate = useNavigate()
  const [showForm, setShowForm] = useState(false)
  const [editando, setEditando] = useState<ProfissionalResponse | null>(null)
  const [nome, setNome] = useState('')
  const [telefone, setTelefone] = useState('')
  const [ativo, setAtivo] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => { void list() }, [list])

  function abrirNovo() {
    setEditando(null)
    setNome('')
    setTelefone('')
    setAtivo(true)
    setShowForm(true)
  }

  function abrirEditar(p: ProfissionalResponse) {
    setEditando(p)
    setNome(p.nome)
    setTelefone(p.telefone ?? '')
    setAtivo(p.ativo)
    setShowForm(true)
  }

  async function salvar() {
    if (!nome.trim()) return
    setSaving(true)
    try {
      if (editando) {
        await update(editando.id, { nome: nome.trim(), telefone: telefone.trim() || undefined, ativo })
      } else {
        await create({ nome: nome.trim(), telefone: telefone.trim() || undefined })
      }
      setShowForm(false)
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  async function excluir(id: string) {
    if (!confirm('Excluir profissional?')) return
    try { await remove(id) } catch (e) { alert(e instanceof Error ? e.message : 'Erro ao excluir') }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>
  if (error) return <p className="text-destructive">{error}</p>

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Profissionais</h1>
        <Button onClick={abrirNovo}>Novo Profissional</Button>
      </div>

      {showForm && (
        <div className="rounded-md border p-4 space-y-4 max-w-md">
          <h2 className="font-semibold">{editando ? 'Editar' : 'Novo'} Profissional</h2>
          <div className="space-y-2">
            <Label>Nome *</Label>
            <Input value={nome} onChange={e => setNome(e.target.value)} placeholder="Nome completo" />
          </div>
          <div className="space-y-2">
            <Label>Telefone / WhatsApp</Label>
            <Input value={telefone} onChange={e => setTelefone(e.target.value)} placeholder="(11) 99999-9999" />
          </div>
          {editando && (
            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="ativo"
                checked={ativo}
                onChange={e => setAtivo(e.target.checked)}
                className="h-4 w-4"
              />
              <Label htmlFor="ativo">Ativo</Label>
            </div>
          )}
          <div className="flex gap-2">
            <Button onClick={salvar} disabled={saving || !nome.trim()}>
              {saving ? '...' : 'Salvar'}
            </Button>
            <Button variant="ghost" onClick={() => setShowForm(false)}>Cancelar</Button>
          </div>
        </div>
      )}

      <div className="rounded-md border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="px-4 py-3 text-left font-medium">Nome</th>
              <th className="px-4 py-3 text-left font-medium">Telefone</th>
              <th className="px-4 py-3 text-left font-medium">Status</th>
              <th className="px-4 py-3 text-left font-medium">Ações</th>
            </tr>
          </thead>
          <tbody>
            {profissionais.map(p => (
              <tr key={p.id} className="border-b">
                <td className="px-4 py-3 font-medium">{p.nome}</td>
                <td className="px-4 py-3 text-muted-foreground">{p.telefone ?? '—'}</td>
                <td className="px-4 py-3">
                  <span className={p.ativo
                    ? 'rounded-full bg-green-100 px-2 py-0.5 text-xs text-green-700'
                    : 'rounded-full bg-gray-100 px-2 py-0.5 text-xs text-gray-500'}>
                    {p.ativo ? 'Ativo' : 'Inativo'}
                  </span>
                </td>
                <td className="px-4 py-3">
                  <div className="flex gap-2">
                    <Button variant="ghost" size="sm" onClick={() => abrirEditar(p)}>Editar</Button>
                    <Button variant="ghost" size="sm"
                      onClick={() => navigate(`/profissionais/${p.id}/disponibilidade`)}>
                      Disponibilidade
                    </Button>
                    <Button variant="ghost" size="sm" onClick={() => excluir(p.id)}>Excluir</Button>
                  </div>
                </td>
              </tr>
            ))}
            {profissionais.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-muted-foreground">
                  Nenhum profissional cadastrado.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Create DisponibilidadeProfissional.tsx**

```tsx
// frontend/src/pages/profissionais/DisponibilidadeProfissional.tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useProfissionais } from '@/hooks/useProfissionais'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import type { DisponibilidadeItem } from '@/types/agendamento'

const DIAS = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb']

export default function DisponibilidadeProfissional() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { getDisponibilidade, saveDisponibilidade } = useProfissionais()
  const [faixas, setFaixas] = useState<DisponibilidadeItem[]>([])
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id) return
    void getDisponibilidade(id).then(data => {
      setFaixas(data)
      setLoading(false)
    }).catch(() => setLoading(false))
  }, [getDisponibilidade, id])

  function adicionarFaixa(dia: number) {
    setFaixas(prev => [...prev, { diaSemana: dia, horaInicio: '08:00', horaFim: '18:00' }])
  }

  function removerFaixa(index: number) {
    setFaixas(prev => prev.filter((_, i) => i !== index))
  }

  function atualizarFaixa(index: number, field: keyof DisponibilidadeItem, value: string | number) {
    setFaixas(prev => prev.map((f, i) => i === index ? { ...f, [field]: value } : f))
  }

  async function salvar() {
    if (!id) return
    setSaving(true)
    try {
      await saveDisponibilidade(id, faixas)
      alert('Disponibilidade salva!')
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>

  return (
    <div className="space-y-6 max-w-2xl">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Disponibilidade Semanal</h1>
        <Button variant="ghost" onClick={() => navigate('/profissionais')}>← Voltar</Button>
      </div>

      <div className="space-y-4">
        {DIAS.map((dia, diaIdx) => {
          const faixasDia = faixas.filter(f => f.diaSemana === diaIdx)
          return (
            <div key={diaIdx} className="rounded-md border p-4">
              <div className="flex items-center justify-between mb-2">
                <span className="font-medium">{dia}</span>
                <Button variant="outline" size="sm" onClick={() => adicionarFaixa(diaIdx)}>
                  + Faixa
                </Button>
              </div>
              {faixasDia.length === 0 && (
                <p className="text-sm text-muted-foreground">Sem horários</p>
              )}
              {faixasDia.map((faixa) => {
                const idx = faixas.indexOf(faixa)
                return (
                  <div key={idx} className="flex items-center gap-2 mt-2">
                    <Input
                      type="time"
                      value={faixa.horaInicio}
                      onChange={e => atualizarFaixa(idx, 'horaInicio', e.target.value)}
                      className="w-32"
                    />
                    <span className="text-muted-foreground">até</span>
                    <Input
                      type="time"
                      value={faixa.horaFim}
                      onChange={e => atualizarFaixa(idx, 'horaFim', e.target.value)}
                      className="w-32"
                    />
                    <Button variant="ghost" size="sm" onClick={() => removerFaixa(idx)}>✕</Button>
                  </div>
                )
              })}
            </div>
          )
        })}
      </div>

      <Button onClick={salvar} disabled={saving}>
        {saving ? 'Salvando...' : 'Salvar Disponibilidade'}
      </Button>
    </div>
  )
}
```

- [ ] **Step 3: TypeScript check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/pages/profissionais/
git commit -m "feat: add Profissionais and DisponibilidadeProfissional pages"
```

---

## Task 12: Agendamentos page (daily grid)

**Files:**
- Create: `frontend/src/pages/agendamentos/Agendamentos.tsx`

- [ ] **Step 1: Create Agendamentos.tsx**

```tsx
// frontend/src/pages/agendamentos/Agendamentos.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useAgendamentos } from '@/hooks/useAgendamentos'
import { useProfissionais } from '@/hooks/useProfissionais'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import type { AgendamentoStatus } from '@/types/agendamento'

const statusClassName = (s: AgendamentoStatus): string => {
  if (s === 'Agendado')   return 'bg-blue-100 text-blue-700 border-blue-200'
  if (s === 'Confirmado') return 'bg-green-100 text-green-700 border-green-200'
  if (s === 'Concluido')  return 'bg-purple-100 text-purple-700 border-purple-200'
  if (s === 'Cancelado')  return 'bg-red-100 text-red-700 border-red-200'
  return ''
}

const fmtHora = (iso: string) =>
  new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })

const toDateStr = (d: Date) => d.toISOString().slice(0, 10)

export default function Agendamentos() {
  const navigate = useNavigate()
  const [data, setData] = useState(toDateStr(new Date()))
  const { agendamentos, loading, error, list } = useAgendamentos()
  const { profissionais, list: listProfs } = useProfissionais()

  useEffect(() => { void listProfs() }, [listProfs])
  useEffect(() => { void list(data) }, [list, data])

  function mudarDia(delta: number) {
    const d = new Date(data + 'T12:00:00Z')
    d.setUTCDate(d.getUTCDate() + delta)
    setData(toDateStr(d))
  }

  const ativos = profissionais.filter(p => p.ativo)

  if (error) return <p className="text-destructive">{error}</p>

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Agenda</h1>
        <Button onClick={() => navigate('/agendamentos/novo')}>Novo Agendamento</Button>
      </div>

      <div className="flex items-center gap-3">
        <Button variant="outline" size="sm" onClick={() => mudarDia(-1)}>
          <ChevronLeft size={16} />
        </Button>
        <input
          type="date"
          value={data}
          onChange={e => setData(e.target.value)}
          className="rounded-md border border-input bg-transparent px-3 py-1 text-sm"
        />
        <Button variant="outline" size="sm" onClick={() => mudarDia(1)}>
          <ChevronRight size={16} />
        </Button>
        <Button variant="ghost" size="sm" onClick={() => setData(toDateStr(new Date()))}>
          Hoje
        </Button>
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : (
        <div className="grid gap-4" style={{ gridTemplateColumns: `repeat(${Math.max(ativos.length, 1)}, minmax(200px, 1fr))` }}>
          {ativos.length === 0 ? (
            <div className="rounded-md border p-8 text-center text-muted-foreground">
              Nenhum profissional ativo.
            </div>
          ) : ativos.map(prof => {
            const cards = agendamentos.filter(a => a.profissionalId === prof.id)
            return (
              <div key={prof.id} className="rounded-md border">
                <div className="border-b bg-muted/50 px-3 py-2 font-semibold text-sm">
                  {prof.nome}
                </div>
                <div className="divide-y">
                  {cards.length === 0 ? (
                    <p className="px-3 py-6 text-center text-sm text-muted-foreground">
                      Sem agendamentos
                    </p>
                  ) : cards.map(a => (
                    <button
                      key={a.id}
                      className="w-full px-3 py-3 text-left hover:bg-accent transition-colors"
                      onClick={() => navigate(`/agendamentos/${a.id}`)}
                    >
                      <div className="flex items-start justify-between gap-2">
                        <div>
                          <p className="text-sm font-medium">{a.clienteNome}</p>
                          <p className="text-xs text-muted-foreground">
                            {fmtHora(a.dataHoraInicio)} – {fmtHora(a.dataHoraFim)}
                          </p>
                          <p className="text-xs text-muted-foreground">{a.servicoNome}</p>
                        </div>
                        <Badge className={statusClassName(a.status)}>{a.status}</Badge>
                      </div>
                    </button>
                  ))}
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 2: TypeScript check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/agendamentos/Agendamentos.tsx
git commit -m "feat: add Agendamentos daily grid page"
```

---

## Task 13: NovoAgendamento page

**Files:**
- Create: `frontend/src/pages/agendamentos/NovoAgendamento.tsx`

- [ ] **Step 1: Create NovoAgendamento.tsx**

```tsx
// frontend/src/pages/agendamentos/NovoAgendamento.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAgendamentos } from '@/hooks/useAgendamentos'
import { useProfissionais } from '@/hooks/useProfissionais'
import { useEstoque } from '@/hooks/useEstoque'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

const fmtHora = (iso: string) =>
  new Date(iso).toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })

const toDateStr = (d: Date) => d.toISOString().slice(0, 10)

export default function NovoAgendamento() {
  const navigate = useNavigate()
  const { create, slots } = useAgendamentos()
  const { profissionais, list: listProfs } = useProfissionais()
  const { produtos, listProdutos } = useEstoque()
  const { clientes, list: listClientes } = useClientes()

  const [profissionalId, setProfissionalId] = useState('')
  const [servicoId, setServicoId] = useState('')
  const [data, setData] = useState(toDateStr(new Date()))
  const [slot, setSlot] = useState('')
  const [telefone, setTelefone] = useState('')
  const [clienteNome, setClienteNome] = useState('')
  const [clienteId, setClienteId] = useState<string | undefined>()
  const [observacao, setObservacao] = useState('')
  const [slotOpcoes, setSlotOpcoes] = useState<string[]>([])
  const [loadingSlots, setLoadingSlots] = useState(false)
  const [saving, setSaving] = useState(false)
  const [erros, setErros] = useState<Record<string, string>>({})

  useEffect(() => { void listProfs() }, [listProfs])
  useEffect(() => { void listProdutos() }, [listProdutos])
  useEffect(() => { void listClientes() }, [listClientes])

  useEffect(() => {
    if (!telefone || telefone.replace(/\D/g, '').length < 10) return
    const numeros = telefone.replace(/\D/g, '')
    const encontrado = clientes.find(c => c.whatsapp?.replace(/\D/g, '') === numeros)
    if (encontrado) {
      setClienteNome(encontrado.nome)
      setClienteId(encontrado.id)
    } else {
      setClienteId(undefined)
    }
  }, [telefone, clientes])

  useEffect(() => {
    if (!profissionalId || !servicoId || !data) {
      setSlotOpcoes([])
      setSlot('')
      return
    }
    setLoadingSlots(true)
    slots(profissionalId, data, servicoId)
      .then(s => { setSlotOpcoes(s); setSlot('') })
      .catch(() => setSlotOpcoes([]))
      .finally(() => setLoadingSlots(false))
  }, [profissionalId, servicoId, data, slots])

  const servicos = produtos.filter(p => (p.duracaoMinutos ?? 0) > 0 && p.ativo)
  const profissionaisAtivos = profissionais.filter(p => p.ativo)

  async function salvar() {
    const novosErros: Record<string, string> = {}
    if (!profissionalId) novosErros.profissional = 'Profissional obrigatório'
    if (!servicoId) novosErros.servico = 'Serviço obrigatório'
    if (!slot) novosErros.slot = 'Horário obrigatório'
    if (!telefone.trim()) novosErros.telefone = 'Telefone obrigatório'
    if (!clienteNome.trim()) novosErros.clienteNome = 'Nome obrigatório'

    if (Object.keys(novosErros).length > 0) {
      setErros(novosErros)
      return
    }
    setErros({})

    setSaving(true)
    try {
      await create({
        profissionalId,
        servicoId,
        dataHoraInicio: slot,
        clienteNome: clienteNome.trim(),
        clienteTelefone: telefone.trim(),
        clienteId,
        observacao: observacao.trim() || undefined,
      })
      navigate('/agendamentos')
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao criar agendamento')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="max-w-lg space-y-5">
      <h1 className="text-2xl font-bold">Novo Agendamento</h1>

      <div className="space-y-2">
        <Label>Profissional *</Label>
        <select
          value={profissionalId}
          onChange={e => setProfissionalId(e.target.value)}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm"
        >
          <option value="">Selecionar profissional...</option>
          {profissionaisAtivos.map(p => (
            <option key={p.id} value={p.id}>{p.nome}</option>
          ))}
        </select>
        {erros.profissional && <p className="text-xs text-destructive">{erros.profissional}</p>}
      </div>

      <div className="space-y-2">
        <Label>Serviço *</Label>
        <select
          value={servicoId}
          onChange={e => setServicoId(e.target.value)}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm"
        >
          <option value="">Selecionar serviço...</option>
          {servicos.map(s => (
            <option key={s.id} value={s.id}>{s.nome} ({s.duracaoMinutos} min)</option>
          ))}
        </select>
        {erros.servico && <p className="text-xs text-destructive">{erros.servico}</p>}
      </div>

      <div className="space-y-2">
        <Label>Data *</Label>
        <Input type="date" value={data} onChange={e => setData(e.target.value)} />
      </div>

      <div className="space-y-2">
        <Label>Horário *</Label>
        {loadingSlots ? (
          <p className="text-sm text-muted-foreground">Buscando horários...</p>
        ) : (
          <select
            value={slot}
            onChange={e => setSlot(e.target.value)}
            disabled={slotOpcoes.length === 0}
            className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm disabled:opacity-50"
          >
            <option value="">
              {slotOpcoes.length === 0
                ? 'Selecione profissional, serviço e data primeiro'
                : 'Selecionar horário...'}
            </option>
            {slotOpcoes.map(s => (
              <option key={s} value={s}>{fmtHora(s)}</option>
            ))}
          </select>
        )}
        {erros.slot && <p className="text-xs text-destructive">{erros.slot}</p>}
      </div>

      <div className="space-y-2">
        <Label>Telefone / WhatsApp *</Label>
        <Input
          value={telefone}
          onChange={e => setTelefone(e.target.value)}
          placeholder="(11) 99999-9999"
        />
        {erros.telefone && <p className="text-xs text-destructive">{erros.telefone}</p>}
      </div>

      <div className="space-y-2">
        <Label>Nome do Cliente *</Label>
        <Input
          value={clienteNome}
          onChange={e => setClienteNome(e.target.value)}
          placeholder="Nome completo"
        />
        {clienteId && (
          <p className="text-xs text-muted-foreground">Cliente existente encontrado.</p>
        )}
        {erros.clienteNome && <p className="text-xs text-destructive">{erros.clienteNome}</p>}
      </div>

      <div className="space-y-2">
        <Label>Observação</Label>
        <textarea
          value={observacao}
          onChange={e => setObservacao(e.target.value)}
          placeholder="Informações adicionais"
          rows={3}
          className="flex w-full rounded-md border border-input bg-transparent px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring resize-none"
        />
      </div>

      <div className="flex gap-2">
        <Button onClick={salvar} disabled={saving}>
          {saving ? '...' : 'Agendar'}
        </Button>
        <Button variant="ghost" onClick={() => navigate('/agendamentos')}>Cancelar</Button>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: TypeScript check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/agendamentos/NovoAgendamento.tsx
git commit -m "feat: add NovoAgendamento form with slot picker and client lookup"
```

---

## Task 14: DetalheAgendamento page

**Files:**
- Create: `frontend/src/pages/agendamentos/DetalheAgendamento.tsx`

- [ ] **Step 1: Create DetalheAgendamento.tsx**

```tsx
// frontend/src/pages/agendamentos/DetalheAgendamento.tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { MessageCircle } from 'lucide-react'
import { useAgendamentos } from '@/hooks/useAgendamentos'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import type { AgendamentoStatus } from '@/types/agendamento'

const statusClassName = (s: AgendamentoStatus): string => {
  if (s === 'Agendado')   return 'bg-blue-100 text-blue-700 border-blue-200'
  if (s === 'Confirmado') return 'bg-green-100 text-green-700 border-green-200'
  if (s === 'Concluido')  return 'bg-purple-100 text-purple-700 border-purple-200'
  if (s === 'Cancelado')  return 'bg-red-100 text-red-700 border-red-200'
  return ''
}

const fmtDt = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })

export default function DetalheAgendamento() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { agendamento, loading, error, get, confirmar, concluir, cancelar } = useAgendamentos()
  const [acao, setAcao] = useState<string | null>(null)

  useEffect(() => {
    if (id) void get(id)
  }, [get, id])

  async function executar(fn: () => Promise<unknown>, label: string) {
    if (!confirm(`Confirmar: ${label}?`)) return
    setAcao(label)
    try { await fn() } catch (e) { alert(e instanceof Error ? e.message : 'Erro') }
    finally { setAcao(null) }
  }

  async function handleConcluir() {
    if (!id) return
    if (!confirm('Concluir agendamento? Uma Venda será criada automaticamente.')) return
    setAcao('concluir')
    try {
      const result = await concluir(id)
      navigate(`/vendas/nova?vendaId=${result.vendaId}`)
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao concluir')
    } finally {
      setAcao(null)
    }
  }

  function abrirWhatsapp() {
    if (!agendamento?.clienteTelefone) return
    const phone = agendamento.clienteTelefone.replace(/\D/g, '')
    const dt = fmtDt(agendamento.dataHoraInicio)
    const msg = encodeURIComponent(
      `Olá ${agendamento.clienteNome}! ` +
      `Seu agendamento de "${agendamento.servicoNome}" está confirmado para ${dt}.`
    )
    window.open(`https://wa.me/${phone}?text=${msg}`, '_blank')
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>
  if (error) return <p className="text-destructive">{error}</p>
  if (!agendamento) return null

  const a = agendamento

  return (
    <div className="max-w-lg space-y-6">
      <div className="flex items-start justify-between">
        <div>
          <h1 className="text-2xl font-bold">{a.clienteNome}</h1>
          <p className="text-muted-foreground">{a.servicoNome} • {a.duracaoMinutos} min</p>
        </div>
        <Badge className={statusClassName(a.status)}>{a.status}</Badge>
      </div>

      <div className="flex flex-wrap gap-2">
        {(a.status === 'Agendado' || a.status === 'Confirmado') && (
          <Button variant="outline" onClick={abrirWhatsapp}>
            <MessageCircle size={16} className="mr-2" /> WhatsApp
          </Button>
        )}
        {a.status === 'Agendado' && (
          <>
            <Button onClick={() => executar(() => confirmar(a.id), 'Confirmar agendamento')}
              disabled={acao !== null}>
              {acao === 'Confirmar agendamento' ? '...' : 'Confirmar'}
            </Button>
            <Button variant="destructive" onClick={() => executar(() => cancelar(a.id), 'Cancelar agendamento')}
              disabled={acao !== null}>
              Cancelar
            </Button>
          </>
        )}
        {a.status === 'Confirmado' && (
          <>
            <Button onClick={handleConcluir} disabled={acao !== null}>
              {acao === 'concluir' ? '...' : 'Concluir'}
            </Button>
            <Button variant="destructive" onClick={() => executar(() => cancelar(a.id), 'Cancelar agendamento')}
              disabled={acao !== null}>
              Cancelar
            </Button>
          </>
        )}
        {a.status === 'Concluido' && a.vendaId && (
          <Button variant="outline" onClick={() => navigate('/vendas')}>
            Ver histórico de vendas
          </Button>
        )}
      </div>

      <div className="rounded-md border divide-y text-sm">
        <div className="px-4 py-3 flex justify-between">
          <span className="text-muted-foreground">Profissional</span>
          <span className="font-medium">{a.profissionalNome}</span>
        </div>
        <div className="px-4 py-3 flex justify-between">
          <span className="text-muted-foreground">Início</span>
          <span className="font-medium">{fmtDt(a.dataHoraInicio)}</span>
        </div>
        <div className="px-4 py-3 flex justify-between">
          <span className="text-muted-foreground">Fim</span>
          <span className="font-medium">{fmtDt(a.dataHoraFim)}</span>
        </div>
        <div className="px-4 py-3 flex justify-between">
          <span className="text-muted-foreground">Telefone</span>
          <span className="font-medium">{a.clienteTelefone}</span>
        </div>
        {a.observacao && (
          <div className="px-4 py-3">
            <span className="text-muted-foreground">Obs: </span>{a.observacao}
          </div>
        )}
      </div>

      <Button variant="ghost" onClick={() => navigate('/agendamentos')}>← Voltar</Button>
    </div>
  )
}
```

- [ ] **Step 2: TypeScript check**

```bash
cd frontend && npx tsc --noEmit
```

Expected: No errors.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/agendamentos/DetalheAgendamento.tsx
git commit -m "feat: add DetalheAgendamento page with status actions"
```

---

## Task 15: Router and Sidebar

**Files:**
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Update router/index.tsx**

Add imports after the orcamentos imports:

```typescript
import Agendamentos from '@/pages/agendamentos/Agendamentos'
import NovoAgendamento from '@/pages/agendamentos/NovoAgendamento'
import DetalheAgendamento from '@/pages/agendamentos/DetalheAgendamento'
import Profissionais from '@/pages/profissionais/Profissionais'
import DisponibilidadeProfissional from '@/pages/profissionais/DisponibilidadeProfissional'
```

Add routes inside `AppLayout` children, after the orcamentos routes:

```typescript
{ path: '/agendamentos', element: <Agendamentos /> },
{ path: '/agendamentos/novo', element: <NovoAgendamento /> },
{ path: '/agendamentos/:id', element: <DetalheAgendamento /> },
{ path: '/profissionais', element: <Profissionais /> },
{ path: '/profissionais/:id/disponibilidade', element: <DisponibilidadeProfissional /> },
```

- [ ] **Step 2: Update Sidebar.tsx**

Add `Calendar` and `UserCog` to the lucide-react import:

```typescript
import { LayoutDashboard, ShoppingCart, Package, Wallet, Users, BarChart3, ArrowDownToLine, ClipboardList, TrendingDown, TrendingUp, FileText, Calendar, UserCog } from 'lucide-react'
```

Add two entries to the `links` array after the orcamentos entry (`{ to: '/orcamentos', ... }`):

```typescript
{ to: '/agendamentos', icon: Calendar, label: 'Agendamentos' },
{ to: '/profissionais', icon: UserCog, label: 'Profissionais' },
```

- [ ] **Step 3: Build frontend**

```bash
cd frontend && npm run build
```

Expected: Build succeeded, no TypeScript errors.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/router/index.tsx frontend/src/components/layout/Sidebar.tsx
git commit -m "feat: wire Agendamentos and Profissionais into router and sidebar"
```

---

## Task 16: Tests

**Files:**
- Create: `frontend/src/test/agendamentos.test.tsx`

- [ ] **Step 1: Write failing tests**

```tsx
// frontend/src/test/agendamentos.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'

const mockCreate = vi.fn()
const mockSlots = vi.fn()

vi.mock('@/hooks/useAgendamentos', () => ({
  useAgendamentos: () => ({
    agendamentos: [
      {
        id: '1',
        profissionalId: 'p1',
        profissionalNome: 'Ana',
        clienteNome: 'João',
        servicoNome: 'Corte',
        dataHoraInicio: '2026-05-25T10:00:00Z',
        dataHoraFim: '2026-05-25T10:30:00Z',
        status: 'Agendado',
      },
      {
        id: '2',
        profissionalId: 'p2',
        profissionalNome: 'Carlos',
        clienteNome: 'Maria',
        servicoNome: 'Manicure',
        dataHoraInicio: '2026-05-25T11:00:00Z',
        dataHoraFim: '2026-05-25T11:30:00Z',
        status: 'Confirmado',
      },
    ],
    agendamento: null,
    loading: false,
    error: null,
    list: vi.fn(),
    get: vi.fn(),
    create: mockCreate,
    confirmar: vi.fn(),
    concluir: vi.fn(),
    cancelar: vi.fn(),
    slots: mockSlots,
  }),
}))

vi.mock('@/hooks/useProfissionais', () => ({
  useProfissionais: () => ({
    profissionais: [
      { id: 'p1', nome: 'Ana', telefone: null, ativo: true },
      { id: 'p2', nome: 'Carlos', telefone: null, ativo: true },
    ],
    loading: false,
    error: null,
    list: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    remove: vi.fn(),
    getDisponibilidade: vi.fn().mockResolvedValue([]),
    saveDisponibilidade: vi.fn(),
    listBloqueios: vi.fn(),
    criarBloqueio: vi.fn(),
    deleteBloqueio: vi.fn(),
  }),
}))

vi.mock('@/hooks/useEstoque', () => ({
  useEstoque: () => ({
    produtos: [
      { id: 'srv1', nome: 'Corte', precoVenda: 40, duracaoMinutos: 30, ativo: true },
    ],
    listProdutos: vi.fn(),
  }),
}))

vi.mock('@/hooks/useClientes', () => ({
  useClientes: () => ({ clientes: [], list: vi.fn() }),
}))

import Agendamentos from '@/pages/agendamentos/Agendamentos'
import NovoAgendamento from '@/pages/agendamentos/NovoAgendamento'
import Profissionais from '@/pages/profissionais/Profissionais'

describe('Agendamentos grid', () => {
  it('renderiza uma coluna por profissional ativo', () => {
    render(<MemoryRouter><Agendamentos /></MemoryRouter>)
    expect(screen.getByText('Ana')).toBeTruthy()
    expect(screen.getByText('Carlos')).toBeTruthy()
  })

  it('exibe cards dos agendamentos do dia', () => {
    render(<MemoryRouter><Agendamentos /></MemoryRouter>)
    expect(screen.getByText('João')).toBeTruthy()
    expect(screen.getByText('Maria')).toBeTruthy()
  })

  it('exibe badges de status com texto correto', () => {
    render(<MemoryRouter><Agendamentos /></MemoryRouter>)
    expect(screen.getByText('Agendado')).toBeTruthy()
    expect(screen.getByText('Confirmado')).toBeTruthy()
  })

  it('exibe botão Novo Agendamento', () => {
    render(<MemoryRouter><Agendamentos /></MemoryRouter>)
    expect(screen.getByRole('button', { name: /Novo Agendamento/i })).toBeTruthy()
  })
})

describe('NovoAgendamento form', () => {
  beforeEach(() => { mockCreate.mockReset(); mockSlots.mockResolvedValue([]) })

  it('renderiza selects de profissional e serviço', () => {
    render(<MemoryRouter><NovoAgendamento /></MemoryRouter>)
    expect(screen.getByText(/Profissional \*/i)).toBeTruthy()
    expect(screen.getByText(/Serviço \*/i)).toBeTruthy()
  })

  it('exibe serviços com duracaoMinutos > 0', () => {
    render(<MemoryRouter><NovoAgendamento /></MemoryRouter>)
    expect(screen.getByText(/Corte \(30 min\)/i)).toBeTruthy()
  })

  it('mostra erros ao tentar agendar sem campos obrigatórios', () => {
    render(<MemoryRouter><NovoAgendamento /></MemoryRouter>)
    fireEvent.click(screen.getByRole('button', { name: /Agendar/i }))
    expect(screen.getByText('Profissional obrigatório')).toBeTruthy()
    expect(screen.getByText('Telefone obrigatório')).toBeTruthy()
  })
})

describe('Profissionais page', () => {
  it('renderiza tabela com profissionais', () => {
    render(<MemoryRouter><Profissionais /></MemoryRouter>)
    expect(screen.getByText('Ana')).toBeTruthy()
    expect(screen.getByText('Carlos')).toBeTruthy()
  })

  it('exibe botão Novo Profissional', () => {
    render(<MemoryRouter><Profissionais /></MemoryRouter>)
    expect(screen.getByRole('button', { name: /Novo Profissional/i })).toBeTruthy()
  })

  it('exibe formulário ao clicar em Novo Profissional', () => {
    render(<MemoryRouter><Profissionais /></MemoryRouter>)
    fireEvent.click(screen.getByRole('button', { name: /Novo Profissional/i }))
    expect(screen.getByPlaceholderText(/Nome completo/i)).toBeTruthy()
  })
})
```

- [ ] **Step 2: Run tests to verify they fail first**

```bash
cd frontend && npm test -- agendamentos.test.tsx
```

Expected: Tests fail because pages don't exist yet at this step.

Actually, since the pages are created in previous tasks, they should pass. Run to confirm:

```bash
cd frontend && npm test -- agendamentos.test.tsx
```

Expected: All tests pass (PASS, 10 tests).

- [ ] **Step 3: Run full test suite to check for regressions**

```bash
cd frontend && npm test
```

Expected: All tests pass.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/test/agendamentos.test.tsx
git commit -m "test: add Agendamentos module tests"
```

---

## Self-Review Checklist

After all tasks, verify:

- [ ] All entities implement `ITenantEntity` where appropriate (Profissional ✓, BloqueioAgenda ✓, Agendamento ✓; DisponibilidadeSemanal intentionally not)
- [ ] `HasQueryFilter` added for Profissional, BloqueioAgenda, Agendamento in AppDbContext ✓
- [ ] `ConcluirAsync` uses try/catch transaction pattern ✓
- [ ] `AgendamentoService.CriarAsync` validates disponibilidade + bloqueios + conflicts ✓
- [ ] DTOs used in all responses — no entity returned directly ✓
- [ ] `/slots` route defined before `/{id:guid}` in AgendamentosEndpoints ✓
- [ ] EF migration generated ✓
- [ ] Frontend tests cover list render, form validation, and status display ✓
