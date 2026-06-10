# Categorias de Lançamentos — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Substituir as listas hardcoded de categorias no frontend por uma tabela `categorias_lancamento` gerenciável por empresa, com CRUD completo e validação na criação/edição de lançamentos.

**Architecture:** Nova entidade `CategoriaLancamento` (sem FK em lançamentos — `Lancamento.Categoria` permanece `string`). Criação/edição de lançamentos valida que a categoria existe na tabela. Renomear categoria atualiza todos os lançamentos existentes via `ExecuteUpdateAsync`. Exclusão bloqueada se algum lançamento usa a categoria.

**Tech Stack:** .NET 10 Minimal APIs, EF Core 10, PostgreSQL, FluentValidation, React + TypeScript + Tailwind CSS, xUnit

**Dependência:** Este plano deve ser executado após o plano `2026-06-09-melhorias-ux.md` (ou ao menos após o Task 2 dele, que cria o `UpdateLancamentoValidator`).

---

## File Map

**Criar:**
- `backend/src/GestorAI.API/Domain/Entities/CategoriaLancamento.cs`
- `backend/src/GestorAI.API/DTOs/Financeiro/CategoriaLancamentoDto.cs`
- `backend/src/GestorAI.API/Services/Financeiro/CategoriaLancamentoService.cs`
- `backend/src/GestorAI.API/Endpoints/CategoriasLancamentoEndpoints.cs`
- `backend/tests/GestorAI.Tests/Services/CategoriaLancamentoServiceTests.cs`
- `frontend/src/hooks/useCategoriasLancamento.ts`
- `frontend/src/pages/financeiro/Categorias.tsx`

**Modificar:**
- `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`
- `backend/src/GestorAI.API/Services/Financeiro/CreateLancamentoValidator.cs`
- `backend/src/GestorAI.API/Services/Financeiro/UpdateLancamentoValidator.cs` *(criado no plano melhorias-ux Task 2)*
- `backend/src/GestorAI.API/Program.cs`
- `frontend/src/types/financeiro.ts`
- `frontend/src/components/financeiro/LancamentoForm.tsx`
- `frontend/src/components/layout/Sidebar.tsx`
- `frontend/src/router/index.tsx`

---

## Task 1: Entidade, AppDbContext e Migration

**Files:**
- Create: `backend/src/GestorAI.API/Domain/Entities/CategoriaLancamento.cs`
- Modify: `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: Criar a entidade**

```csharp
// backend/src/GestorAI.API/Domain/Entities/CategoriaLancamento.cs
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class CategoriaLancamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public TipoLancamento Tipo { get; set; }
}
```

- [ ] **Step 2: Adicionar DbSet e QueryFilter em AppDbContext**

Em `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`, adicionar após `public DbSet<AutomacaoLog> AutomacaoLogs`:

```csharp
    public DbSet<CategoriaLancamento> CategoriasLancamento => Set<CategoriaLancamento>();
```

E em `OnModelCreating`, após a linha `modelBuilder.Entity<AutomacaoLog>().HasIndex(...)`:

```csharp
        modelBuilder.Entity<CategoriaLancamento>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<CategoriaLancamento>()
            .HasIndex(c => new { c.EmpresaId, c.Tipo, c.Nome })
            .IsUnique();
