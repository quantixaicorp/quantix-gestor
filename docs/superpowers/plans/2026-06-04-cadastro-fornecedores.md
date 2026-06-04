# Cadastro de Fornecedores — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CRUD completo de fornecedores — lista com busca, cadastro e edição em modal, exclusão com confirmação.

**Architecture:** Segue o padrão do módulo Clientes. Backend em .NET 10 Minimal APIs com EF Core multi-tenant. Frontend React com hook + formulário Zod + página de lista. Nenhuma alteração em módulos existentes além de AppDbContext, Program.cs, router e Sidebar.

**Tech Stack:** .NET 10, EF Core + PostgreSQL, FluentValidation, React + TypeScript, React Hook Form + Zod, Tailwind CSS, Lucide React.

---

## File Map

**Criar:**
- `backend/src/GestorAI.API/Domain/Entities/Fornecedor.cs`
- `backend/src/GestorAI.API/DTOs/Fornecedores/FornecedorDto.cs`
- `backend/src/GestorAI.API/Services/Fornecedores/FornecedorService.cs`
- `backend/src/GestorAI.API/Services/Fornecedores/CreateFornecedorValidator.cs`
- `backend/src/GestorAI.API/Endpoints/FornecedoresEndpoints.cs`
- `backend/tests/GestorAI.Tests/Services/FornecedorServiceTests.cs`
- `frontend/src/types/fornecedores.ts`
- `frontend/src/hooks/useFornecedores.ts`
- `frontend/src/components/fornecedores/FornecedorForm.tsx`
- `frontend/src/pages/fornecedores/Fornecedores.tsx`

**Modificar:**
- `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`
- `backend/src/GestorAI.API/Program.cs`
- `frontend/src/router/index.tsx`
- `frontend/src/components/layout/Sidebar.tsx`

---

## Task 1: Entidade e DTOs

**Files:**
- Create: `backend/src/GestorAI.API/Domain/Entities/Fornecedor.cs`
- Create: `backend/src/GestorAI.API/DTOs/Fornecedores/FornecedorDto.cs`

- [ ] **Step 1: Criar a entidade Fornecedor**

```csharp
// backend/src/GestorAI.API/Domain/Entities/Fornecedor.cs
namespace GestorAI.API.Domain.Entities;

public class Fornecedor : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public string? CnpjCpf { get; set; }
    public string? Telefone { get; set; }
    public string? Email { get; set; }
    public string? Logradouro { get; set; }
    public string? Cidade { get; set; }
    public string? Uf { get; set; }
    public string? Cep { get; set; }
    public string? Contato { get; set; }
    public string? Observacoes { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Criar o arquivo de DTOs**

```csharp
// backend/src/GestorAI.API/DTOs/Fornecedores/FornecedorDto.cs
namespace GestorAI.API.DTOs.Fornecedores;

public record FornecedorResponse(
    Guid Id,
    string Nome,
    string? CnpjCpf,
    string? Telefone,
    string? Email,
    string? Logradouro,
    string? Cidade,
    string? Uf,
    string? Cep,
    string? Contato,
    string? Observacoes,
    DateTime DataCadastro);

public record CreateFornecedorRequest(
    string Nome,
    string? CnpjCpf,
    string? Telefone,
    string? Email,
    string? Logradouro,
    string? Cidade,
    string? Uf,
    string? Cep,
    string? Contato,
    string? Observacoes);

public record UpdateFornecedorRequest(
    string Nome,
    string? CnpjCpf,
    string? Telefone,
    string? Email,
    string? Logradouro,
    string? Cidade,
    string? Uf,
    string? Cep,
    string? Contato,
    string? Observacoes);
```

- [ ] **Step 3: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Entities/Fornecedor.cs \
        backend/src/GestorAI.API/DTOs/Fornecedores/FornecedorDto.cs
git commit -m "feat: entidade Fornecedor e DTOs"
```

---

## Task 2: AppDbContext e migração EF Core

**Files:**
- Modify: `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: Adicionar DbSet e configurações em AppDbContext**

Adicione após `public DbSet<Cobranca> Cobrancas => Set<Cobranca>();`:
```csharp
public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
```

No método `OnModelCreating`, adicione após a configuração do `ConfiguracaoEmpresa`:
```csharp
modelBuilder.Entity<Fornecedor>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
modelBuilder.Entity<Fornecedor>()
    .HasIndex(f => new { f.EmpresaId, f.CnpjCpf })
    .IsUnique()
    .HasFilter("\"CnpjCpf\" IS NOT NULL");
