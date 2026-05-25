# GestorAI — Plano 4/5: Financeiro

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Módulo Financeiro completo — Lançamentos (receitas e despesas), Contas a Pagar, Contas a Receber e Fluxo de Caixa. Lançamentos criados automaticamente pelo módulo de Vendas já aparecem aqui.

**Architecture:** `LancamentoService` unifica receitas e despesas em uma única entidade com filtros por `Tipo` e `Status`. Contas a pagar = `Tipo=Despesa`, contas a receber = `Tipo=Receita + Status=Pendente`. Lançamentos vencidos calculados por query (sem campo extra no banco). Pagamento é uma ação explícita do usuário.

**Tech Stack:** .NET 10, EF Core 9 | React 18, TypeScript, shadcn/ui, React Hook Form, Zod

**Pré-requisito:** Plano 3 concluído — `Lancamento` já existe no banco e é criado pelas vendas.

---

## Mapa de arquivos

### Backend

| Arquivo | Responsabilidade |
|---|---|
| `src/GestorAI.API/DTOs/Financeiro/LancamentoDto.cs` | Records request/response de lançamentos |
| `src/GestorAI.API/Services/Financeiro/LancamentoService.cs` | CRUD, pagamento, cancelamento e fluxo de caixa |
| `src/GestorAI.API/Services/Financeiro/CreateLancamentoValidator.cs` | Validação FluentValidation |
| `src/GestorAI.API/Endpoints/FinanceiroEndpoints.cs` | Minimal API routes |
| `tests/GestorAI.Tests/Services/LancamentoServiceTests.cs` | Testes do LancamentoService |

### Frontend

| Arquivo | Responsabilidade |
|---|---|
| `src/types/financeiro.ts` | Types TypeScript |
| `src/hooks/useFinanceiro.ts` | Estado + operações financeiras |
| `src/pages/financeiro/Lancamentos.tsx` | Visão unificada de receitas e despesas |
| `src/pages/financeiro/ContasPagar.tsx` | Despesas pendentes com alerta de vencidas |
| `src/pages/financeiro/ContasReceber.tsx` | Receitas pendentes incluindo vendas |
| `src/components/financeiro/LancamentoForm.tsx` | Form de novo lançamento |

---

## Task 1: DTOs de Financeiro

**Files:**
- Create: `src/GestorAI.API/DTOs/Financeiro/LancamentoDto.cs`

- [ ] **Step 1: Criar diretório e arquivo**

```bash
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend/src/GestorAI.API/DTOs/Financeiro
```

`src/GestorAI.API/DTOs/Financeiro/LancamentoDto.cs`:
```csharp
namespace GestorAI.API.DTOs.Financeiro;

public record LancamentoResponse(
    Guid Id,
    string Tipo,
    string Descricao,
    decimal Valor,
    DateTime DataVencimento,
    DateTime? DataPagamento,
    string Status,
    string Categoria,
    Guid? VendaId,
    string? Observacao,
    bool Vencido);

public record CreateLancamentoRequest(
    string Tipo,
    string Descricao,
    decimal Valor,
    DateTime DataVencimento,
    string Categoria,
    string? Observacao);

public record PagarLancamentoRequest(DateTime DataPagamento);

public record FluxoCaixaItemResponse(DateTime Data, decimal Receitas, decimal Despesas, decimal Saldo);

public record FluxoCaixaResponse(
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal SaldoFinal,
    List<FluxoCaixaItemResponse> Itens);
```

- [ ] **Step 2: Build**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend && dotnet build
```

Expected: `Build succeeded.`

---

## Task 2: Validator de Lançamento

**Files:**
- Create: `src/GestorAI.API/Services/Financeiro/CreateLancamentoValidator.cs`

- [ ] **Step 1: Criar diretório e validator**

```bash
mkdir -p src/GestorAI.API/Services/Financeiro
```

`src/GestorAI.API/Services/Financeiro/CreateLancamentoValidator.cs`:
```csharp
using FluentValidation;
using GestorAI.API.DTOs.Financeiro;

namespace GestorAI.API.Services.Financeiro;