```

- [ ] **Step 3: Gerar a migration**

```bash
cd backend/src/GestorAI.API
dotnet ef migrations add AddCategoriaLancamento
```

Expected: novo arquivo em `Migrations/` com `UpAsync` criando a tabela `categorias_lancamento`.

- [ ] **Step 4: Adicionar seed na migration**

Abra o arquivo de migration gerado. No método `Up`, após o `CreateTable`, adicione:

```csharp
migrationBuilder.Sql(@"
    INSERT INTO categorias_lancamento (id, empresa_id, nome, tipo)
    SELECT gen_random_uuid(), l.empresa_id, cat.nome, cat.tipo_val
    FROM (SELECT DISTINCT empresa_id FROM lancamentos) l
    CROSS JOIN (VALUES
        ('Aluguel',    1),
        ('Fornecedor', 1),
        ('Utilidades', 1),
        ('Salários',   1),
        ('Marketing',  1),
        ('Outros',     1),
        ('Venda',      0),
        ('Serviço',    0),
        ('Outros',     0)
    ) AS cat(nome, tipo_val)
    ON CONFLICT (empresa_id, tipo, nome) DO NOTHING;
");
```

*(TipoLancamento.Receita = 0, TipoLancamento.Despesa = 1 — ordem da enum)*

- [ ] **Step 5: Aplicar a migration**

```bash
cd backend/src/GestorAI.API
dotnet ef database update
```

Expected: `Done.` sem erros.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Entities/CategoriaLancamento.cs \
        backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs \
        backend/src/GestorAI.API/Migrations/
git commit -m "feat: add CategoriaLancamento entity, DbContext, migration with seed"
```

---

## Task 2: DTOs + Service + Testes

**Files:**
- Create: `backend/src/GestorAI.API/DTOs/Financeiro/CategoriaLancamentoDto.cs`
- Create: `backend/src/GestorAI.API/Services/Financeiro/CategoriaLancamentoService.cs`
- Create: `backend/tests/GestorAI.Tests/Services/CategoriaLancamentoServiceTests.cs`

- [ ] **Step 1: Escrever os testes (falham primeiro)**

```csharp
// backend/tests/GestorAI.Tests/Services/CategoriaLancamentoServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Financeiro;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class CategoriaLancamentoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, CategoriaLancamentoService svc) Setup()
    {
        var tc = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tc);
        return (db, new CategoriaLancamentoService(db, tc));
    }

    [Fact]
    public async Task CreateAsync_CriaCategoria_ComNomeUnico()
    {
        var (_, svc) = Setup();
        var result = await svc.CreateAsync(
            new CreateCategoriaLancamentoRequest("Honorários", "Receita"), default);
        Assert.Equal("Honorários", result.Nome);
        Assert.Equal("Receita", result.Tipo);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateAsync_LancaExcecao_QuandoNomeDuplicadoNoMesmoTipo()
    {
        var (db, svc) = Setup();
        db.CategoriasLancamento.Add(new CategoriaLancamento
        {
            EmpresaId = _empresaId,
            Nome = "Honorários",
            Tipo = TipoLancamento.Receita
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            svc.CreateAsync(new CreateCategoriaLancamentoRequest("Honorários", "Receita"), default));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UpdateAsync_RenomeiaCom_NomeValido_EAtualizaLancamentosExistentes()
    {
        var (db, svc) = Setup();
        var cat = new CategoriaLancamento
        {
            EmpresaId = _empresaId,
            Nome = "Aluguel",
            Tipo = TipoLancamento.Despesa
        };
        db.CategoriasLancamento.Add(cat);
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId,
            Tipo = TipoLancamento.Despesa,
            Descricao = "Aluguel março",
            Valor = 1500m,
            DataVencimento = DateTime.Today,
            Categoria = "Aluguel",
            Status = GestorAI.API.Domain.Enums.StatusLancamento.Pendente
        });
        await db.SaveChangesAsync();

        var result = await svc.UpdateAsync(cat.Id, new UpdateCategoriaLancamentoRequest("Aluguel Comercial"), default);

        Assert.Equal("Aluguel Comercial", result.Nome);
        var lancamento = await db.Lancamentos.IgnoreQueryFilters().FirstAsync();
        Assert.Equal("Aluguel Comercial", lancamento.Categoria);
    }

    [Fact]
    public async Task DeleteAsync_RemoveCategoria_QuandoSemLancamentosVinculados()
    {
        var (db, svc) = Setup();
        var cat = new CategoriaLancamento
        {
            EmpresaId = _empresaId,
            Nome = "Marketing",
            Tipo = TipoLancamento.Despesa
        };
        db.CategoriasLancamento.Add(cat);
        await db.SaveChangesAsync();

        await svc.DeleteAsync(cat.Id, default);

        Assert.Empty(db.CategoriasLancamento.IgnoreQueryFilters().ToList());
    }

    [Fact]
    public async Task DeleteAsync_LancaExcecao_QuandoLancamentoUsaCategoria()
    {
        var (db, svc) = Setup();
        var cat = new CategoriaLancamento
        {
            EmpresaId = _empresaId,
            Nome = "Marketing",
            Tipo = TipoLancamento.Despesa
        };
        db.CategoriasLancamento.Add(cat);
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = _empresaId,
            Tipo = TipoLancamento.Despesa,
            Descricao = "Anúncio Google",
            Valor = 300m,
            DataVencimento = DateTime.Today,
            Categoria = "Marketing",
            Status = GestorAI.API.Domain.Enums.StatusLancamento.Pendente
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            svc.DeleteAsync(cat.Id, default));
        Assert.Equal(400, ex.StatusCode);
    }
}
```

- [ ] **Step 2: Rodar os testes — devem falhar**

```bash
cd backend
dotnet test --filter "CategoriaLancamentoServiceTests" -v minimal
```

Expected: compilation error — `CategoriaLancamentoService` não existe ainda.

- [ ] **Step 3: Criar os DTOs**

```csharp
// backend/src/GestorAI.API/DTOs/Financeiro/CategoriaLancamentoDto.cs
namespace GestorAI.API.DTOs.Financeiro;

public record CategoriaLancamentoResponse(Guid Id, string Nome, string Tipo);
public record CreateCategoriaLancamentoRequest(string Nome, string Tipo);
public record UpdateCategoriaLancamentoRequest(string Nome);
```

- [ ] **Step 4: Criar o serviço**

```csharp
// backend/src/GestorAI.API/Services/Financeiro/CategoriaLancamentoService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Financeiro;

public class CategoriaLancamentoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<CategoriaLancamentoResponse>> ListAsync(string? tipo, CancellationToken ct)
    {
        var query = db.CategoriasLancamento.AsQueryable();

        if (!string.IsNullOrEmpty(tipo) && Enum.TryParse<TipoLancamento>(tipo, out var t))
            query = query.Where(c => c.Tipo == t);

        return await query
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaLancamentoResponse(c.Id, c.Nome, c.Tipo.ToString()))
            .ToListAsync(ct);
    }

    public async Task<CategoriaLancamentoResponse> CreateAsync(
        CreateCategoriaLancamentoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoLancamento>(req.Tipo, out var tipo))
            throw new AppException($"Tipo inválido: {req.Tipo}.", 400);

        var existe = await db.CategoriasLancamento
            .AnyAsync(c => c.Nome == req.Nome && c.Tipo == tipo, ct);
        if (existe)
            throw new AppException("Já existe uma categoria com este nome para o tipo informado.", 400);

        var cat = new CategoriaLancamento
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            Tipo = tipo
        };
        db.CategoriasLancamento.Add(cat);
        await db.SaveChangesAsync(ct);
        return new CategoriaLancamentoResponse(cat.Id, cat.Nome, cat.Tipo.ToString());
    }

    public async Task<CategoriaLancamentoResponse> UpdateAsync(
        Guid id, UpdateCategoriaLancamentoRequest req, CancellationToken ct)
    {
        var cat = await db.CategoriasLancamento.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Categoria não encontrada.", 404);

        var nomeAntigo = cat.Nome;

        var duplicado = await db.CategoriasLancamento
            .AnyAsync(c => c.Id != id && c.Nome == req.Nome && c.Tipo == cat.Tipo, ct);
        if (duplicado)
            throw new AppException("Já existe uma categoria com este nome para o tipo informado.", 400);

        cat.Nome = req.Nome;

        var lancamentosParaAtualizar = await db.Lancamentos
            .Where(l => l.Categoria == nomeAntigo)
            .ToListAsync(ct);
        foreach (var l in lancamentosParaAtualizar)
            l.Categoria = req.Nome;

        await db.SaveChangesAsync(ct);

        return new CategoriaLancamentoResponse(cat.Id, cat.Nome, cat.Tipo.ToString());
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var cat = await db.CategoriasLancamento.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Categoria não encontrada.", 404);

        var emUso = await db.Lancamentos
            .AnyAsync(l => l.Categoria == cat.Nome, ct);
        if (emUso)
            throw new AppException(
                "Esta categoria está em uso em lançamentos e não pode ser excluída.", 400);

        db.CategoriasLancamento.Remove(cat);
        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Registrar o serviço em Program.cs**

Em `backend/src/GestorAI.API/Program.cs`, dentro do bloco `// Services — Financeiro`:

```csharp
builder.Services.AddScoped<CategoriaLancamentoService>();
```

- [ ] **Step 6: Rodar os testes — devem passar**

```bash
cd backend
dotnet test --filter "CategoriaLancamentoServiceTests" -v minimal
```

Expected: 5 testes PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Financeiro/CategoriaLancamentoDto.cs \
        backend/src/GestorAI.API/Services/Financeiro/CategoriaLancamentoService.cs \
        backend/tests/GestorAI.Tests/Services/CategoriaLancamentoServiceTests.cs \
        backend/src/GestorAI.API/Program.cs
git commit -m "feat: CategoriaLancamentoService CRUD with tests"
```

---

## Task 3: Endpoints

**Files:**
- Create: `backend/src/GestorAI.API/Endpoints/CategoriasLancamentoEndpoints.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar os endpoints**

```csharp
// backend/src/GestorAI.API/Endpoints/CategoriasLancamentoEndpoints.cs
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Services.Financeiro;

namespace GestorAI.API.Endpoints;

public static class CategoriasLancamentoEndpoints
{
    public static void MapCategoriasLancamento(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categorias-lancamento").RequireAuthorization();

        group.MapGet("", async (string? tipo, CategoriaLancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(tipo, ct)));

        group.MapPost("", async (
            CreateCategoriaLancamentoRequest req,
            CategoriaLancamentoService svc,
            CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/categorias-lancamento/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCategoriaLancamentoRequest req,
            CategoriaLancamentoService svc,
            CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapDelete("/{id:guid}", async (
            Guid id,
            CategoriaLancamentoService svc,
            CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}
```

- [ ] **Step 2: Registrar os endpoints em Program.cs**

Em `backend/src/GestorAI.API/Program.cs`, adicionar após `app.MapFinanceiro()`:

```csharp
app.MapCategoriasLancamento();
```

- [ ] **Step 3: Build para verificar**

```bash
cd backend/src/GestorAI.API
dotnet build
```

Expected: Build succeeded, 0 erros.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Endpoints/CategoriasLancamentoEndpoints.cs \
        backend/src/GestorAI.API/Program.cs
git commit -m "feat: add /api/categorias-lancamento endpoints (CRUD)"
```

---

## Task 4: Validação em CreateLancamentoValidator e UpdateLancamentoValidator

**Files:**
- Modify: `backend/src/GestorAI.API/Services/Financeiro/CreateLancamentoValidator.cs`
- Modify: `backend/src/GestorAI.API/Services/Financeiro/UpdateLancamentoValidator.cs` *(existe após melhorias-ux Task 2)*

- [ ] **Step 1: Atualizar CreateLancamentoValidator com MustAsync**

Substitua o conteúdo completo de `CreateLancamentoValidator.cs`:

```csharp
using FluentValidation;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Financeiro;

public class CreateLancamentoValidator : AbstractValidator<CreateLancamentoRequest>
{
    public CreateLancamentoValidator(AppDbContext db, TenantContext tenantContext)
    {
        RuleFor(x => x.Tipo)
            .Must(t => t is "Receita" or "Despesa")
            .WithMessage("Tipo deve ser 'Receita' ou 'Despesa'.");
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.DataVencimento).NotEmpty();
        RuleFor(x => x.Categoria)
            .NotEmpty().MaximumLength(100)
            .MustAsync(async (request, categoria, ct) =>
            {
                if (!Enum.TryParse<TipoLancamento>(request.Tipo, out var tipo)) return false;
                return await db.CategoriasLancamento
                    .AnyAsync(c => c.Nome == categoria && c.Tipo == tipo, ct);
            })
            .WithMessage("Categoria não encontrada para o tipo informado.");
    }
}
```

- [ ] **Step 2: Adicionar mesma validação em UpdateLancamentoValidator**

No arquivo `backend/src/GestorAI.API/Services/Financeiro/UpdateLancamentoValidator.cs` (criado no plano melhorias-ux Task 2), adicionar injeção de `AppDbContext` e `TenantContext` ao construtor e a regra de `MustAsync` no campo `Categoria`:

```csharp
using FluentValidation;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Financeiro;