```

- [ ] **Step 2: Gerar a migração**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet ef migrations add AddFornecedores --project src/GestorAI.API
```

Expected: arquivo `Migrations/YYYYMMDDHHMMSS_AddFornecedores.cs` criado.

- [ ] **Step 3: Aplicar a migração ao banco**

```bash
/usr/local/share/dotnet/dotnet ef database update --project src/GestorAI.API
```

Expected: `Done.`

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs \
        backend/src/GestorAI.API/Migrations/
git commit -m "feat: DbSet Fornecedores e migração AddFornecedores"
```

---

## Task 3: Testes do FornecedorService (TDD — escrever antes da implementação)

**Files:**
- Create: `backend/tests/GestorAI.Tests/Services/FornecedorServiceTests.cs`

- [ ] **Step 1: Escrever os testes (ainda sem implementação — devem falhar)**

```csharp
// backend/tests/GestorAI.Tests/Services/FornecedorServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Fornecedores;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Fornecedores;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class FornecedorServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, FornecedorService svc) Setup()
    {
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new FornecedorService(db, tenantContext));
    }

    [Fact]
    public async Task ListAsync_RetornaApenasDoTenant()
    {
        var (db, svc) = Setup();
        var outroTenant = Guid.NewGuid();

        db.Fornecedores.AddRange(
            new Fornecedor { EmpresaId = _empresaId, Nome = "Fornecedor A" },
            new Fornecedor { EmpresaId = outroTenant, Nome = "Fornecedor Outro Tenant" }
        );
        await db.SaveChangesAsync();

        var result = await svc.ListAsync(null, default);

        Assert.Single(result);
        Assert.Equal("Fornecedor A", result[0].Nome);
    }

    [Fact]
    public async Task CreateAsync_SalvaComEmpresaId()
    {
        var (db, svc) = Setup();

        var req = new CreateFornecedorRequest(
            Nome: "Distribuidora XYZ",
            CnpjCpf: "12345678000195",
            Telefone: "11999990000",
            Email: "contato@xyz.com",
            Logradouro: "Rua das Flores, 100",
            Cidade: "São Paulo",
            Uf: "SP",
            Cep: "01310-100",
            Contato: "João Silva",
            Observacoes: null);

        var result = await svc.CreateAsync(req, default);

        Assert.NotEqual(Guid.Empty, result.Id);
        var saved = await db.Fornecedores.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == result.Id);
        Assert.NotNull(saved);
        Assert.Equal(_empresaId, saved.EmpresaId);
        Assert.Equal("Distribuidora XYZ", saved.Nome);
    }

    [Fact]
    public async Task UpdateAsync_Lanca404_QuandoNaoEncontrado()
    {
        var (_, svc) = Setup();

        var req = new UpdateFornecedorRequest(
            Nome: "Novo Nome",
            CnpjCpf: null, Telefone: null, Email: null,
            Logradouro: null, Cidade: null, Uf: null,
            Cep: null, Contato: null, Observacoes: null);

        await Assert.ThrowsAsync<AppException>(() =>
            svc.UpdateAsync(Guid.NewGuid(), req, default));
    }

    [Fact]
    public async Task DeleteAsync_RemoveFornecedor()
    {
        var (db, svc) = Setup();
        var fornecedor = new Fornecedor { EmpresaId = _empresaId, Nome = "Para Deletar" };
        db.Fornecedores.Add(fornecedor);
        await db.SaveChangesAsync();

        await svc.DeleteAsync(fornecedor.Id, default);

        var saved = await db.Fornecedores.IgnoreQueryFilters()
            .FirstOrDefaultAsync(f => f.Id == fornecedor.Id);
        Assert.Null(saved);
    }

    [Fact]
    public async Task DeleteAsync_Lanca404_QuandoNaoEncontrado()
    {
        var (_, svc) = Setup();

        await Assert.ThrowsAsync<AppException>(() =>
            svc.DeleteAsync(Guid.NewGuid(), default));
    }
}
```

- [ ] **Step 2: Rodar os testes e verificar que falham (FornecedorService ainda não existe)**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests --filter "FornecedorServiceTests" 2>&1 | tail -10
```

