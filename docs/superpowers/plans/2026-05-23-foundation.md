# GestorAI — Plano 1/5: Foundation

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Scaffold completo do GestorAI — backend .NET 10 com auth JWT, multi-tenancy e PostgreSQL; frontend React 18 com login OAuth2 PKCE, api.ts e layout base funcionando.

**Architecture:** .NET 10 Minimal API com EF Core + Npgsql, TenantMiddleware extrai `empresa_id` do JWT e popula TenantContext (scoped), AppDbContext aplica `HasQueryFilter` automático por `EmpresaId`. Frontend Vite + React 18 com AuthContext gerenciando PKCE flow contra Quantix Admin.

**Tech Stack:** .NET 10, EF Core 9, Npgsql, FluentValidation 11, JwtBearer | React 18, TypeScript, Vite 6, shadcn/ui, Tailwind CSS 3, React Router DOM v6, Vitest 2

---

## Mapa de arquivos

### Backend — `gestorai-erp/backend/`

| Arquivo | Responsabilidade |
|---|---|
| `GestorAI.sln` | Solution |
| `src/GestorAI.API/GestorAI.API.csproj` | Projeto principal |
| `src/GestorAI.API/Program.cs` | Wiring de DI, middleware, endpoints |
| `src/GestorAI.API/appsettings.json` | Config JWT, DB, CORS |
| `src/GestorAI.API/Domain/Entities/ITenantEntity.cs` | Interface tenant |
| `src/GestorAI.API/Domain/Entities/Categoria.cs` | Entidade Categoria |
| `src/GestorAI.API/Domain/Entities/Produto.cs` | Entidade Produto |
| `src/GestorAI.API/Domain/Entities/MovimentacaoEstoque.cs` | Entidade MovimentacaoEstoque |
| `src/GestorAI.API/Domain/Entities/Cliente.cs` | Entidade Cliente |
| `src/GestorAI.API/Domain/Entities/Venda.cs` | Entidade Venda |
| `src/GestorAI.API/Domain/Entities/ItemVenda.cs` | Entidade ItemVenda |
| `src/GestorAI.API/Domain/Entities/Lancamento.cs` | Entidade Lancamento |
| `src/GestorAI.API/Domain/Enums/TipoLancamento.cs` | enum Receita/Despesa |
| `src/GestorAI.API/Domain/Enums/StatusVenda.cs` | enum Aberta/Concluida/Cancelada |
| `src/GestorAI.API/Domain/Enums/FormaPagamento.cs` | enum Dinheiro/Pix/Cartao/Outro |
| `src/GestorAI.API/Domain/Enums/TipoMovimentacao.cs` | enum Entrada/Saida/Ajuste |
| `src/GestorAI.API/Domain/Enums/OrigemMovimentacao.cs` | enum Venda/Compra/Manual |
| `src/GestorAI.API/Domain/Enums/StatusLancamento.cs` | enum Pendente/Pago/Cancelado |
| `src/GestorAI.API/Infrastructure/Data/AppDbContext.cs` | EF Core context + QueryFilters |
| `src/GestorAI.API/Infrastructure/Repositories/Repository.cs` | Repositório genérico |
| `src/GestorAI.API/Shared/MultiTenancy/TenantContext.cs` | Scoped: armazena EmpresaId |
| `src/GestorAI.API/Shared/MultiTenancy/TenantMiddleware.cs` | JWT claim → TenantContext |
| `src/GestorAI.API/Shared/Exceptions/AppException.cs` | Exceção de negócio |
| `src/GestorAI.API/Shared/Exceptions/ExceptionMiddleware.cs` | Handler global de erros |
| `src/GestorAI.API/Shared/Filters/ValidationFilter.cs` | FluentValidation filter |
| `tests/GestorAI.Tests/GestorAI.Tests.csproj` | Projeto de testes |
| `tests/GestorAI.Tests/MultiTenancy/TenantMiddlewareTests.cs` | Testes do TenantMiddleware |
| `tests/GestorAI.Tests/Data/AppDbContextTests.cs` | Testes dos QueryFilters |

### Frontend — `gestorai-erp/frontend/`

| Arquivo | Responsabilidade |
|---|---|
| `package.json` | Dependências |
| `vite.config.ts` | Config Vite + Vitest |
| `tailwind.config.ts` | Config Tailwind |
| `postcss.config.js` | PostCSS |
| `src/main.tsx` | Entry point |
| `src/App.tsx` | Router raiz |
| `src/contexts/AuthContext.tsx` | OAuth2 PKCE + estado de auth global |
| `src/services/api.ts` | HTTP client com Bearer JWT + silent refresh |
| `src/pages/Auth.tsx` | Redirect para Quantix Admin |
| `src/pages/AuthCallback.tsx` | Troca code por tokens |
| `src/pages/Dashboard.tsx` | Stub (implementado no Plano 5) |
| `src/components/layout/Sidebar.tsx` | Navegação lateral |
| `src/components/layout/AppLayout.tsx` | Shell com Sidebar |
| `src/router/index.tsx` | Definição de rotas com guard |
| `src/lib/utils.ts` | cn() helper (shadcn padrão) |
| `src/test/setup.ts` | Setup Vitest + jsdom |
| `src/test/api.test.ts` | Testes do api.ts |

### Raiz do projeto

| Arquivo | Responsabilidade |
|---|---|
| `gestorai-erp/docker-compose.yml` | API + PostgreSQL |
| `gestorai-erp/backend/Dockerfile` | Build da API |
| `gestorai-erp/.gitignore` | Ignores padrão |