public class UpdateLancamentoValidator : AbstractValidator<UpdateLancamentoRequest>
{
    public UpdateLancamentoValidator(AppDbContext db, TenantContext tenantContext)
    {
        RuleFor(x => x.Tipo)
            .Must(t => t is "Receita" or "Despesa")
            .WithMessage("Tipo deve ser 'Receita' ou 'Despesa'.");
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.DataVencimento).NotEmpty();
        RuleFor(x => x.Categoria)
            .NotEmpty().MaximumLength(100)
            .MustAsync(async (request, categoria, ct) =>
            {
                if (!Enum.TryParse<TipoLancamento>(request.Tipo, out var tipo)) return false;
                return await db.CategoriasLancamento
                    .AnyAsync(c => c.Nome == categoria && c.Tipo == tipo, ct);
            })
            .WithMessage("Categoria não encontrada para o tipo informado.");
    }
}
```

- [ ] **Step 3: Rodar suite completa**

```bash
cd backend
dotnet test -v minimal
```

Expected: todos os testes anteriores PASS + novos testes passam.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Services/Financeiro/CreateLancamentoValidator.cs \
        backend/src/GestorAI.API/Services/Financeiro/UpdateLancamentoValidator.cs
git commit -m "feat: validate categoria against DB in lancamento validators"
```