public class CreateLancamentoValidator : AbstractValidator<CreateLancamentoRequest>
{
    public CreateLancamentoValidator()
    {
        RuleFor(x => x.Tipo)
            .Must(t => t is "Receita" or "Despesa")
            .WithMessage("Tipo deve ser 'Receita' ou 'Despesa'.");
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.DataVencimento).NotEmpty();
        RuleFor(x => x.Categoria).NotEmpty().MaximumLength(100);
    }
}
```

- [ ] **Step 2: Build**

```bash
dotnet build
```

Expected: `Build succeeded.`

---

## Task 3: LancamentoService com testes

**Files:**
- Create: `src/GestorAI.API/Services/Financeiro/LancamentoService.cs`
- Test: `tests/GestorAI.Tests/Services/LancamentoServiceTests.cs`

- [ ] **Step 1: Escrever testes (TDD — red)**

`tests/GestorAI.Tests/Services/LancamentoServiceTests.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class LancamentoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, LancamentoService service) Setup()
    {
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new LancamentoService(db, tenantContext));
    }

    [Fact]
    public async Task CreateAsync_PersisteDespesa()
    {
        var (_, service) = Setup();
        var req = new CreateLancamentoRequest(
            "Despesa", "Aluguel", 1500m,
            DateTime.Today.AddDays(5), "Aluguel", null);

        var result = await service.CreateAsync(req, default);

        Assert.Equal("Despesa", result.Tipo);
        Assert.Equal(1500m, result.Valor);
        Assert.Equal("Pendente", result.Status);
        Assert.False(result.Vencido);
    }

    [Fact]
    public async Task PagarAsync_SetaStatusPagoEDataPagamento()
    {
        var (db, service) = Setup();
        var lancamento = new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
            Descricao = "Água", Valor = 100m,
            DataVencimento = DateTime.Today.AddDays(-1),
            Status = StatusLancamento.Pendente,
            Categoria = "Utilidades"
        };
        db.Lancamentos.Add(lancamento);
        await db.SaveChangesAsync();

        var dataPagamento = DateTime.Today;
        var result = await service.PagarAsync(lancamento.Id, new PagarLancamentoRequest(dataPagamento), default);

        Assert.Equal("Pago", result.Status);
        Assert.Equal(dataPagamento, result.DataPagamento!.Value.Date);
    }

    [Fact]
    public async Task PagarAsync_ThrowsSeJaPago()
    {
        var (db, service) = Setup();
        var lancamento = new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
            Descricao = "Já pago", Valor = 50m,
            DataVencimento = DateTime.Today,
            DataPagamento = DateTime.Today,
            Status = StatusLancamento.Pago,
            Categoria = "Outros"
        };
        db.Lancamentos.Add(lancamento);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() =>
            service.PagarAsync(lancamento.Id, new PagarLancamentoRequest(DateTime.Today), default));
    }

    [Fact]
    public async Task ListAsync_VencidoCalculadoPorQuery()
    {
        var (db, service) = Setup();
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
            Descricao = "Vencida", Valor = 200m,
            DataVencimento = DateTime.Today.AddDays(-3),
            Status = StatusLancamento.Pendente,
            Categoria = "Outros"
        });
        await db.SaveChangesAsync();

        var result = await service.ListAsync(null, null, null, default);

        Assert.Single(result);
        Assert.True(result[0].Vencido);
    }

    [Fact]
    public async Task GetFluxoCaixaAsync_AgregaPorDia()
    {
        var (db, service) = Setup();
        var hoje = DateTime.Today;
        db.Lancamentos.AddRange(
            new Lancamento
            {
                EmpresaId = _empresaId, Tipo = TipoLancamento.Receita,
                Descricao = "Venda", Valor = 500m,
                DataVencimento = hoje, DataPagamento = hoje,
                Status = StatusLancamento.Pago, Categoria = "Venda"
            },
            new Lancamento
            {
                EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
                Descricao = "Fornecedor", Valor = 200m,
                DataVencimento = hoje, DataPagamento = hoje,
                Status = StatusLancamento.Pago, Categoria = "Compras"
            });
        await db.SaveChangesAsync();

        var result = await service.GetFluxoCaixaAsync(hoje, hoje, default);

        Assert.Equal(500m, result.TotalReceitas);
        Assert.Equal(200m, result.TotalDespesas);
        Assert.Equal(300m, result.SaldoFinal);
    }
}
```

- [ ] **Step 2: Rodar — confirmar que falham**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test tests/GestorAI.Tests --filter "LancamentoServiceTests"
```