Expected: erro de compilação ou 5 testes falhando.

- [ ] **Step 3: Commit dos testes**

```bash
git add backend/tests/GestorAI.Tests/Services/FornecedorServiceTests.cs
git commit -m "test: testes do FornecedorService (TDD)"
```

---

## Task 4: FornecedorService e Validator

**Files:**
- Create: `backend/src/GestorAI.API/Services/Fornecedores/FornecedorService.cs`
- Create: `backend/src/GestorAI.API/Services/Fornecedores/CreateFornecedorValidator.cs`

- [ ] **Step 1: Implementar o FornecedorService**

```csharp
// backend/src/GestorAI.API/Services/Fornecedores/FornecedorService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Fornecedores;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Fornecedores;

public class FornecedorService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<FornecedorResponse>> ListAsync(string? busca, CancellationToken ct)
    {
        var query = db.Fornecedores.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(f => f.Nome.Contains(busca) ||
                (f.CnpjCpf != null && f.CnpjCpf.Contains(busca)));

        return await query
            .OrderBy(f => f.Nome)
            .Select(f => ToResponse(f))
            .ToListAsync(ct);
    }

    public async Task<FornecedorResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var f = await db.Fornecedores.FindAsync([id], ct)
            ?? throw new AppException("Fornecedor não encontrado", 404);
        return ToResponse(f);
    }

    public async Task<FornecedorResponse> CreateAsync(CreateFornecedorRequest req, CancellationToken ct)
    {
        var fornecedor = new Fornecedor
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            CnpjCpf = req.CnpjCpf,
            Telefone = req.Telefone,
            Email = req.Email,
            Logradouro = req.Logradouro,
            Cidade = req.Cidade,
            Uf = req.Uf,
            Cep = req.Cep,
            Contato = req.Contato,
            Observacoes = req.Observacoes,
        };
        db.Fornecedores.Add(fornecedor);
        await db.SaveChangesAsync(ct);
        return ToResponse(fornecedor);
    }

    public async Task<FornecedorResponse> UpdateAsync(Guid id, UpdateFornecedorRequest req, CancellationToken ct)
    {
        var fornecedor = await db.Fornecedores.FindAsync([id], ct)
            ?? throw new AppException("Fornecedor não encontrado", 404);

        fornecedor.Nome = req.Nome;
        fornecedor.CnpjCpf = req.CnpjCpf;
        fornecedor.Telefone = req.Telefone;
        fornecedor.Email = req.Email;
        fornecedor.Logradouro = req.Logradouro;
        fornecedor.Cidade = req.Cidade;
        fornecedor.Uf = req.Uf;
        fornecedor.Cep = req.Cep;
        fornecedor.Contato = req.Contato;
        fornecedor.Observacoes = req.Observacoes;

        await db.SaveChangesAsync(ct);
        return ToResponse(fornecedor);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var fornecedor = await db.Fornecedores.FindAsync([id], ct)
            ?? throw new AppException("Fornecedor não encontrado", 404);
        db.Fornecedores.Remove(fornecedor);
        await db.SaveChangesAsync(ct);
    }

    private static FornecedorResponse ToResponse(Fornecedor f) =>
        new(f.Id, f.Nome, f.CnpjCpf, f.Telefone, f.Email,
            f.Logradouro, f.Cidade, f.Uf, f.Cep,
            f.Contato, f.Observacoes, f.DataCadastro);
}
```

- [ ] **Step 2: Implementar o validator**

```csharp
// backend/src/GestorAI.API/Services/Fornecedores/CreateFornecedorValidator.cs
using FluentValidation;
using GestorAI.API.DTOs.Fornecedores;

namespace GestorAI.API.Services.Fornecedores;

public class CreateFornecedorValidator : AbstractValidator<CreateFornecedorRequest>
{
    public CreateFornecedorValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CnpjCpf)
            .Must(v => v == null || System.Text.RegularExpressions.Regex.IsMatch(v, @"^\d{11}$|^\d{14}$"))
            .WithMessage("CNPJ deve ter 14 dígitos ou CPF 11 dígitos (apenas números)")
            .When(x => !string.IsNullOrEmpty(x.CnpjCpf));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Telefone).MaximumLength(20);
        RuleFor(x => x.Uf).MaximumLength(2);
        RuleFor(x => x.Cep).MaximumLength(9);
    }
}
```