---

## Task 5: Frontend — Tipos e Hook

**Files:**
- Modify: `frontend/src/types/financeiro.ts`
- Create: `frontend/src/hooks/useCategoriasLancamento.ts`

- [ ] **Step 1: Adicionar tipos em financeiro.ts**

Adicione ao final de `frontend/src/types/financeiro.ts`:

```typescript
export interface CategoriaLancamentoResponse {
  id: string
  nome: string
  tipo: 'Receita' | 'Despesa'
}

export interface CreateCategoriaLancamentoRequest {
  nome: string
  tipo: 'Receita' | 'Despesa'
}

export interface UpdateCategoriaLancamentoRequest {
  nome: string
}
```

- [ ] **Step 2: Criar o hook**

```typescript
// frontend/src/hooks/useCategoriasLancamento.ts
import { useCallback } from 'react'
import { api } from '@/services/api'
import type {
  CategoriaLancamentoResponse,
  CreateCategoriaLancamentoRequest,
  UpdateCategoriaLancamentoRequest,
} from '@/types/financeiro'

export function useCategoriasLancamento() {
  const list = useCallback(async (tipo?: string): Promise<CategoriaLancamentoResponse[]> => {
    const qs = tipo ? `?tipo=${tipo}` : ''
    return api.get<CategoriaLancamentoResponse[]>(`/api/categorias-lancamento${qs}`)
  }, [])

  const create = useCallback(async (req: CreateCategoriaLancamentoRequest) =>
    api.post<CategoriaLancamentoResponse>('/api/categorias-lancamento', req), [])

  const update = useCallback(async (id: string, req: UpdateCategoriaLancamentoRequest) =>
    api.put<CategoriaLancamentoResponse>(`/api/categorias-lancamento/${id}`, req), [])

  const remove = useCallback(async (id: string) =>
    api.delete(`/api/categorias-lancamento/${id}`), [])

  return { list, create, update, remove }
}
```