Expected: Erro de compilação — `LancamentoService` não existe.

- [ ] **Step 3: Criar LancamentoService.cs**

`src/GestorAI.API/Services/Financeiro/LancamentoService.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Financeiro;

public class LancamentoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<LancamentoResponse>> ListAsync(
        string? tipo, string? status, DateTime? vencimentoAte, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var query = db.Lancamentos.AsQueryable();

        if (!string.IsNullOrEmpty(tipo) && Enum.TryParse<TipoLancamento>(tipo, out var t))
            query = query.Where(l => l.Tipo == t);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusLancamento>(status, out var s))
            query = query.Where(l => l.Status == s);
        if (vencimentoAte.HasValue)
            query = query.Where(l => l.DataVencimento <= vencimentoAte.Value);

        return await query
            .OrderBy(l => l.DataVencimento)
            .Select(l => ToResponse(l, hoje))
            .ToListAsync(ct);
    }

    public async Task<LancamentoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var l = await db.Lancamentos.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);
        return ToResponse(l, hoje);
    }

    public async Task<LancamentoResponse> CreateAsync(CreateLancamentoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoLancamento>(req.Tipo, out var tipo))
            throw new AppException("Tipo inválido.");

        var lancamento = new Lancamento
        {
            EmpresaId = tenantContext.EmpresaId,
            Tipo = tipo,
            Descricao = req.Descricao,
            Valor = req.Valor,
            DataVencimento = req.DataVencimento,
            Status = StatusLancamento.Pendente,
            Categoria = req.Categoria,
            Observacao = req.Observacao,
        };
        db.Lancamentos.Add(lancamento);
        await db.SaveChangesAsync(ct);
        return ToResponse(lancamento, DateTime.UtcNow.Date);
    }

    public async Task<LancamentoResponse> PagarAsync(
        Guid id, PagarLancamentoRequest req, CancellationToken ct)
    {
        var lancamento = await db.Lancamentos.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (lancamento.Status == StatusLancamento.Pago)
            throw new AppException("Lançamento já está pago.");
        if (lancamento.Status == StatusLancamento.Cancelado)
            throw new AppException("Lançamento cancelado não pode ser pago.");

        lancamento.Status = StatusLancamento.Pago;
        lancamento.DataPagamento = req.DataPagamento;

        await db.SaveChangesAsync(ct);
        return ToResponse(lancamento, DateTime.UtcNow.Date);
    }

    public async Task<LancamentoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var lancamento = await db.Lancamentos.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (lancamento.Status == StatusLancamento.Pago)
            throw new AppException("Lançamento pago não pode ser cancelado.");

        lancamento.Status = StatusLancamento.Cancelado;
        await db.SaveChangesAsync(ct);
        return ToResponse(lancamento, DateTime.UtcNow.Date);
    }

    public async Task<FluxoCaixaResponse> GetFluxoCaixaAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var lancamentos = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pago
                && l.DataPagamento.HasValue
                && l.DataPagamento.Value.Date >= de.Date
                && l.DataPagamento.Value.Date <= ate.Date)
            .ToListAsync(ct);

        var agrupados = lancamentos
            .GroupBy(l => l.DataPagamento!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g => new FluxoCaixaItemResponse(
                g.Key,
                g.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor),
                g.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor),
                g.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor)
                - g.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor)))
            .ToList();

        return new FluxoCaixaResponse(
            lancamentos.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor),
            lancamentos.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor),
            lancamentos.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor)
            - lancamentos.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor),
            agrupados);
    }

    private static LancamentoResponse ToResponse(Lancamento l, DateTime hoje) => new(
        l.Id, l.Tipo.ToString(), l.Descricao, l.Valor,
        l.DataVencimento, l.DataPagamento, l.Status.ToString(),
        l.Categoria, l.VendaId, l.Observacao,
        l.Status == StatusLancamento.Pendente && l.DataVencimento.Date < hoje);
}
```