- [ ] **Step 3: Rodar os testes e verificar que passam**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests --filter "FornecedorServiceTests" -v 2>&1 | tail -15
```

Expected: `5 passed, 0 failed`

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Services/Fornecedores/
git commit -m "feat: FornecedorService e CreateFornecedorValidator"
```

---

## Task 5: Endpoints e registro em Program.cs

**Files:**
- Create: `backend/src/GestorAI.API/Endpoints/FornecedoresEndpoints.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar os endpoints**

```csharp
// backend/src/GestorAI.API/Endpoints/FornecedoresEndpoints.cs
using GestorAI.API.DTOs.Fornecedores;
using GestorAI.API.Services.Fornecedores;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class FornecedoresEndpoints
{
    public static void MapFornecedores(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fornecedores").RequireAuthorization();

        group.MapGet("/", async (string? busca, FornecedorService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(busca, ct)));

        group.MapGet("/{id:guid}", async (Guid id, FornecedorService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateFornecedorRequest req, FornecedorService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/fornecedores/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateFornecedorRequest>>();

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateFornecedorRequest req, FornecedorService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, FornecedorService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}
```

- [ ] **Step 2: Registrar serviço, validator e endpoint em Program.cs**

Em `Program.cs`, adicione após `builder.Services.AddScoped<ClienteService>();`:
```csharp
// Services — Fornecedores
builder.Services.AddScoped<FornecedorService>();
```

Adicione o using no topo do arquivo:
```csharp
using GestorAI.API.DTOs.Fornecedores;
using GestorAI.API.Services.Fornecedores;
```

Adicione após `builder.Services.AddScoped<IValidator<CreateClienteRequest>, CreateClienteValidator>();`:
```csharp
builder.Services.AddScoped<IValidator<CreateFornecedorRequest>, CreateFornecedorValidator>();
```

Adicione após `app.MapClientes();`:
```csharp
app.MapFornecedores();
```

- [ ] **Step 3: Compilar e verificar que sobe sem erros**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet build src/GestorAI.API 2>&1 | tail -5
```

Expected: `Build succeeded.`

- [ ] **Step 4: Rodar todos os testes do backend**

```bash
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests 2>&1 | tail -5
```

Expected: todos os testes passando (incluindo os 5 novos).

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Endpoints/FornecedoresEndpoints.cs \
        backend/src/GestorAI.API/Program.cs
git commit -m "feat: endpoints /api/fornecedores e registro no Program.cs"
```

---

## Task 6: Frontend — tipos e hook

**Files:**
- Create: `frontend/src/types/fornecedores.ts`
- Create: `frontend/src/hooks/useFornecedores.ts`

- [ ] **Step 1: Criar os tipos TypeScript**

```typescript
// frontend/src/types/fornecedores.ts
export interface FornecedorResponse {
  id: string
  nome: string
  cnpjCpf: string | null
  telefone: string | null
  email: string | null
  logradouro: string | null
  cidade: string | null
  uf: string | null
  cep: string | null
  contato: string | null
  observacoes: string | null
  dataCadastro: string
}

export interface CreateFornecedorRequest {
  nome: string
  cnpjCpf?: string
  telefone?: string
  email?: string
  logradouro?: string
  cidade?: string
  uf?: string
  cep?: string
  contato?: string
  observacoes?: string
}

export interface UpdateFornecedorRequest {
  nome: string
  cnpjCpf?: string
  telefone?: string
  email?: string
  logradouro?: string
  cidade?: string
  uf?: string
  cep?: string
  contato?: string
  observacoes?: string
}
```

- [ ] **Step 2: Criar o hook useFornecedores**

```typescript
// frontend/src/hooks/useFornecedores.ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { FornecedorResponse, CreateFornecedorRequest, UpdateFornecedorRequest } from '@/types/fornecedores'

export function useFornecedores() {
  const [fornecedores, setFornecedores] = useState<FornecedorResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (busca?: string) => {
    setLoading(true)
    try {
      const qs = busca ? `?busca=${encodeURIComponent(busca)}` : ''
      const data = await api.get<FornecedorResponse[]>(`/api/fornecedores${qs}`)
      setFornecedores(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar fornecedores')
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CreateFornecedorRequest) => {
    const result = await api.post<FornecedorResponse>('/api/fornecedores', req)
    setFornecedores(prev => [...prev, result].sort((a, b) => a.nome.localeCompare(b.nome)))
    return result
  }, [])

  const update = useCallback(async (id: string, req: UpdateFornecedorRequest) => {
    const result = await api.put<FornecedorResponse>(`/api/fornecedores/${id}`, req)
    setFornecedores(prev => prev.map(f => f.id === id ? result : f))
    return result
  }, [])

  const remove = useCallback(async (id: string) => {
    await api.delete(`/api/fornecedores/${id}`)
    setFornecedores(prev => prev.filter(f => f.id !== id))
  }, [])

  return { fornecedores, loading, error, list, create, update, remove }
}
```

Nota: `api.delete` precisa existir no serviço `api`. Verifique em `frontend/src/services/api.ts` se o método `delete` existe. Se não existir, adicione:
```typescript
delete: async (path: string) => {
  const res = await fetch(`${BASE_URL}${path}`, {
    method: 'DELETE',
    headers: authHeaders(),
  })
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }))
    throw new Error((err as { error?: string }).error ?? res.statusText)
  }
},
```

- [ ] **Step 3: Verificar tipos sem erros**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
PATH="/opt/homebrew/bin:$PATH" node_modules/.bin/tsc --noEmit 2>&1 | head -20
```