- [ ] **Step 3: Verificar tipagem**

```bash
cd frontend
npx tsc --noEmit
```

Expected: sem erros.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/types/financeiro.ts \
        frontend/src/hooks/useCategoriasLancamento.ts
git commit -m "feat: CategoriaLancamento types and hook"
```

---

## Task 6: Frontend — LancamentoForm dinâmico

**Files:**
- Modify: `frontend/src/components/financeiro/LancamentoForm.tsx`

- [ ] **Step 1: Substituir o LancamentoForm com categorias dinâmicas**

Substitua o conteúdo completo de `frontend/src/components/financeiro/LancamentoForm.tsx`:

```tsx
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useCategoriasLancamento } from '@/hooks/useCategoriasLancamento'
import type { CategoriaLancamentoResponse } from '@/types/financeiro'
import type { CreateLancamentoRequest } from '@/types/financeiro'

const schema = z.object({
  tipo: z.enum(['Receita', 'Despesa']),
  descricao: z.string().min(1, 'Descrição obrigatória').max(300),
  valor: z.string()
    .min(1, 'Valor obrigatório')
    .refine(v => !isNaN(parseFloat(v)) && parseFloat(v) > 0, 'Valor deve ser maior que zero'),
  dataVencimento: z.string().min(1, 'Data obrigatória'),
  categoria: z.string().min(1, 'Categoria obrigatória').max(100),
  observacao: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

interface Props {
  defaultTipo?: 'Receita' | 'Despesa'
  defaultValues?: Partial<FormValues>
  onSubmit: (data: CreateLancamentoRequest) => Promise<void>
  onCancel: () => void
}

export default function LancamentoForm({
  defaultTipo = 'Despesa',
  defaultValues,
  onSubmit,
  onCancel,
}: Props) {
  const { list: listCategorias } = useCategoriasLancamento()
  const [categorias, setCategorias] = useState<CategoriaLancamentoResponse[]>([])

  const { register, watch, handleSubmit, setValue, formState: { errors, isSubmitting } } =
    useForm<FormValues>({
      resolver: zodResolver(schema),
      defaultValues: { tipo: defaultTipo, ...defaultValues },
    })

  const tipo = watch('tipo')

  useEffect(() => {
    void listCategorias(tipo).then(cats => {
      setCategorias(cats)
      if (!defaultValues?.categoria) setValue('categoria', '')
    })
  }, [tipo, listCategorias, setValue, defaultValues?.categoria])

  function handleFormSubmit(data: FormValues) {
    return onSubmit({ ...data, valor: parseFloat(data.valor) })
  }

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
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
          {categorias.map(c => <option key={c.id} value={c.nome}>{c.nome}</option>)}
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

- [ ] **Step 2: Verificar tipagem**

```bash
cd frontend
npx tsc --noEmit
```

Expected: sem erros.

- [ ] **Step 3: Commit**

```bash
git add frontend/src/components/financeiro/LancamentoForm.tsx
git commit -m "feat: LancamentoForm fetches categories from API (dynamic)"
```

---

## Task 7: Página Categorias + Rota + Menu

**Files:**
- Create: `frontend/src/pages/financeiro/Categorias.tsx`
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Criar a página Categorias.tsx**

```tsx
// frontend/src/pages/financeiro/Categorias.tsx
import { useEffect, useState } from 'react'
import { Tag } from 'lucide-react'
import { Pencil, Trash2, Plus } from 'lucide-react'
import { useCategoriasLancamento } from '@/hooks/useCategoriasLancamento'
import { useConfirm } from '@/hooks/useConfirm'
import { toast } from '@/hooks/useToast'
import { Button } from '@/components/ui/button'
import type { CategoriaLancamentoResponse } from '@/types/financeiro'

export default function Categorias() {
  const { list, create, update, remove } = useCategoriasLancamento()
  const { confirm, ConfirmDialogNode } = useConfirm()

  const [tab, setTab] = useState<'Receita' | 'Despesa'>('Despesa')
  const [categorias, setCategorias] = useState<CategoriaLancamentoResponse[]>([])
  const [loading, setLoading] = useState(false)

  const [modalNova, setModalNova] = useState(false)
  const [novoNome, setNovoNome] = useState('')
  const [novoTipo, setNovoTipo] = useState<'Receita' | 'Despesa'>('Despesa')
  const [salvandoNova, setSalvandoNova] = useState(false)

  const [editando, setEditando] = useState<CategoriaLancamentoResponse | null>(null)
  const [editNome, setEditNome] = useState('')
  const [salvandoEdit, setSalvandoEdit] = useState(false)

  const inputClass = 'w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm'

  async function reload() {
    setLoading(true)
    try {
      const data = await list(tab)
      setCategorias(data)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void reload() }, [tab])

  async function handleCriar() {
    if (!novoNome.trim()) return
    setSalvandoNova(true)
    try {
      await create({ nome: novoNome.trim(), tipo: novoTipo })
      setModalNova(false)
      setNovoNome('')
      if (novoTipo === tab) await reload()
      toast.success('Categoria criada!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao criar categoria')
    } finally {
      setSalvandoNova(false)
    }
  }

  async function handleEditar() {
    if (!editando || !editNome.trim()) return
    setSalvandoEdit(true)
    try {
      await update(editando.id, { nome: editNome.trim() })
      setEditando(null)
      await reload()
      toast.success('Categoria atualizada!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao atualizar categoria')
    } finally {
      setSalvandoEdit(false)
    }
  }

  async function handleRemover(cat: CategoriaLancamentoResponse) {
    const ok = await confirm({ title: `Excluir "${cat.nome}"?` })
    if (!ok) return
    try {
      await remove(cat.id)
      await reload()
      toast.success('Categoria excluída!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao excluir categoria')
    }
  }

  return (
    <div className="flex flex-col gap-6 max-w-2xl">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Tag className="h-5 w-5" />
          <h1 className="text-xl font-bold">Categorias de Lançamentos</h1>
        </div>
        <Button size="sm" onClick={() => { setNovoTipo(tab); setModalNova(true) }}>
          <Plus size={14} className="mr-1" /> Nova Categoria
        </Button>
      </div>

      <div className="flex gap-1 border-b">
        {(['Despesa', 'Receita'] as const).map(t => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              tab === t
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            {t}
          </button>
        ))}
      </div>

      {loading ? (
        <p className="text-muted-foreground text-sm">Carregando...</p>
      ) : categorias.length === 0 ? (
        <p className="text-muted-foreground text-sm">Nenhuma categoria cadastrada para {tab}.</p>
      ) : (
        <div className="rounded-md border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Nome</th>
                <th className="px-4 py-3 w-20" />
              </tr>
            </thead>
            <tbody>
              {categorias.map(cat => (
                <tr key={cat.id} className="border-b last:border-0">
                  <td className="px-4 py-3">{cat.nome}</td>
                  <td className="px-4 py-3">
                    <div className="flex gap-1 justify-end">
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => { setEditando(cat); setEditNome(cat.nome) }}
                      >
                        <Pencil size={14} />
                      </Button>
                      <Button
                        size="sm"
                        variant="ghost"
                        className="text-destructive"
                        onClick={() => handleRemover(cat)}
                      >
                        <Trash2 size={14} />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Modal nova categoria */}
      {modalNova && (
        <div
          className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center"
          onClick={() => { setModalNova(false); setNovoNome('') }}
        >
          <div
            className="bg-background rounded-xl border p-6 w-full max-w-sm flex flex-col gap-4"
            onClick={e => e.stopPropagation()}
          >
            <h2 className="font-bold">Nova Categoria</h2>
            <div>
              <label className="block text-sm mb-1">Tipo</label>
              <select
                value={novoTipo}
                onChange={e => setNovoTipo(e.target.value as 'Receita' | 'Despesa')}
                className={inputClass}
              >
                <option value="Despesa">Despesa</option>
                <option value="Receita">Receita</option>
              </select>
            </div>
            <div>
              <label className="block text-sm mb-1">Nome</label>
              <input
                type="text"
                value={novoNome}
                onChange={e => setNovoNome(e.target.value)}
                className={inputClass}
                placeholder="Ex: Manutenção"
                autoFocus
              />
            </div>
            <div className="flex gap-2">
              <Button onClick={handleCriar} disabled={salvandoNova || !novoNome.trim()}>
                {salvandoNova ? 'Salvando...' : 'Salvar'}
              </Button>
              <Button variant="outline" onClick={() => { setModalNova(false); setNovoNome('') }}>
                Cancelar
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Modal editar categoria */}
      {editando && (
        <div
          className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center"
          onClick={() => setEditando(null)}
        >
          <div
            className="bg-background rounded-xl border p-6 w-full max-w-sm flex flex-col gap-4"
            onClick={e => e.stopPropagation()}
          >
            <h2 className="font-bold">Editar Categoria</h2>
            <div>
              <label className="block text-sm mb-1">Nome</label>
              <input
                type="text"
                value={editNome}
                onChange={e => setEditNome(e.target.value)}
                className={inputClass}
                autoFocus
              />
            </div>
            <div className="flex gap-2">
              <Button onClick={handleEditar} disabled={salvandoEdit || !editNome.trim()}>
                {salvandoEdit ? 'Salvando...' : 'Salvar'}
              </Button>
              <Button variant="outline" onClick={() => setEditando(null)}>Cancelar</Button>
            </div>
          </div>
        </div>
      )}

      {ConfirmDialogNode}
    </div>
  )
}
```

- [ ] **Step 2: Adicionar rota em router/index.tsx**

Adicione o import:
```tsx
import Categorias from '@/pages/financeiro/Categorias'
```

E dentro do array `children`, após `{ path: '/financeiro/receber', ... }`:
```tsx
{ path: '/financeiro/categorias', element: <Categorias /> },
```

- [ ] **Step 3: Adicionar link no Sidebar.tsx**

No array `menuGroups`, no grupo `Financeiro`, adicionar após a linha de `Contas a Receber`:

Primeiro, importar o ícone `Tag` (já importado pelo bloco de imports existente — se não estiver, adicionar ao bloco):
```tsx
Tag,
```

Depois adicionar o item no grupo Financeiro:
```tsx
{ icon: Tag, label: 'Categorias', path: '/financeiro/categorias' },
```

O bloco `Financeiro` ficará:
```tsx
  {
    label: 'Financeiro',
    items: [
      { icon: Wallet,       label: 'Lançamentos',      path: '/financeiro' },
      { icon: TrendingDown, label: 'Contas a Pagar',   path: '/financeiro/pagar' },
      { icon: TrendingUp,   label: 'Contas a Receber', path: '/financeiro/receber' },
      { icon: Tag,          label: 'Categorias',       path: '/financeiro/categorias' },
    ],
  },
```

- [ ] **Step 4: Verificar tipagem**

```bash
cd frontend
npx tsc --noEmit
```

Expected: sem erros.

- [ ] **Step 5: Rodar suite backend completa**

```bash
cd backend
dotnet test -v minimal
```

Expected: todos os testes PASS.

- [ ] **Step 6: Commit final**

```bash
git add frontend/src/pages/financeiro/Categorias.tsx \
        frontend/src/router/index.tsx \
        frontend/src/components/layout/Sidebar.tsx
git commit -m "feat: Categorias page, route and sidebar link"
```