- [ ] **Step 4: Rodar testes — confirmar que passam**

```bash
dotnet test tests/GestorAI.Tests --filter "LancamentoServiceTests"
```

Expected: `Passed! - Failed: 0, Passed: 5`

- [ ] **Step 5: Rodar todos os testes**

```bash
dotnet test tests/GestorAI.Tests
```

Expected: Todos passando.

- [ ] **Step 6: Commit**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add backend/
git commit -m "feat: LancamentoService com fluxo de caixa e testes"
```

---

## Task 4: Endpoints Financeiro

**Files:**
- Create: `src/GestorAI.API/Endpoints/FinanceiroEndpoints.cs`
- Modify: `src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar FinanceiroEndpoints.cs**

`src/GestorAI.API/Endpoints/FinanceiroEndpoints.cs`:
```csharp
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class FinanceiroEndpoints
{
    public static void MapFinanceiro(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization("FinanceAccess");

        group.MapGet("/lancamentos", async (
            string? tipo, string? status, DateTime? vencimentoAte,
            LancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(tipo, status, vencimentoAte, ct)));

        group.MapGet("/lancamentos/{id:guid}", async (
            Guid id, LancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/lancamentos", async (
            CreateLancamentoRequest req, LancamentoService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/lancamentos/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateLancamentoRequest>>();

        group.MapPost("/lancamentos/{id:guid}/pagar", async (
            Guid id, PagarLancamentoRequest req, LancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.PagarAsync(id, req, ct)));

        group.MapPost("/lancamentos/{id:guid}/cancelar", async (
            Guid id, LancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapGet("/financeiro/fluxo-caixa", async (
            DateTime de, DateTime ate, LancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetFluxoCaixaAsync(de, ate, ct)));
    }
}
```

- [ ] **Step 2: Registrar em Program.cs**

Adicionar antes de `var app = builder.Build();`:
```csharp
builder.Services.AddScoped<LancamentoService>();
builder.Services.AddScoped<IValidator<CreateLancamentoRequest>, CreateLancamentoValidator>();
```

Adicionar usings:
```csharp
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Services.Financeiro;
```

Adicionar após `app.MapVendas();`:
```csharp
app.MapFinanceiro();
```

- [ ] **Step 3: Build e testes**

```bash
dotnet build && dotnet test tests/GestorAI.Tests
```

Expected: Tudo passando.

- [ ] **Step 4: Commit**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add backend/
git commit -m "feat: endpoints Financeiro"
```

---

## Task 5: Types e hook Financeiro

**Files:**
- Create: `src/types/financeiro.ts`
- Create: `src/hooks/useFinanceiro.ts`

- [ ] **Step 1: Criar src/types/financeiro.ts**

`src/types/financeiro.ts`:
```ts
export interface LancamentoResponse {
  id: string
  tipo: 'Receita' | 'Despesa'
  descricao: string
  valor: number
  dataVencimento: string
  dataPagamento: string | null
  status: 'Pendente' | 'Pago' | 'Cancelado'
  categoria: string
  vendaId: string | null
  observacao: string | null
  vencido: boolean
}

export interface CreateLancamentoRequest {
  tipo: 'Receita' | 'Despesa'
  descricao: string
  valor: number
  dataVencimento: string
  categoria: string
  observacao?: string
}

export interface PagarLancamentoRequest {
  dataPagamento: string
}

export interface FluxoCaixaItemResponse {
  data: string
  receitas: number
  despesas: number
  saldo: number
}

export interface FluxoCaixaResponse {
  totalReceitas: number
  totalDespesas: number
  saldoFinal: number
  itens: FluxoCaixaItemResponse[]
}
```

- [ ] **Step 2: Criar src/hooks/useFinanceiro.ts**

`src/hooks/useFinanceiro.ts`:
```ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  LancamentoResponse, CreateLancamentoRequest,
  PagarLancamentoRequest, FluxoCaixaResponse,
} from '@/types/financeiro'