Expected: sem output (sem erros).

- [ ] **Step 4: Commit**

```bash
git add frontend/src/types/fornecedores.ts frontend/src/hooks/useFornecedores.ts
git commit -m "feat: tipos e hook useFornecedores"
```

---

## Task 7: Frontend — formulário, página, rota e sidebar

**Files:**
- Create: `frontend/src/components/fornecedores/FornecedorForm.tsx`
- Create: `frontend/src/pages/fornecedores/Fornecedores.tsx`
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Criar o formulário FornecedorForm**

```tsx
// frontend/src/components/fornecedores/FornecedorForm.tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CreateFornecedorRequest } from '@/types/fornecedores'

const schema = z.object({
  nome: z.string().min(1, 'Nome obrigatório').max(200),
  cnpjCpf: z.string()
    .refine(v => !v || /^\d{11}$|^\d{14}$/.test(v), 'Informe 11 dígitos (CPF) ou 14 dígitos (CNPJ)')
    .optional()
    .or(z.literal('')),
  telefone: z.string().max(20).optional().or(z.literal('')),
  email: z.string().email('E-mail inválido').optional().or(z.literal('')),
  contato: z.string().max(200).optional().or(z.literal('')),
  logradouro: z.string().max(300).optional().or(z.literal('')),
  cidade: z.string().max(100).optional().or(z.literal('')),
  uf: z.string().max(2).optional().or(z.literal('')),
  cep: z.string().max(9).optional().or(z.literal('')),
  observacoes: z.string().optional().or(z.literal('')),
})
type FormValues = z.infer<typeof schema>

interface Props {
  defaultValues?: Partial<FormValues>
  onSubmit: (data: CreateFornecedorRequest) => Promise<void>
  onCancel: () => void
}

export default function FornecedorForm({ defaultValues, onSubmit, onCancel }: Props) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({ resolver: zodResolver(schema), defaultValues })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Nome *</Label>
        <Input {...register('nome')} placeholder="Razão social ou nome" />
        {errors.nome && <p className="text-xs text-destructive">{errors.nome.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="grid gap-2">
          <Label>CNPJ / CPF</Label>
          <Input {...register('cnpjCpf')} placeholder="Apenas números" />
          {errors.cnpjCpf && <p className="text-xs text-destructive">{errors.cnpjCpf.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Telefone</Label>
          <Input {...register('telefone')} placeholder="11999990000" />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div className="grid gap-2">
          <Label>E-mail</Label>
          <Input {...register('email')} type="email" placeholder="contato@empresa.com" />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Contato</Label>
          <Input {...register('contato')} placeholder="Nome do responsável" />
        </div>
      </div>

      <div className="border-t pt-3">
        <p className="text-xs font-medium text-muted-foreground mb-2 uppercase tracking-wide">Endereço</p>
        <div className="grid gap-3">
          <div className="grid gap-2">
            <Label>Logradouro</Label>
            <Input {...register('logradouro')} placeholder="Rua, número, complemento" />
          </div>
          <div className="grid grid-cols-3 gap-3">
            <div className="col-span-2 grid gap-2">
              <Label>Cidade</Label>
              <Input {...register('cidade')} placeholder="São Paulo" />
            </div>
            <div className="grid gap-2">
              <Label>UF</Label>
              <Input {...register('uf')} placeholder="SP" maxLength={2} />
            </div>
          </div>
          <div className="grid gap-2 max-w-[160px]">
            <Label>CEP</Label>
            <Input {...register('cep')} placeholder="01310-100" />
          </div>
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Observações</Label>
        <Input {...register('observacoes')} placeholder="Anotações internas" />
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

- [ ] **Step 2: Criar a página Fornecedores**

```tsx
// frontend/src/pages/fornecedores/Fornecedores.tsx
import { useEffect, useState } from 'react'
import { Plus, Search, Pencil, Trash2 } from 'lucide-react'
import { useFornecedores } from '@/hooks/useFornecedores'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import FornecedorForm from '@/components/fornecedores/FornecedorForm'
import { toast } from '@/hooks/useToast'
import type { FornecedorResponse, CreateFornecedorRequest } from '@/types/fornecedores'