---

## Task 1: Scaffold do backend

**Files:**
- Create: `gestorai-erp/backend/GestorAI.sln`
- Create: `gestorai-erp/backend/src/GestorAI.API/GestorAI.API.csproj`
- Create: `gestorai-erp/backend/tests/GestorAI.Tests/GestorAI.Tests.csproj`

- [ ] **Step 1: Criar estrutura e solution**

```bash
cd /Users/brunomedeiros/Repositorios/ERP
mkdir -p gestorai-erp/backend gestorai-erp/frontend
cd gestorai-erp/backend
dotnet new sln -n GestorAI
dotnet new web -n GestorAI.API -o src/GestorAI.API --framework net10.0
dotnet new xunit -n GestorAI.Tests -o tests/GestorAI.Tests --framework net10.0
dotnet sln add src/GestorAI.API/GestorAI.API.csproj
dotnet sln add tests/GestorAI.Tests/GestorAI.Tests.csproj
```

- [ ] **Step 2: Adicionar pacotes NuGet ao projeto principal**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet add src/GestorAI.API package Npgsql.EntityFrameworkCore.PostgreSQL --version 9.*
dotnet add src/GestorAI.API package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/GestorAI.API package FluentValidation --version 11.*
dotnet add src/GestorAI.API package Microsoft.EntityFrameworkCore.Design --version 9.*
```

- [ ] **Step 3: Adicionar pacotes ao projeto de testes**

```bash
dotnet add tests/GestorAI.Tests package Microsoft.EntityFrameworkCore.InMemory --version 9.*
dotnet add tests/GestorAI.Tests package FluentAssertions --version 6.*
dotnet add tests/GestorAI.Tests reference src/GestorAI.API/GestorAI.API.csproj
```

- [ ] **Step 4: Verificar build**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet build
```

Expected: `Build succeeded. 0 Error(s).`

---

## Task 2: Enums de domínio

**Files:**
- Create: `gestorai-erp/backend/src/GestorAI.API/Domain/Enums/*.cs` (6 arquivos)

- [ ] **Step 1: Criar diretório e os 6 enums**

```bash
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend/src/GestorAI.API/Domain/Enums
```

`src/GestorAI.API/Domain/Enums/TipoLancamento.cs`:
```csharp
namespace GestorAI.API.Domain.Enums;
public enum TipoLancamento { Receita, Despesa }
```

`src/GestorAI.API/Domain/Enums/StatusVenda.cs`:
```csharp
namespace GestorAI.API.Domain.Enums;
public enum StatusVenda { Aberta, Concluida, Cancelada }
```

`src/GestorAI.API/Domain/Enums/FormaPagamento.cs`:
```csharp
namespace GestorAI.API.Domain.Enums;
public enum FormaPagamento { Dinheiro, Pix, Cartao, Outro }
```

`src/GestorAI.API/Domain/Enums/TipoMovimentacao.cs`:
```csharp
namespace GestorAI.API.Domain.Enums;
public enum TipoMovimentacao { Entrada, Saida, Ajuste }
```

`src/GestorAI.API/Domain/Enums/OrigemMovimentacao.cs`:
```csharp
namespace GestorAI.API.Domain.Enums;
public enum OrigemMovimentacao { Venda, Compra, Manual }
```

`src/GestorAI.API/Domain/Enums/StatusLancamento.cs`:
```csharp
namespace GestorAI.API.Domain.Enums;
public enum StatusLancamento { Pendente, Pago, Cancelado }
```