export function useFinanceiro() {
  const [lancamentos, setLancamentos] = useState<LancamentoResponse[]>([])
  const [fluxo, setFluxo] = useState<FluxoCaixaResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (params?: {
    tipo?: string; status?: string; vencimentoAte?: string
  }) => {
    setLoading(true)
    try {
      const qs = new URLSearchParams()
      if (params?.tipo) qs.set('tipo', params.tipo)
      if (params?.status) qs.set('status', params.status)
      if (params?.vencimentoAte) qs.set('vencimentoAte', params.vencimentoAte)
      const data = await api.get<LancamentoResponse[]>(`/api/lancamentos?${qs}`)
      setLancamentos(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar lançamentos')
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CreateLancamentoRequest) => {
    const result = await api.post<LancamentoResponse>('/api/lancamentos', req)
    setLancamentos(prev => [...prev, result])
    return result
  }, [])

  const pagar = useCallback(async (id: string, req: PagarLancamentoRequest) => {
    const result = await api.post<LancamentoResponse>(`/api/lancamentos/${id}/pagar`, req)
    setLancamentos(prev => prev.map(l => l.id === id ? result : l))
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<LancamentoResponse>(`/api/lancamentos/${id}/cancelar`, {})
    setLancamentos(prev => prev.map(l => l.id === id ? result : l))
    return result
  }, [])

  const getFluxoCaixa = useCallback(async (de: string, ate: string) => {
    const data = await api.get<FluxoCaixaResponse>(
      `/api/financeiro/fluxo-caixa?de=${de}&ate=${ate}`)
    setFluxo(data)
    return data
  }, [])

  return { lancamentos, fluxo, loading, error, list, create, pagar, cancelar, getFluxoCaixa }
}
```

- [ ] **Step 3: Build TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend && npm run build
```

Expected: Sem erros.

---

## Task 6: Componente LancamentoForm

**Files:**
- Create: `src/components/financeiro/LancamentoForm.tsx`

- [ ] **Step 1: Criar diretório e componente**

```bash
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/components/financeiro
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/pages/financeiro
```

`src/components/financeiro/LancamentoForm.tsx`:
```tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CreateLancamentoRequest } from '@/types/financeiro'

const schema = z.object({
  tipo: z.enum(['Receita', 'Despesa']),
  descricao: z.string().min(1, 'Descrição obrigatória').max(300),
  valor: z.coerce.number().positive('Valor deve ser maior que zero'),
  dataVencimento: z.string().min(1, 'Data obrigatória'),
  categoria: z.string().min(1, 'Categoria obrigatória').max(100),
  observacao: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

const categoriasDespesa = ['Aluguel', 'Fornecedor', 'Utilidades', 'Salários', 'Marketing', 'Outros']
const categoriasReceita = ['Venda', 'Serviço', 'Outros']

interface Props {
  defaultTipo?: 'Receita' | 'Despesa'
  onSubmit: (data: CreateLancamentoRequest) => Promise<void>
  onCancel: () => void
}

export default function LancamentoForm({ defaultTipo = 'Despesa', onSubmit, onCancel }: Props) {
  const { register, watch, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: { tipo: defaultTipo },
    })

  const tipo = watch('tipo')
  const categorias = tipo === 'Despesa' ? categoriasDespesa : categoriasReceita

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Tipo</Label>
        <div className="flex gap-2">
          {(['Despesa', 'Receita'] as const).map(t => (
            <label key={t} className="flex items-center gap-2 cursor-pointer">
              <input type="radio" value={t} {...register('tipo')} />
              <span className="text-sm">{t}</span>
            </label>
          ))}
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Descrição</Label>
        <Input {...register('descricao')} placeholder="Ex: Aluguel março" />
        {errors.descricao && <p className="text-xs text-destructive">{errors.descricao.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Valor (R$)</Label>
          <Input type="number" step="0.01" {...register('valor')} />
          {errors.valor && <p className="text-xs text-destructive">{errors.valor.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Vencimento</Label>
          <Input type="date" {...register('dataVencimento')} />
          {errors.dataVencimento && <p className="text-xs text-destructive">{errors.dataVencimento.message}</p>}
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Categoria</Label>
        <select {...register('categoria')}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
          <option value="">Selecione...</option>
          {categorias.map(c => <option key={c} value={c}>{c}</option>)}
        </select>
        {errors.categoria && <p className="text-xs text-destructive">{errors.categoria.message}</p>}
      </div>

      <div className="grid gap-2">
        <Label>Observação (opcional)</Label>
        <Input {...register('observacao')} placeholder="Anotações" />
      </div>

      <div className="flex justify-end gap-2 pt-2">
        <Button type="button" variant="outline" onClick={onCancel}>Cancelar</Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Salvando...' : 'Salvar'}
        </Button>
      </div>
    </form>
  )
}
```

---

## Task 7: Páginas Financeiro

**Files:**
- Create: `src/pages/financeiro/Lancamentos.tsx`
- Create: `src/pages/financeiro/ContasPagar.tsx`
- Create: `src/pages/financeiro/ContasReceber.tsx`

- [ ] **Step 1: Criar Lancamentos.tsx**

`src/pages/financeiro/Lancamentos.tsx`:
```tsx
import { useEffect, useState } from 'react'
import { Plus } from 'lucide-react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import LancamentoForm from '@/components/financeiro/LancamentoForm'
import type { CreateLancamentoRequest, LancamentoResponse } from '@/types/financeiro'

const tipoVariant = (tipo: string) => tipo === 'Receita' ? 'secondary' : 'destructive'
const statusVariant = (s: string, vencido: boolean) =>
  s === 'Pago' ? 'secondary' :
  s === 'Cancelado' ? 'outline' :
  vencido ? 'destructive' : 'outline'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function Lancamentos() {
  const { lancamentos, loading, list, create, pagar, cancelar } = useFinanceiro()
  const [modalAberto, setModalAberto] = useState(false)
  const [pagando, setPagando] = useState<string | null>(null)

  useEffect(() => { list() }, [])

  async function handleCreate(data: CreateLancamentoRequest) {
    await create(data)
    setModalAberto(false)
  }

  async function handlePagar(l: LancamentoResponse) {
    if (!confirm(`Confirmar pagamento de ${fmt(l.valor)} — ${l.descricao}?`)) return
    setPagando(l.id)
    try {
      await pagar(l.id, { dataPagamento: new Date().toISOString() })
    } finally { setPagando(null) }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Lançamentos</h1>
        <Button onClick={() => setModalAberto(true)}>
          <Plus size={16} className="mr-2" /> Novo Lançamento
        </Button>
      </div>

      {loading ? <p className="text-muted-foreground">Carregando...</p> : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Descrição</th>
                <th className="px-4 py-3 text-left font-medium">Tipo</th>
                <th className="px-4 py-3 text-left font-medium">Categoria</th>
                <th className="px-4 py-3 text-left font-medium">Vencimento</th>
                <th className="px-4 py-3 text-right font-medium">Valor</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {lancamentos.map(l => (
                <tr key={l.id} className="border-b">
                  <td className="px-4 py-3 font-medium">{l.descricao}</td>
                  <td className="px-4 py-3">
                    <Badge variant={tipoVariant(l.tipo)}>{l.tipo}</Badge>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{l.categoria}</td>
                  <td className="px-4 py-3 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                  <td className="px-4 py-3 text-right font-medium">{fmt(l.valor)}</td>
                  <td className="px-4 py-3">
                    <Badge variant={statusVariant(l.status, l.vencido)}>
                      {l.vencido && l.status === 'Pendente' ? 'Vencida' : l.status}
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    {l.status === 'Pendente' && (
                      <Button size="sm" variant="outline"
                        disabled={pagando === l.id}
                        onClick={() => handlePagar(l)}>
                        {pagando === l.id ? '...' : 'Pagar'}
                      </Button>
                    )}
                  </td>
                </tr>
              ))}
              {lancamentos.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum lançamento encontrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={modalAberto} onOpenChange={setModalAberto}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Novo Lançamento</DialogTitle></DialogHeader>
          <LancamentoForm onSubmit={handleCreate} onCancel={() => setModalAberto(false)} />
        </DialogContent>
      </Dialog>
    </div>
  )
}
```

- [ ] **Step 2: Criar ContasPagar.tsx**

`src/pages/financeiro/ContasPagar.tsx`:
```tsx
import { useEffect, useState } from 'react'
import { AlertTriangle } from 'lucide-react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import type { LancamentoResponse } from '@/types/financeiro'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function ContasPagar() {
  const { lancamentos, loading, list, pagar } = useFinanceiro()
  const [pagando, setPagando] = useState<string | null>(null)

  useEffect(() => { list({ tipo: 'Despesa', status: 'Pendente' }) }, [])

  const vencidas = lancamentos.filter(l => l.vencido)
  const proximas = lancamentos.filter(l => !l.vencido)
  const totalPendente = lancamentos.reduce((s, l) => s + l.valor, 0)

  async function handlePagar(l: LancamentoResponse) {
    if (!confirm(`Confirmar pagamento de ${fmt(l.valor)} — ${l.descricao}?`)) return
    setPagando(l.id)
    try { await pagar(l.id, { dataPagamento: new Date().toISOString() }) }
    finally { setPagando(null) }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Contas a Pagar</h1>
        <div className="text-right">
          <p className="text-sm text-muted-foreground">Total pendente</p>
          <p className="text-xl font-bold text-destructive">{fmt(totalPendente)}</p>
        </div>
      </div>

      {vencidas.length > 0 && (
        <div className="rounded-md border border-destructive/50 bg-destructive/5 p-3 flex gap-2">
          <AlertTriangle size={18} className="text-destructive mt-0.5 shrink-0" />
          <p className="text-sm text-destructive">
            <strong>{vencidas.length} conta(s) vencida(s)</strong> totalizando {fmt(vencidas.reduce((s, l) => s + l.valor, 0))}.
            Regularize o quanto antes.
          </p>
        </div>
      )}

      {loading ? <p className="text-muted-foreground">Carregando...</p> : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Descrição</th>
                <th className="px-4 py-3 text-left font-medium">Categoria</th>
                <th className="px-4 py-3 text-left font-medium">Vencimento</th>
                <th className="px-4 py-3 text-right font-medium">Valor</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {[...vencidas, ...proximas].map(l => (
                <tr key={l.id} className={`border-b ${l.vencido ? 'bg-destructive/5' : ''}`}>
                  <td className="px-4 py-3 font-medium">
                    {l.descricao}
                    {l.vencido && <Badge variant="destructive" className="ml-2 text-xs">Vencida</Badge>}
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{l.categoria}</td>
                  <td className="px-4 py-3 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                  <td className="px-4 py-3 text-right font-medium">{fmt(l.valor)}</td>
                  <td className="px-4 py-3">
                    <Button size="sm" variant="outline"
                      disabled={pagando === l.id}
                      onClick={() => handlePagar(l)}>
                      {pagando === l.id ? '...' : 'Pagar'}
                    </Button>
                  </td>
                </tr>
              ))}
              {lancamentos.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhuma conta a pagar
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 3: Criar ContasReceber.tsx**

`src/pages/financeiro/ContasReceber.tsx`:
```tsx
import { useEffect } from 'react'
import { useFinanceiro } from '@/hooks/useFinanceiro'
import { Badge } from '@/components/ui/badge'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function ContasReceber() {
  const { lancamentos, loading, list } = useFinanceiro()

  useEffect(() => { list({ tipo: 'Receita', status: 'Pendente' }) }, [])

  const total = lancamentos.reduce((s, l) => s + l.valor, 0)

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Contas a Receber</h1>
        <div className="text-right">
          <p className="text-sm text-muted-foreground">Total a receber</p>
          <p className="text-xl font-bold text-primary">{fmt(total)}</p>
        </div>
      </div>

      {loading ? <p className="text-muted-foreground">Carregando...</p> : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Descrição</th>
                <th className="px-4 py-3 text-left font-medium">Categoria</th>
                <th className="px-4 py-3 text-left font-medium">Vencimento</th>
                <th className="px-4 py-3 text-right font-medium">Valor</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {lancamentos.map(l => (
                <tr key={l.id} className="border-b">
                  <td className="px-4 py-3 font-medium">{l.descricao}</td>
                  <td className="px-4 py-3 text-muted-foreground">{l.categoria}</td>
                  <td className="px-4 py-3 text-muted-foreground">{fmtDate(l.dataVencimento)}</td>
                  <td className="px-4 py-3 text-right font-medium">{fmt(l.valor)}</td>
                  <td className="px-4 py-3">
                    <Badge variant={l.vencido ? 'destructive' : 'outline'}>
                      {l.vencido ? 'Vencida' : 'Pendente'}
                    </Badge>
                  </td>
                </tr>
              ))}
              {lancamentos.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhuma conta a receber
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
```

---

## Task 8: Wiring e verificação final

**Files:**
- Modify: `src/router/index.tsx`
- Modify: `src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Atualizar router**

`src/router/index.tsx`:
```tsx
import { createBrowserRouter } from 'react-router-dom'
import AppLayout from '@/components/layout/AppLayout'
import Auth from '@/pages/Auth'
import AuthCallback from '@/pages/AuthCallback'
import Dashboard from '@/pages/Dashboard'
import Produtos from '@/pages/estoque/Produtos'
import Movimentacoes from '@/pages/estoque/Movimentacoes'
import Clientes from '@/pages/clientes/Clientes'
import NovaVenda from '@/pages/vendas/NovaVenda'
import Historico from '@/pages/vendas/Historico'
import Lancamentos from '@/pages/financeiro/Lancamentos'
import ContasPagar from '@/pages/financeiro/ContasPagar'
import ContasReceber from '@/pages/financeiro/ContasReceber'

export const router = createBrowserRouter([
  { path: '/auth', element: <Auth /> },
  { path: '/auth/callback', element: <AuthCallback /> },
  {
    element: <AppLayout />,
    children: [
      { path: '/', element: <Dashboard /> },
      { path: '/vendas', element: <Historico /> },
      { path: '/vendas/nova', element: <NovaVenda /> },
      { path: '/estoque', element: <Produtos /> },
      { path: '/estoque/movimentacoes', element: <Movimentacoes /> },
      { path: '/financeiro', element: <Lancamentos /> },
      { path: '/financeiro/pagar', element: <ContasPagar /> },
      { path: '/financeiro/receber', element: <ContasReceber /> },
      { path: '/clientes', element: <Clientes /> },
      { path: '/relatorios', element: <div>Relatórios — Plano 5</div> },
    ],
  },
])
```

- [ ] **Step 2: Atualizar Sidebar com links financeiros**

Em `src/components/layout/Sidebar.tsx`, substituir o link de Financeiro por três links:
```tsx
{ to: '/financeiro', icon: Wallet, label: 'Lançamentos' },
{ to: '/financeiro/pagar', icon: TrendingDown, label: 'Contas a Pagar' },
{ to: '/financeiro/receber', icon: TrendingUp, label: 'Contas a Receber' },
```

Adicionar imports: `import { TrendingDown, TrendingUp } from 'lucide-react'`

- [ ] **Step 3: Build e testes**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend && npm run build
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend && dotnet test
```

Expected: Sem erros TypeScript; todos os testes passando.

- [ ] **Step 4: Commit final**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add .
git commit -m "feat: Plano 4 completo — módulo Financeiro funcional"
```

---

## Checklist de entrega

- [ ] `GET /api/lancamentos` filtra por tipo, status e vencimento
- [ ] `POST /api/lancamentos/{id}/pagar` seta Status=Pago — throw se já pago
- [ ] Lançamentos criados pelas vendas aparecem em Contas a Receber
- [ ] Campo `Vencido` calculado em query (sem campo no banco)
- [ ] Testes passando: LancamentoServiceTests (5)
- [ ] Página Lançamentos: lista, cria e paga lançamentos
- [ ] Página Contas a Pagar: destaca vencidas com alerta e badge vermelho
- [ ] Página Contas a Receber: exibe receitas pendentes incluindo de vendas
- [ ] Build TypeScript sem erros