export default function Fornecedores() {
  const { fornecedores, loading, list, create, update, remove } = useFornecedores()
  const [busca, setBusca] = useState('')
  const [modalAberto, setModalAberto] = useState(false)
  const [editando, setEditando] = useState<FornecedorResponse | null>(null)
  const [confirmandoDelete, setConfirmandoDelete] = useState<FornecedorResponse | null>(null)

  useEffect(() => { void list() }, [list])

  async function handleCreate(data: CreateFornecedorRequest) {
    try {
      await create(data)
      setModalAberto(false)
      toast.success('Fornecedor cadastrado!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    }
  }

  async function handleUpdate(data: CreateFornecedorRequest) {
    if (!editando) return
    try {
      await update(editando.id, data)
      setEditando(null)
      toast.success('Fornecedor atualizado!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao salvar')
    }
  }

  async function handleDelete() {
    if (!confirmandoDelete) return
    try {
      await remove(confirmandoDelete.id)
      setConfirmandoDelete(null)
      toast.success('Fornecedor removido!')
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao remover')
    }
  }

  const filtrados = fornecedores.filter(f =>
    f.nome.toLowerCase().includes(busca.toLowerCase()) ||
    (f.cnpjCpf ?? '').includes(busca))

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Fornecedores</h1>
        <Button onClick={() => setModalAberto(true)}>
          <Plus size={16} className="mr-2" /> Novo Fornecedor
        </Button>
      </div>

      <div className="relative max-w-sm">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Buscar por nome ou CNPJ/CPF..."
          value={busca}
          onChange={e => setBusca(e.target.value)}
        />
      </div>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Nome</th>
                <th className="px-4 py-3 text-left font-medium">CNPJ / CPF</th>
                <th className="px-4 py-3 text-left font-medium">Telefone</th>
                <th className="px-4 py-3 text-left font-medium">Cidade / UF</th>
                <th className="px-4 py-3 text-left font-medium">Contato</th>
                <th className="px-4 py-3 text-left font-medium w-24">Ações</th>
              </tr>
            </thead>
            <tbody>
              {filtrados.map(f => (
                <tr key={f.id} className="border-b hover:bg-muted/30">
                  <td className="px-4 py-3 font-medium">{f.nome}</td>
                  <td className="px-4 py-3 text-muted-foreground">{f.cnpjCpf ?? '—'}</td>
                  <td className="px-4 py-3 text-muted-foreground">{f.telefone ?? '—'}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {f.cidade && f.uf ? `${f.cidade} / ${f.uf}` : f.cidade ?? '—'}
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{f.contato ?? '—'}</td>
                  <td className="px-4 py-3">
                    <div className="flex gap-1">
                      <Button size="icon" variant="ghost" onClick={() => setEditando(f)}>
                        <Pencil size={14} />
                      </Button>
                      <Button size="icon" variant="ghost" className="text-destructive hover:text-destructive" onClick={() => setConfirmandoDelete(f)}>
                        <Trash2 size={14} />
                      </Button>
                    </div>
                  </td>
                </tr>
              ))}
              {filtrados.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum fornecedor cadastrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Modal: novo fornecedor */}
      <Dialog open={modalAberto} onOpenChange={setModalAberto}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader><DialogTitle>Novo Fornecedor</DialogTitle></DialogHeader>
          <FornecedorForm onSubmit={handleCreate} onCancel={() => setModalAberto(false)} />
        </DialogContent>
      </Dialog>

      {/* Modal: editar fornecedor */}
      <Dialog open={!!editando} onOpenChange={open => { if (!open) setEditando(null) }}>
        <DialogContent className="max-w-lg max-h-[90vh] overflow-y-auto">
          <DialogHeader><DialogTitle>Editar Fornecedor</DialogTitle></DialogHeader>
          {editando && (
            <FornecedorForm
              defaultValues={{
                nome: editando.nome,
                cnpjCpf: editando.cnpjCpf ?? '',
                telefone: editando.telefone ?? '',
                email: editando.email ?? '',
                contato: editando.contato ?? '',
                logradouro: editando.logradouro ?? '',
                cidade: editando.cidade ?? '',
                uf: editando.uf ?? '',
                cep: editando.cep ?? '',
                observacoes: editando.observacoes ?? '',
              }}
              onSubmit={handleUpdate}
              onCancel={() => setEditando(null)}
            />
          )}
        </DialogContent>
      </Dialog>

      {/* Modal: confirmar exclusão */}
      <Dialog open={!!confirmandoDelete} onOpenChange={open => { if (!open) setConfirmandoDelete(null) }}>
        <DialogContent className="max-w-sm">
          <DialogHeader><DialogTitle>Excluir fornecedor?</DialogTitle></DialogHeader>
          <p className="text-sm text-muted-foreground">
            Tem certeza que deseja excluir <strong>{confirmandoDelete?.nome}</strong>? Esta ação não pode ser desfeita.
          </p>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="outline" onClick={() => setConfirmandoDelete(null)}>Cancelar</Button>
            <Button variant="destructive" onClick={() => void handleDelete()}>Excluir</Button>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  )
}
```

- [ ] **Step 3: Registrar a rota em router/index.tsx**

Adicione o import no topo (junto com os demais imports de páginas):
```typescript
import Fornecedores from '@/pages/fornecedores/Fornecedores'
```

Adicione a rota dentro do array `children` de `<AppLayout>`, após a rota de `/clientes`:
```typescript
{ path: '/fornecedores', element: <Fornecedores /> },
```

- [ ] **Step 4: Adicionar item no Sidebar**

No array `menuGroups` em `Sidebar.tsx`, adicione `Truck` nos imports do lucide:
```typescript
import {
  // ... imports existentes ...
  Truck,
} from 'lucide-react'
```

Adicione um novo grupo após o grupo `'Clientes / Relatórios'`:
```typescript
{
  label: 'Compras',
  items: [
    { icon: Truck, label: 'Fornecedores', path: '/fornecedores' },
  ],
},
```

- [ ] **Step 5: Verificar TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
PATH="/opt/homebrew/bin:$PATH" node_modules/.bin/tsc --noEmit 2>&1 | head -20
```

Expected: sem output.

- [ ] **Step 6: Verificar se o serviço api.ts tem o método delete**

```bash
grep -n "delete" /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/services/api.ts
```

Se não tiver, abra o arquivo e adicione o método `delete` conforme descrito na Task 6, Step 2.

- [ ] **Step 7: Commit final**

```bash
git add frontend/src/components/fornecedores/FornecedorForm.tsx \
        frontend/src/pages/fornecedores/Fornecedores.tsx \
        frontend/src/router/index.tsx \
        frontend/src/components/layout/Sidebar.tsx
git commit -m "feat: página Fornecedores com lista, cadastro, edição e exclusão"
```