- [ ] **Step 2: Build**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend && dotnet build
```

Expected: `Build succeeded.`

---

## Task 3: Entidades de domínio

**Files:**
- Create: `gestorai-erp/backend/src/GestorAI.API/Domain/Entities/*.cs` (8 arquivos)

- [ ] **Step 1: Criar diretório**

```bash
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend/src/GestorAI.API/Domain/Entities
```

- [ ] **Step 2: Criar ITenantEntity.cs**

`src/GestorAI.API/Domain/Entities/ITenantEntity.cs`:
```csharp
namespace GestorAI.API.Domain.Entities;
public interface ITenantEntity { Guid EmpresaId { get; set; } }
```

- [ ] **Step 3: Criar Categoria.cs**

`src/GestorAI.API/Domain/Entities/Categoria.cs`:
```csharp
namespace GestorAI.API.Domain.Entities;

public class Categoria : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public ICollection<Produto> Produtos { get; set; } = [];
}
```

- [ ] **Step 4: Criar Produto.cs**

`src/GestorAI.API/Domain/Entities/Produto.cs`:
```csharp
namespace GestorAI.API.Domain.Entities;

public class Produto : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid CategoriaId { get; set; }
    public required string Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal CustoMedio { get; set; }
    public decimal EstoqueAtual { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public string? CodigoBarras { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public Categoria? Categoria { get; set; }
    public ICollection<ItemVenda> ItensVenda { get; set; } = [];
    public ICollection<MovimentacaoEstoque> Movimentacoes { get; set; } = [];
}
```

- [ ] **Step 5: Criar MovimentacaoEstoque.cs**

`src/GestorAI.API/Domain/Entities/MovimentacaoEstoque.cs`:
```csharp
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class MovimentacaoEstoque : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ProdutoId { get; set; }
    public TipoMovimentacao Tipo { get; set; }
    public decimal Quantidade { get; set; }
    public OrigemMovimentacao Origem { get; set; }
    public Guid? ReferenciaId { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    public string? Observacao { get; set; }
    public Produto? Produto { get; set; }
}
```

- [ ] **Step 6: Criar Cliente.cs**

`src/GestorAI.API/Domain/Entities/Cliente.cs`:
```csharp
namespace GestorAI.API.Domain.Entities;

public class Cliente : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public required string Whatsapp { get; set; }
    public string? Email { get; set; }
    public string? Observacoes { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    public ICollection<Venda> Vendas { get; set; } = [];
}
```

- [ ] **Step 7: Criar Venda.cs**

`src/GestorAI.API/Domain/Entities/Venda.cs`:
```csharp
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Venda : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? ClienteId { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    public StatusVenda Status { get; set; } = StatusVenda.Aberta;
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public int? Parcelas { get; set; }
    public string? Observacao { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<ItemVenda> Itens { get; set; } = [];
    public Lancamento? Lancamento { get; set; }
}
```

- [ ] **Step 8: Criar ItemVenda.cs**

`src/GestorAI.API/Domain/Entities/ItemVenda.cs`:
```csharp
namespace GestorAI.API.Domain.Entities;

public class ItemVenda
{
    public Guid Id { get; set; }
    public Guid VendaId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public Venda? Venda { get; set; }
    public Produto? Produto { get; set; }
}
```

- [ ] **Step 9: Criar Lancamento.cs**

`src/GestorAI.API/Domain/Entities/Lancamento.cs`:
```csharp
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Lancamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public TipoLancamento Tipo { get; set; }
    public required string Descricao { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public StatusLancamento Status { get; set; } = StatusLancamento.Pendente;
    public required string Categoria { get; set; }
    public Guid? VendaId { get; set; }
    public string? Observacao { get; set; }
    public Venda? Venda { get; set; }
}
```

- [ ] **Step 10: Build**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend && dotnet build
```

Expected: `Build succeeded.`

---

## Task 4: Multi-tenancy

**Files:**
- Create: `gestorai-erp/backend/src/GestorAI.API/Shared/MultiTenancy/TenantContext.cs`
- Create: `gestorai-erp/backend/src/GestorAI.API/Shared/MultiTenancy/TenantMiddleware.cs`
- Test: `gestorai-erp/backend/tests/GestorAI.Tests/MultiTenancy/TenantMiddlewareTests.cs`

- [ ] **Step 1: Escrever os testes (TDD — red)**

`tests/GestorAI.Tests/MultiTenancy/TenantMiddlewareTests.cs`:
```csharp
using System.Security.Claims;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace GestorAI.Tests.MultiTenancy;

public class TenantMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithValidClaim_SetsTenantContext()
    {
        var empresaId = Guid.NewGuid();
        var tenantContext = new TenantContext();
        var middleware = new TenantMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("empresa_id", empresaId.ToString())]));

        await middleware.InvokeAsync(context, tenantContext);

        Assert.Equal(empresaId, tenantContext.EmpresaId);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingClaim_LeavesEmpresaIdEmpty()
    {
        var tenantContext = new TenantContext();
        var middleware = new TenantMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        await middleware.InvokeAsync(context, tenantContext);

        Assert.Equal(Guid.Empty, tenantContext.EmpresaId);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidGuid_LeavesEmpresaIdEmpty()
    {
        var tenantContext = new TenantContext();
        var middleware = new TenantMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("empresa_id", "nao-e-guid")]));

        await middleware.InvokeAsync(context, tenantContext);

        Assert.Equal(Guid.Empty, tenantContext.EmpresaId);
    }
}
```

- [ ] **Step 2: Rodar testes — confirmar que falham (compilação)**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test tests/GestorAI.Tests
```

Expected: Erro de compilação — `TenantContext` e `TenantMiddleware` não existem.

- [ ] **Step 3: Criar TenantContext.cs**

```bash
mkdir -p src/GestorAI.API/Shared/MultiTenancy
```

`src/GestorAI.API/Shared/MultiTenancy/TenantContext.cs`:
```csharp
namespace GestorAI.API.Shared.MultiTenancy;

public class TenantContext
{
    public Guid EmpresaId { get; set; }
}
```

- [ ] **Step 4: Criar TenantMiddleware.cs**

`src/GestorAI.API/Shared/MultiTenancy/TenantMiddleware.cs`:
```csharp
namespace GestorAI.API.Shared.MultiTenancy;

public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var claim = context.User.FindFirst("empresa_id")?.Value;
        if (Guid.TryParse(claim, out var id))
            tenantContext.EmpresaId = id;
        await next(context);
    }
}
```

- [ ] **Step 5: Rodar testes — confirmar que passam**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test tests/GestorAI.Tests
```

Expected: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 6: Commit**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add backend/
git commit -m "feat: domain entities, enums e multi-tenancy"
```

---

## Task 5: AppDbContext e QueryFilters

**Files:**
- Create: `gestorai-erp/backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`
- Test: `gestorai-erp/backend/tests/GestorAI.Tests/Data/AppDbContextTests.cs`

- [ ] **Step 1: Escrever testes (TDD — red)**

`tests/GestorAI.Tests/Data/AppDbContextTests.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Data;

public class AppDbContextTests
{
    private static AppDbContext CreateContext(Guid empresaId) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new TenantContext { EmpresaId = empresaId });

    [Fact]
    public async Task Clientes_QueryFilter_ExcludesOtherTenantData()
    {
        var empresaId = Guid.NewGuid();
        var outroId = Guid.NewGuid();
        using var ctx = CreateContext(empresaId);

        ctx.Clientes.Add(new Cliente { EmpresaId = empresaId, Nome = "Ana", Whatsapp = "11999990001" });
        ctx.Clientes.Add(new Cliente { EmpresaId = outroId, Nome = "Bob", Whatsapp = "11999990002" });
        await ctx.SaveChangesAsync();

        var result = await ctx.Clientes.ToListAsync();

        Assert.Single(result);
        Assert.Equal("Ana", result[0].Nome);
    }

    [Fact]
    public async Task Produtos_QueryFilter_ReturnsOnlyCurrentTenant()
    {
        var empresaId = Guid.NewGuid();
        var outroId = Guid.NewGuid();
        using var ctx = CreateContext(empresaId);

        ctx.Categorias.Add(new Categoria { EmpresaId = empresaId, Nome = "Cat A" });
        ctx.Categorias.Add(new Categoria { EmpresaId = outroId, Nome = "Cat B" });
        await ctx.SaveChangesAsync();

        var result = await ctx.Categorias.ToListAsync();

        Assert.Single(result);
        Assert.Equal("Cat A", result[0].Nome);
    }
}
```

- [ ] **Step 2: Rodar — confirmar que falham**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test tests/GestorAI.Tests
```

Expected: Erro de compilação — `AppDbContext` não existe.

- [ ] **Step 3: Criar AppDbContext.cs**

```bash
mkdir -p src/GestorAI.API/Infrastructure/Data
```

`src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`:
```csharp
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
    }
}
```

- [ ] **Step 4: Criar Repository.cs**

```bash
mkdir -p src/GestorAI.API/Infrastructure/Repositories
```

`src/GestorAI.API/Infrastructure/Repositories/Repository.cs`:
```csharp
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Infrastructure.Repositories;

public class Repository<T>(AppDbContext db) where T : class
{
    protected readonly AppDbContext Db = db;
    protected readonly DbSet<T> Set = db.Set<T>();

    public IQueryable<T> Query() => Set.AsQueryable();
    public async Task<T?> FindAsync(Guid id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct);
    public void Add(T entity) => Set.Add(entity);
    public void Remove(T entity) => Set.Remove(entity);
    public async Task SaveAsync(CancellationToken ct = default) =>
        await Db.SaveChangesAsync(ct);
}
```

- [ ] **Step 5: Rodar testes — confirmar que passam**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test tests/GestorAI.Tests
```

Expected: `Passed! - Failed: 0, Passed: 5`

---

## Task 6: Shared infrastructure (Exceptions + ValidationFilter)

**Files:**
- Create: `gestorai-erp/backend/src/GestorAI.API/Shared/Exceptions/AppException.cs`
- Create: `gestorai-erp/backend/src/GestorAI.API/Shared/Exceptions/ExceptionMiddleware.cs`
- Create: `gestorai-erp/backend/src/GestorAI.API/Shared/Filters/ValidationFilter.cs`

- [ ] **Step 1: Criar AppException.cs**

```bash
mkdir -p src/GestorAI.API/Shared/Exceptions src/GestorAI.API/Shared/Filters
```

`src/GestorAI.API/Shared/Exceptions/AppException.cs`:
```csharp
namespace GestorAI.API.Shared.Exceptions;

public class AppException(string message, int statusCode = 400) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
```

- [ ] **Step 2: Criar ExceptionMiddleware.cs**

`src/GestorAI.API/Shared/Exceptions/ExceptionMiddleware.cs`:
```csharp
using System.Text.Json;

namespace GestorAI.API.Shared.Exceptions;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = ex.Message }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { error = "Erro interno do servidor" }));
        }
    }
}
```

- [ ] **Step 3: Criar ValidationFilter.cs**

`src/GestorAI.API/Shared/Filters/ValidationFilter.cs`:
```csharp
using FluentValidation;

namespace GestorAI.API.Shared.Filters;

public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null) return await next(context);

        var result = await validator.ValidateAsync(argument);
        if (!result.IsValid)
            return Results.ValidationProblem(result.ToDictionary());

        return await next(context);
    }
}
```

- [ ] **Step 4: Build**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend && dotnet build
```

Expected: `Build succeeded.`

---

## Task 7: Program.cs e appsettings.json

**Files:**
- Create: `gestorai-erp/backend/src/GestorAI.API/appsettings.json`
- Modify: `gestorai-erp/backend/src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar appsettings.json**

`src/GestorAI.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5433;Database=gestorai;Username=gestorai;Password=gestorai"
  },
  "Jwt": {
    "Authority": "http://localhost:5001",
    "Audience": "gestorai"
  },
  "AllowedOrigins": ["http://localhost:5174"],
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

- [ ] **Step 2: Substituir Program.cs pelo wiring completo**

`src/GestorAI.API/Program.cs`:
```csharp
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<TenantContext>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.Authority = builder.Configuration["Jwt:Authority"];
        opt.Audience = builder.Configuration["Jwt:Audience"];
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
        };
    });

builder.Services.AddAuthorization(opt =>
{
    opt.AddPolicy("AdminOnly", p => p.RequireRole("admin"));
    opt.AddPolicy("FinanceAccess", p => p.RequireRole("admin", "financeiro"));
    opt.AddPolicy("EstoqueAccess", p => p.RequireRole("admin", "estoque"));
    opt.AddPolicy("VendasAccess", p => p.RequireRole("admin", "vendas", "estoque"));
});

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors();
app.UseAuthentication();
app.UseMiddleware<TenantMiddleware>();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
   .AllowAnonymous();

app.Run();

public partial class Program { }
```

- [ ] **Step 3: Build final do backend**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend && dotnet build
```

Expected: `Build succeeded.`

- [ ] **Step 4: Rodar todos os testes**

```bash
dotnet test tests/GestorAI.Tests
```

Expected: `Passed! - Failed: 0, Passed: 5`

- [ ] **Step 5: Commit**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add backend/
git commit -m "feat: infrastructure completa — DbContext, multi-tenancy, auth, exceptions"
```

---

## Task 8: Migração EF Core e Docker

**Files:**
- Create: `gestorai-erp/backend/Dockerfile`
- Create: `gestorai-erp/docker-compose.yml`

- [ ] **Step 1: Criar Dockerfile**

`backend/Dockerfile`:
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY GestorAI.sln .
COPY src/GestorAI.API/GestorAI.API.csproj src/GestorAI.API/
RUN dotnet restore
COPY . .
RUN dotnet publish src/GestorAI.API -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT ["dotnet", "GestorAI.API.dll"]
```

- [ ] **Step 2: Criar docker-compose.yml**

`gestorai-erp/docker-compose.yml`:
```yaml
services:
  gestorai-api:
    build: ./backend
    ports:
      - "5002:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Default=Host=gestorai-db;Port=5432;Database=gestorai;Username=gestorai;Password=gestorai
      - Jwt__Authority=http://host.docker.internal:5001
      - Jwt__Audience=gestorai
      - AllowedOrigins__0=http://localhost:5174
    depends_on:
      gestorai-db:
        condition: service_healthy

  gestorai-db:
    image: postgres:16-alpine
    ports:
      - "5433:5432"
    environment:
      POSTGRES_DB: gestorai
      POSTGRES_USER: gestorai
      POSTGRES_PASSWORD: gestorai
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U gestorai"]
      interval: 5s
      timeout: 5s
      retries: 5
    volumes:
      - gestorai_pgdata:/var/lib/postgresql/data

volumes:
  gestorai_pgdata:
```

- [ ] **Step 3: Subir banco para criar migration**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
docker compose up gestorai-db -d
# Aguardar healthcheck passar (~10s)
docker compose ps
```

Expected: `gestorai-db` com status `healthy`.

- [ ] **Step 4: Criar migration inicial**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet ef migrations add InitialCreate --project src/GestorAI.API
dotnet ef database update --project src/GestorAI.API
```

Expected: `Done.` — tabelas criadas no banco.

- [ ] **Step 5: Verificar GET /health**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet run --project src/GestorAI.API &
sleep 3
curl http://localhost:5002/health
```

Expected: `{"status":"healthy"}`

```bash
kill %1
```

- [ ] **Step 6: Commit**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add .
git commit -m "feat: Docker Compose, Dockerfile e migration inicial"
```

---

## Task 9: Scaffold do frontend

**Files:**
- Create: `gestorai-erp/frontend/` (estrutura Vite)
- Modify: `gestorai-erp/frontend/vite.config.ts`
- Modify: `gestorai-erp/frontend/tailwind.config.ts`

- [ ] **Step 1: Criar projeto Vite**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
```

- [ ] **Step 2: Instalar dependências de produção**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npm install react-router-dom react-hook-form @hookform/resolvers zod recharts lucide-react \
  class-variance-authority clsx tailwind-merge \
  @radix-ui/react-dialog @radix-ui/react-dropdown-menu @radix-ui/react-label \
  @radix-ui/react-select @radix-ui/react-slot @radix-ui/react-toast
```

- [ ] **Step 3: Instalar dependências de dev**

```bash
npm install -D tailwindcss@3 postcss autoprefixer \
  vitest @testing-library/react @testing-library/user-event \
  @vitest/coverage-v8 jsdom @types/node
npx tailwindcss init -p --ts
```

- [ ] **Step 4: Configurar tailwind.config.ts**

`tailwind.config.ts`:
```ts
import type { Config } from 'tailwindcss'

export default {
  darkMode: ['class'],
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        border: 'hsl(var(--border))',
        input: 'hsl(var(--input))',
        ring: 'hsl(var(--ring))',
        background: 'hsl(var(--background))',
        foreground: 'hsl(var(--foreground))',
        primary: {
          DEFAULT: 'hsl(var(--primary))',
          foreground: 'hsl(var(--primary-foreground))',
        },
        secondary: {
          DEFAULT: 'hsl(var(--secondary))',
          foreground: 'hsl(var(--secondary-foreground))',
        },
        muted: {
          DEFAULT: 'hsl(var(--muted))',
          foreground: 'hsl(var(--muted-foreground))',
        },
        accent: {
          DEFAULT: 'hsl(var(--accent))',
          foreground: 'hsl(var(--accent-foreground))',
        },
        destructive: {
          DEFAULT: 'hsl(var(--destructive))',
          foreground: 'hsl(var(--destructive-foreground))',
        },
        card: {
          DEFAULT: 'hsl(var(--card))',
          foreground: 'hsl(var(--card-foreground))',
        },
      },
      borderRadius: {
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)',
      },
    },
  },
  plugins: [],
} satisfies Config
```

- [ ] **Step 5: Configurar vite.config.ts com Vitest**

`vite.config.ts`:
```ts
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': path.resolve(__dirname, './src') },
  },
  server: { port: 5174 },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
  },
})
```

- [ ] **Step 6: Criar src/index.css com variáveis CSS shadcn**

`src/index.css`:
```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  :root {
    --background: 0 0% 100%;
    --foreground: 222.2 84% 4.9%;
    --card: 0 0% 100%;
    --card-foreground: 222.2 84% 4.9%;
    --primary: 221.2 83.2% 53.3%;
    --primary-foreground: 210 40% 98%;
    --secondary: 210 40% 96.1%;
    --secondary-foreground: 222.2 47.4% 11.2%;
    --muted: 210 40% 96.1%;
    --muted-foreground: 215.4 16.3% 46.9%;
    --accent: 210 40% 96.1%;
    --accent-foreground: 222.2 47.4% 11.2%;
    --destructive: 0 84.2% 60.2%;
    --destructive-foreground: 210 40% 98%;
    --border: 214.3 31.8% 91.4%;
    --input: 214.3 31.8% 91.4%;
    --ring: 221.2 83.2% 53.3%;
    --radius: 0.5rem;
  }
  .dark {
    --background: 222.2 84% 4.9%;
    --foreground: 210 40% 98%;
    --card: 222.2 84% 4.9%;
    --card-foreground: 210 40% 98%;
    --primary: 217.2 91.2% 59.8%;
    --primary-foreground: 222.2 47.4% 11.2%;
    --secondary: 217.2 32.6% 17.5%;
    --secondary-foreground: 210 40% 98%;
    --muted: 217.2 32.6% 17.5%;
    --muted-foreground: 215 20.2% 65.1%;
    --accent: 217.2 32.6% 17.5%;
    --accent-foreground: 210 40% 98%;
    --destructive: 0 62.8% 30.6%;
    --destructive-foreground: 210 40% 98%;
    --border: 217.2 32.6% 17.5%;
    --input: 217.2 32.6% 17.5%;
    --ring: 224.3 76.3% 48%;
  }
}

@layer base {
  * { @apply border-border; }
  body { @apply bg-background text-foreground; }
}
```

- [ ] **Step 7: Criar src/lib/utils.ts**

`src/lib/utils.ts`:
```ts
import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
```

- [ ] **Step 8: Criar src/test/setup.ts**

`src/test/setup.ts`:
```ts
import '@testing-library/jest-dom'
```

```bash
npm install -D @testing-library/jest-dom
```

- [ ] **Step 9: Verificar que o projeto compila**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npm run build
```

Expected: `✓ built in` sem erros.

---

## Task 10: api.ts + testes

**Files:**
- Create: `gestorai-erp/frontend/src/services/api.ts`
- Create: `gestorai-erp/frontend/src/test/api.test.ts`

- [ ] **Step 1: Escrever testes (TDD — red)**

`src/test/api.test.ts`:
```ts
import { describe, it, expect, vi, beforeEach } from 'vitest'

const mockFetch = vi.fn()
vi.stubGlobal('fetch', mockFetch)

// Limpa tokens antes de cada teste
beforeEach(() => {
  localStorage.clear()
  mockFetch.mockReset()
})

describe('api', () => {
  it('envia Authorization header com token do localStorage', async () => {
    localStorage.setItem('ga_token', 'meu-token')
    mockFetch.mockResolvedValueOnce({
      ok: true,
      status: 200,
      json: async () => ({ ok: true }),
    })

    const { api } = await import('@/services/api')
    await api.get('/test')

    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/test'),
      expect.objectContaining({
        headers: expect.objectContaining({ Authorization: 'Bearer meu-token' }),
      })
    )
  })

  it('em 401 tenta refresh e retenta request', async () => {
    localStorage.setItem('ga_token', 'token-expirado')
    localStorage.setItem('ga_refresh_token', 'refresh-valido')

    mockFetch
      .mockResolvedValueOnce({ ok: false, status: 401 })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ access_token: 'novo-token' }),
      })
      .mockResolvedValueOnce({
        ok: true,
        status: 200,
        json: async () => ({ data: 'ok' }),
      })

    const { api } = await import('@/services/api')
    const result = await api.get('/protegido')

    expect(result).toEqual({ data: 'ok' })
    expect(localStorage.getItem('ga_token')).toBe('novo-token')
  })
})
```

- [ ] **Step 2: Rodar — confirmar que falham**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npx vitest run src/test/api.test.ts
```

Expected: Erro — `@/services/api` não existe.

- [ ] **Step 3: Criar api.ts**

`src/services/api.ts`:
```ts
const API_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'
const ADMIN_URL = import.meta.env.VITE_ADMIN_URL ?? 'http://localhost:5001'
const CLIENT_ID = import.meta.env.VITE_CLIENT_ID ?? 'gestorai'

async function refreshAccessToken(): Promise<string | null> {
  const refresh = localStorage.getItem('ga_refresh_token')
  if (!refresh) return null

  const res = await fetch(`${ADMIN_URL}/connect/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: CLIENT_ID,
      refresh_token: refresh,
    }),
  })

  if (!res.ok) {
    localStorage.removeItem('ga_token')
    localStorage.removeItem('ga_refresh_token')
    window.location.href = '/auth'
    return null
  }

  const data = await res.json()
  localStorage.setItem('ga_token', data.access_token)
  return data.access_token
}

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem('ga_token')

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
      ...options.headers,
    },
  })

  if (res.status === 401) {
    const newToken = await refreshAccessToken()
    if (!newToken) throw new Error('Unauthorized')
    return request(path, options)
  }

  if (!res.ok) {
    const error = await res.json().catch(() => ({ error: res.statusText }))
    throw new Error(error.error ?? res.statusText)
  }

  return res.json()
}

export const api = {
  get: <T>(path: string) => request<T>(path),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  delete: <T>(path: string) => request<T>(path, { method: 'DELETE' }),
}
```

- [ ] **Step 4: Rodar testes — confirmar que passam**

```bash
npx vitest run src/test/api.test.ts
```

Expected: `✓ 2 tests passed`

---

## Task 11: AuthContext (OAuth2 PKCE)

**Files:**
- Create: `gestorai-erp/frontend/src/contexts/AuthContext.tsx`
- Create: `gestorai-erp/frontend/src/pages/Auth.tsx`
- Create: `gestorai-erp/frontend/src/pages/AuthCallback.tsx`

- [ ] **Step 1: Criar AuthContext.tsx**

`src/contexts/AuthContext.tsx`:
```tsx
import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'

const ADMIN_URL = import.meta.env.VITE_ADMIN_URL ?? 'http://localhost:5001'
const CLIENT_ID = import.meta.env.VITE_CLIENT_ID ?? 'gestorai'

interface AuthContextValue {
  isAuthenticated: boolean
  isLoading: boolean
  login: () => Promise<void>
  logout: () => void
  handleCallback: (code: string) => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

function generateCodeVerifier(): string {
  const array = new Uint8Array(32)
  crypto.getRandomValues(array)
  return btoa(String.fromCharCode(...array))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '')
}

async function generateCodeChallenge(verifier: string): Promise<string> {
  const data = new TextEncoder().encode(verifier)
  const digest = await crypto.subtle.digest('SHA-256', data)
  return btoa(String.fromCharCode(...new Uint8Array(digest)))
    .replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '')
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    setIsAuthenticated(!!localStorage.getItem('ga_token'))
    setIsLoading(false)
  }, [])

  async function login() {
    const verifier = generateCodeVerifier()
    const challenge = await generateCodeChallenge(verifier)
    localStorage.setItem('pkce_verifier', verifier)

    const params = new URLSearchParams({
      response_type: 'code',
      client_id: CLIENT_ID,
      redirect_uri: `${window.location.origin}/auth/callback`,
      scope: 'openid profile offline_access',
      code_challenge: challenge,
      code_challenge_method: 'S256',
    })
    window.location.href = `${ADMIN_URL}/connect/authorize?${params}`
  }

  async function handleCallback(code: string) {
    const verifier = localStorage.getItem('pkce_verifier')
    if (!verifier) throw new Error('PKCE verifier ausente')

    const res = await fetch(`${ADMIN_URL}/connect/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
      body: new URLSearchParams({
        grant_type: 'authorization_code',
        client_id: CLIENT_ID,
        code,
        redirect_uri: `${window.location.origin}/auth/callback`,
        code_verifier: verifier,
      }),
    })

    if (!res.ok) throw new Error('Falha na troca do código')

    const tokens = await res.json()
    localStorage.setItem('ga_token', tokens.access_token)
    localStorage.setItem('ga_refresh_token', tokens.refresh_token)
    localStorage.removeItem('pkce_verifier')
    setIsAuthenticated(true)
  }

  function logout() {
    localStorage.removeItem('ga_token')
    localStorage.removeItem('ga_refresh_token')
    setIsAuthenticated(false)
    window.location.href = '/auth'
  }

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, login, logout, handleCallback }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth deve ser usado dentro de AuthProvider')
  return ctx
}
```

- [ ] **Step 2: Criar Auth.tsx**

`src/pages/Auth.tsx`:
```tsx
import { useEffect } from 'react'
import { useAuth } from '@/contexts/AuthContext'

export default function Auth() {
  const { login } = useAuth()
  useEffect(() => { login() }, [])
  return (
    <div className="flex h-screen items-center justify-center">
      <p className="text-muted-foreground">Redirecionando para login...</p>
    </div>
  )
}
```

- [ ] **Step 3: Criar AuthCallback.tsx**

`src/pages/AuthCallback.tsx`:
```tsx
import { useEffect, useRef } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'

export default function AuthCallback() {
  const { handleCallback } = useAuth()
  const navigate = useNavigate()
  const ran = useRef(false)

  useEffect(() => {
    if (ran.current) return
    ran.current = true

    const code = new URLSearchParams(window.location.search).get('code')
    if (!code) { navigate('/auth'); return }

    handleCallback(code)
      .then(() => navigate('/'))
      .catch(() => navigate('/auth'))
  }, [])

  return (
    <div className="flex h-screen items-center justify-center">
      <p className="text-muted-foreground">Autenticando...</p>
    </div>
  )
}
```

---

## Task 12: Layout, Router e páginas stub

**Files:**
- Create: `gestorai-erp/frontend/src/components/layout/Sidebar.tsx`
- Create: `gestorai-erp/frontend/src/components/layout/AppLayout.tsx`
- Create: `gestorai-erp/frontend/src/router/index.tsx`
- Create: `gestorai-erp/frontend/src/pages/Dashboard.tsx`
- Modify: `gestorai-erp/frontend/src/main.tsx`
- Modify: `gestorai-erp/frontend/src/App.tsx`

- [ ] **Step 1: Criar Sidebar.tsx**

```bash
mkdir -p src/components/layout src/router src/pages
```

`src/components/layout/Sidebar.tsx`:
```tsx
import { NavLink } from 'react-router-dom'
import { LayoutDashboard, ShoppingCart, Package, Wallet, Users, BarChart3 } from 'lucide-react'
import { cn } from '@/lib/utils'

const links = [
  { to: '/', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/vendas', icon: ShoppingCart, label: 'Vendas' },
  { to: '/estoque', icon: Package, label: 'Estoque' },
  { to: '/financeiro', icon: Wallet, label: 'Financeiro' },
  { to: '/clientes', icon: Users, label: 'Clientes' },
  { to: '/relatorios', icon: BarChart3, label: 'Relatórios' },
]

export default function Sidebar() {
  return (
    <aside className="flex h-screen w-56 flex-col border-r bg-card px-3 py-4">
      <div className="mb-6 px-2">
        <span className="text-xl font-bold text-primary">GestorAI</span>
      </div>
      <nav className="flex flex-col gap-1">
        {links.map(({ to, icon: Icon, label }) => (
          <NavLink
            key={to}
            to={to}
            end={to === '/'}
            className={({ isActive }) =>
              cn(
                'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                isActive
                  ? 'bg-primary text-primary-foreground'
                  : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
              )
            }
          >
            <Icon size={18} />
            {label}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
```

- [ ] **Step 2: Criar AppLayout.tsx**

`src/components/layout/AppLayout.tsx`:
```tsx
import { Navigate, Outlet } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import Sidebar from './Sidebar'

export default function AppLayout() {
  const { isAuthenticated, isLoading } = useAuth()

  if (isLoading) {
    return (
      <div className="flex h-screen items-center justify-center">
        <p className="text-muted-foreground">Carregando...</p>
      </div>
    )
  }

  if (!isAuthenticated) return <Navigate to="/auth" replace />

  return (
    <div className="flex h-screen overflow-hidden">
      <Sidebar />
      <main className="flex-1 overflow-y-auto p-6">
        <Outlet />
      </main>
    </div>
  )
}
```

- [ ] **Step 3: Criar router/index.tsx**

`src/router/index.tsx`:
```tsx
import { createBrowserRouter } from 'react-router-dom'
import AppLayout from '@/components/layout/AppLayout'
import Auth from '@/pages/Auth'
import AuthCallback from '@/pages/AuthCallback'
import Dashboard from '@/pages/Dashboard'

export const router = createBrowserRouter([
  { path: '/auth', element: <Auth /> },
  { path: '/auth/callback', element: <AuthCallback /> },
  {
    element: <AppLayout />,
    children: [
      { path: '/', element: <Dashboard /> },
      { path: '/vendas', element: <div>Vendas — Plano 3</div> },
      { path: '/estoque', element: <div>Estoque — Plano 2</div> },
      { path: '/financeiro', element: <div>Financeiro — Plano 4</div> },
      { path: '/clientes', element: <div>Clientes — Plano 2</div> },
      { path: '/relatorios', element: <div>Relatórios — Plano 5</div> },
    ],
  },
])
```

- [ ] **Step 4: Criar pages/Dashboard.tsx (stub)**

`src/pages/Dashboard.tsx`:
```tsx
export default function Dashboard() {
  return (
    <div>
      <h1 className="text-2xl font-bold">Dashboard</h1>
      <p className="mt-2 text-muted-foreground">Implementado no Plano 5.</p>
    </div>
  )
}
```

- [ ] **Step 5: Atualizar main.tsx**

`src/main.tsx`:
```tsx
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider } from 'react-router-dom'
import { AuthProvider } from '@/contexts/AuthContext'
import { router } from '@/router'
import './index.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>
  </StrictMode>
)
```

- [ ] **Step 6: Criar .env.local**

`frontend/.env.local`:
```
VITE_API_URL=http://localhost:5002
VITE_ADMIN_URL=http://localhost:5001
VITE_CLIENT_ID=gestorai
```

- [ ] **Step 7: Verificar build**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npm run build
```

Expected: `✓ built in` sem erros TypeScript.

- [ ] **Step 8: Rodar todos os testes**

```bash
npx vitest run
```

Expected: `✓ 2 tests passed`

- [ ] **Step 9: Commit final**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add frontend/
git commit -m "feat: frontend scaffold — auth PKCE, api.ts, layout e router"
```

---

## Task 13: .gitignore e verificação final

**Files:**
- Create: `gestorai-erp/.gitignore`

- [ ] **Step 1: Criar .gitignore**

`gestorai-erp/.gitignore`:
```gitignore
# .NET
bin/
obj/
*.user
.vs/
appsettings.Development.json

# Node
node_modules/
dist/
.env.local
.env*.local

# EF Core migrations geradas localmente
# (manter Migrations/ no git)

# Docker
.dockerignore

# OS
.DS_Store
Thumbs.db
```

- [ ] **Step 2: Verificar sistema completo**

```bash
# Backend: todos os testes passam
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test

# Frontend: todos os testes passam
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npx vitest run

# Frontend: build sem erros
npm run build

# Backend: build sem erros
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet build
```

Expected: zero erros em todos os comandos.

- [ ] **Step 3: Commit final**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add .
git commit -m "feat: Plano 1 completo — GestorAI foundation pronta"
```

---

## Checklist de entrega

Ao final deste plano, o sistema deve:

- [ ] Backend compila e passa em todos os testes (`dotnet test`)
- [ ] `GET /health` retorna `{"status":"healthy"}` 
- [ ] Multi-tenancy: QueryFilters filtram por `EmpresaId` automaticamente
- [ ] TenantMiddleware extrai `empresa_id` do JWT e popula TenantContext
- [ ] Banco PostgreSQL sobe via `docker compose up`
- [ ] Migration inicial aplicada com todas as tabelas criadas
- [ ] Frontend compila sem erros TypeScript (`npm run build`)
- [ ] Testes do `api.ts` passam (token, retry com refresh)
- [ ] Rota `/auth` redireciona para Quantix Admin via PKCE
- [ ] Sidebar renderiza com todos os módulos
- [ ] Rotas protegidas redirecionam para `/auth` sem token
