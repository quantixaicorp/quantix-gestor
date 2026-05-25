# GestorAI — Plano 2/5: Estoque + Clientes

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Módulos de Estoque (Categorias, Produtos, Movimentações) e Clientes totalmente funcionais — CRUD, entrada manual de estoque com recálculo de custo médio, alertas de estoque baixo.

**Architecture:** Segue o padrão do Plano 1: Services injetam `TenantContext` para setar `EmpresaId` ao criar entidades; `AppDbContext` aplica QueryFilter automaticamente nas leituras. Endpoints Minimal API com `ValidationFilter`. Hooks do frontend chamam `api.ts`.

**Tech Stack:** .NET 10, EF Core 9, FluentValidation | React 18, TypeScript, Vite, shadcn/ui, Tailwind CSS 3

**Pré-requisito:** Plano 1 concluído e testes passando.

---

## Mapa de arquivos

### Backend

| Arquivo | Responsabilidade |
|---|---|
| `src/GestorAI.API/DTOs/Estoque/CategoriaDto.cs` | Records de request/response para categorias |
| `src/GestorAI.API/DTOs/Estoque/ProdutoDto.cs` | Records de request/response para produtos |
| `src/GestorAI.API/DTOs/Estoque/MovimentacaoDto.cs` | Record de response para movimentações |
| `src/GestorAI.API/DTOs/Clientes/ClienteDto.cs` | Records de request/response para clientes |
| `src/GestorAI.API/Services/Estoque/CategoriaService.cs` | CRUD de categorias |
| `src/GestorAI.API/Services/Estoque/ProdutoService.cs` | CRUD de produtos + entrada de estoque |
| `src/GestorAI.API/Services/Clientes/ClienteService.cs` | CRUD de clientes |
| `src/GestorAI.API/Services/Estoque/CreateProdutoValidator.cs` | Validação FluentValidation |
| `src/GestorAI.API/Services/Clientes/CreateClienteValidator.cs` | Validação FluentValidation |
| `src/GestorAI.API/Endpoints/EstoqueEndpoints.cs` | Minimal API routes de estoque |
| `src/GestorAI.API/Endpoints/ClientesEndpoints.cs` | Minimal API routes de clientes |
| `tests/GestorAI.Tests/Services/ProdutoServiceTests.cs` | Testes de ProdutoService |
| `tests/GestorAI.Tests/Services/ClienteServiceTests.cs` | Testes de ClienteService |

### Frontend

| Arquivo | Responsabilidade |
|---|---|
| `src/hooks/useEstoque.ts` | Estado + operações de estoque e categorias |
| `src/hooks/useClientes.ts` | Estado + operações de clientes |
| `src/types/estoque.ts` | Types TypeScript para estoque |
| `src/types/clientes.ts` | Types TypeScript para clientes |
| `src/pages/estoque/Produtos.tsx` | Listagem e CRUD de produtos |
| `src/pages/estoque/Movimentacoes.tsx` | Histórico de movimentações |
| `src/components/estoque/ProdutoForm.tsx` | Form de criação/edição de produto |
| `src/components/estoque/EntradaEstoqueDialog.tsx` | Dialog para entrada de estoque |
| `src/pages/clientes/Clientes.tsx` | Listagem e CRUD de clientes |
| `src/components/clientes/ClienteForm.tsx` | Form de criação/edição de cliente |

---

## Task 1: DTOs

**Files:**
- Create: `src/GestorAI.API/DTOs/Estoque/CategoriaDto.cs`
- Create: `src/GestorAI.API/DTOs/Estoque/ProdutoDto.cs`
- Create: `src/GestorAI.API/DTOs/Estoque/MovimentacaoDto.cs`
- Create: `src/GestorAI.API/DTOs/Clientes/ClienteDto.cs`

- [ ] **Step 1: Criar diretórios**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
mkdir -p src/GestorAI.API/DTOs/Estoque src/GestorAI.API/DTOs/Clientes
```

- [ ] **Step 2: Criar CategoriaDto.cs**

`src/GestorAI.API/DTOs/Estoque/CategoriaDto.cs`:
```csharp
namespace GestorAI.API.DTOs.Estoque;

public record CategoriaResponse(Guid Id, string Nome);
public record CreateCategoriaRequest(string Nome);
```

- [ ] **Step 3: Criar ProdutoDto.cs**

`src/GestorAI.API/DTOs/Estoque/ProdutoDto.cs`:
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
    bool EstoqueBaixo);

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
    bool Ativo);

public record EntradaEstoqueRequest(
    Guid ProdutoId,
    decimal Quantidade,
    decimal? CustoUnitario,
    string? Observacao);
```

- [ ] **Step 4: Criar MovimentacaoDto.cs**

`src/GestorAI.API/DTOs/Estoque/MovimentacaoDto.cs`:
```csharp
namespace GestorAI.API.DTOs.Estoque;

public record MovimentacaoResponse(
    Guid Id,
    Guid ProdutoId,
    string ProdutoNome,
    string Tipo,
    decimal Quantidade,
    string Origem,
    DateTime DataHora,
    string? Observacao);
```

- [ ] **Step 5: Criar ClienteDto.cs**

`src/GestorAI.API/DTOs/Clientes/ClienteDto.cs`:
```csharp
namespace GestorAI.API.DTOs.Clientes;

public record ClienteResponse(
    Guid Id,
    string Nome,
    string Whatsapp,
    string? Email,
    string? Observacoes,
    DateTime DataCadastro);

public record CreateClienteRequest(
    string Nome,
    string Whatsapp,
    string? Email,
    string? Observacoes);

public record UpdateClienteRequest(
    string Nome,
    string Whatsapp,
    string? Email,
    string? Observacoes);
```

- [ ] **Step 6: Build**

```bash
dotnet build
```

Expected: `Build succeeded.`

---

## Task 2: Validators

**Files:**
- Create: `src/GestorAI.API/Services/Estoque/CreateProdutoValidator.cs`
- Create: `src/GestorAI.API/Services/Clientes/CreateClienteValidator.cs`

- [ ] **Step 1: Criar diretórios**

```bash
mkdir -p src/GestorAI.API/Services/Estoque src/GestorAI.API/Services/Clientes
```

- [ ] **Step 2: Criar CreateProdutoValidator.cs**

`src/GestorAI.API/Services/Estoque/CreateProdutoValidator.cs`:
```csharp
using FluentValidation;
using GestorAI.API.DTOs.Estoque;

namespace GestorAI.API.Services.Estoque;

public class CreateProdutoValidator : AbstractValidator<CreateProdutoRequest>
{
    public CreateProdutoValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrecoVenda).GreaterThan(0);
        RuleFor(x => x.CustoMedio).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstoqueAtual).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EstoqueMinimo).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoriaId).NotEmpty();
    }
}

public class EntradaEstoqueValidator : AbstractValidator<EntradaEstoqueRequest>
{
    public EntradaEstoqueValidator()
    {
        RuleFor(x => x.ProdutoId).NotEmpty();
        RuleFor(x => x.Quantidade).GreaterThan(0);
        RuleFor(x => x.CustoUnitario).GreaterThanOrEqualTo(0).When(x => x.CustoUnitario.HasValue);
    }
}
```

- [ ] **Step 3: Criar CreateClienteValidator.cs**

`src/GestorAI.API/Services/Clientes/CreateClienteValidator.cs`:
```csharp
using FluentValidation;
using GestorAI.API.DTOs.Clientes;

namespace GestorAI.API.Services.Clientes;

public class CreateClienteValidator : AbstractValidator<CreateClienteRequest>
{
    public CreateClienteValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Whatsapp).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}
```

- [ ] **Step 4: Build**

```bash
dotnet build
```

Expected: `Build succeeded.`

---

## Task 3: Services de Estoque com testes

**Files:**
- Create: `src/GestorAI.API/Services/Estoque/CategoriaService.cs`
- Create: `src/GestorAI.API/Services/Estoque/ProdutoService.cs`
- Test: `tests/GestorAI.Tests/Services/ProdutoServiceTests.cs`

- [ ] **Step 1: Escrever testes (TDD — red)**

`tests/GestorAI.Tests/Services/ProdutoServiceTests.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Estoque;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ProdutoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ProdutoService service) Setup()
    {
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        var service = new ProdutoService(db, tenantContext);
        return (db, service);
    }

    private async Task<Categoria> SeedCategoriaAsync(AppDbContext db)
    {
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Categoria Teste" };
        db.Categorias.Add(cat);
        await db.SaveChangesAsync();
        return cat;
    }

    [Fact]
    public async Task CreateAsync_PersistsProduto()
    {
        var (db, service) = Setup();
        var cat = await SeedCategoriaAsync(db);
        var req = new CreateProdutoRequest(cat.Id, "Camiseta", null, 50m, 20m, 10m, 3m, null);

        var result = await service.CreateAsync(req, default);

        Assert.Equal("Camiseta", result.Nome);
        Assert.Equal(_empresaId, db.Produtos.First().EmpresaId);
    }

    [Fact]
    public async Task EntradaEstoqueAsync_UpdatesEstoqueAtualAndCustoMedio()
    {
        var (db, service) = Setup();
        var cat = await SeedCategoriaAsync(db);
        var produto = new Produto
        {
            EmpresaId = _empresaId, CategoriaId = cat.Id,
            Nome = "Produto", PrecoVenda = 100m,
            CustoMedio = 20m, EstoqueAtual = 10m, EstoqueMinimo = 2m
        };
        db.Produtos.Add(produto);
        await db.SaveChangesAsync();

        var req = new EntradaEstoqueRequest(produto.Id, 10m, 30m, null);
        await service.EntradaEstoqueAsync(req, default);

        var atualizado = await db.Produtos.FindAsync(produto.Id);
        Assert.Equal(20m, atualizado!.EstoqueAtual);
        Assert.Equal(25m, atualizado.CustoMedio); // (10*20 + 10*30) / 20 = 25
    }

    [Fact]
    public async Task EntradaEstoqueAsync_CreatesMovimentacaoEstoque()
    {
        var (db, service) = Setup();
        var cat = await SeedCategoriaAsync(db);
        var produto = new Produto
        {
            EmpresaId = _empresaId, CategoriaId = cat.Id,
            Nome = "Produto", PrecoVenda = 50m,
            CustoMedio = 10m, EstoqueAtual = 5m, EstoqueMinimo = 2m
        };
        db.Produtos.Add(produto);
        await db.SaveChangesAsync();

        await service.EntradaEstoqueAsync(
            new EntradaEstoqueRequest(produto.Id, 5m, null, "Reposição"), default);

        var mov = await db.MovimentacoesEstoque.IgnoreQueryFilters().FirstAsync();
        Assert.Equal(TipoMovimentacao.Entrada, mov.Tipo);
        Assert.Equal(OrigemMovimentacao.Manual, mov.Origem);
        Assert.Equal(5m, mov.Quantidade);
    }

    [Fact]
    public async Task EntradaEstoqueAsync_ThrowsWhenProdutoNotFound()
    {
        var (_, service) = Setup();
        var req = new EntradaEstoqueRequest(Guid.NewGuid(), 5m, null, null);

        await Assert.ThrowsAsync<AppException>(() => service.EntradaEstoqueAsync(req, default));
    }
}
```

- [ ] **Step 2: Rodar — confirmar que falham**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test tests/GestorAI.Tests --filter "ProdutoServiceTests"
```

Expected: Erro de compilação — `ProdutoService` não existe.

- [ ] **Step 3: Criar CategoriaService.cs**

`src/GestorAI.API/Services/Estoque/CategoriaService.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Estoque;

public class CategoriaService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<CategoriaResponse>> ListAsync(CancellationToken ct) =>
        await db.Categorias
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaResponse(c.Id, c.Nome))
            .ToListAsync(ct);

    public async Task<CategoriaResponse> CreateAsync(CreateCategoriaRequest req, CancellationToken ct)
    {
        var categoria = new Categoria { Nome = req.Nome, EmpresaId = tenantContext.EmpresaId };
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync(ct);
        return new CategoriaResponse(categoria.Id, categoria.Nome);
    }
}
```

- [ ] **Step 4: Criar ProdutoService.cs**

`src/GestorAI.API/Services/Estoque/ProdutoService.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Estoque;

public class ProdutoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ProdutoResponse>> ListAsync(
        string? busca, Guid? categoriaId, bool? apenasEstoqueBaixo, CancellationToken ct)
    {
        var query = db.Produtos.Include(p => p.Categoria).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(p => p.Nome.Contains(busca) ||
                                     (p.CodigoBarras != null && p.CodigoBarras == busca));
        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        if (apenasEstoqueBaixo == true)
            query = query.Where(p => p.EstoqueAtual <= p.EstoqueMinimo);

        return await query
            .OrderBy(p => p.Nome)
            .Select(p => ToResponse(p))
            .ToListAsync(ct);
    }

    public async Task<ProdutoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Produtos.Include(x => x.Categoria)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Produto não encontrado", 404);
        return ToResponse(p);
    }

    public async Task<ProdutoResponse> CreateAsync(CreateProdutoRequest req, CancellationToken ct)
    {
        var produto = new Produto
        {
            EmpresaId = tenantContext.EmpresaId,
            CategoriaId = req.CategoriaId,
            Nome = req.Nome,
            Descricao = req.Descricao,
            PrecoVenda = req.PrecoVenda,
            CustoMedio = req.CustoMedio,
            EstoqueAtual = req.EstoqueAtual,
            EstoqueMinimo = req.EstoqueMinimo,
            CodigoBarras = req.CodigoBarras,
        };
        db.Produtos.Add(produto);

        if (req.EstoqueAtual > 0)
            db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                EmpresaId = tenantContext.EmpresaId,
                ProdutoId = produto.Id,
                Tipo = TipoMovimentacao.Entrada,
                Quantidade = req.EstoqueAtual,
                Origem = OrigemMovimentacao.Manual,
                Observacao = "Estoque inicial",
            });

        await db.SaveChangesAsync(ct);
        return await GetAsync(produto.Id, ct);
    }

    public async Task<ProdutoResponse> UpdateAsync(Guid id, UpdateProdutoRequest req, CancellationToken ct)
    {
        var produto = await db.Produtos.FindAsync([id], ct)
            ?? throw new AppException("Produto não encontrado", 404);

        produto.CategoriaId = req.CategoriaId;
        produto.Nome = req.Nome;
        produto.Descricao = req.Descricao;
        produto.PrecoVenda = req.PrecoVenda;
        produto.EstoqueMinimo = req.EstoqueMinimo;
        produto.CodigoBarras = req.CodigoBarras;
        produto.Ativo = req.Ativo;
        produto.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<ProdutoResponse> EntradaEstoqueAsync(EntradaEstoqueRequest req, CancellationToken ct)
    {
        var produto = await db.Produtos.FindAsync([req.ProdutoId], ct)
            ?? throw new AppException("Produto não encontrado", 404);

        var novoEstoque = produto.EstoqueAtual + req.Quantidade;

        if (req.CustoUnitario.HasValue && novoEstoque > 0)
            produto.CustoMedio =
                (produto.EstoqueAtual * produto.CustoMedio + req.Quantidade * req.CustoUnitario.Value)
                / novoEstoque;

        produto.EstoqueAtual = novoEstoque;
        produto.AtualizadoEm = DateTime.UtcNow;

        db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
        {
            EmpresaId = tenantContext.EmpresaId,
            ProdutoId = produto.Id,
            Tipo = TipoMovimentacao.Entrada,
            Quantidade = req.Quantidade,
            Origem = OrigemMovimentacao.Manual,
            Observacao = req.Observacao,
        });

        await db.SaveChangesAsync(ct);
        return await GetAsync(produto.Id, ct);
    }

    public async Task<List<MovimentacaoResponse>> ListMovimentacoesAsync(
        Guid? produtoId, CancellationToken ct) =>
        await db.MovimentacoesEstoque
            .Include(m => m.Produto)
            .Where(m => !produtoId.HasValue || m.ProdutoId == produtoId.Value)
            .OrderByDescending(m => m.DataHora)
            .Select(m => new MovimentacaoResponse(
                m.Id, m.ProdutoId, m.Produto!.Nome,
                m.Tipo.ToString(), m.Quantidade,
                m.Origem.ToString(), m.DataHora, m.Observacao))
            .ToListAsync(ct);

    private static ProdutoResponse ToResponse(Produto p) => new(
        p.Id, p.CategoriaId, p.Categoria?.Nome ?? "",
        p.Nome, p.Descricao, p.PrecoVenda, p.CustoMedio,
        p.EstoqueAtual, p.EstoqueMinimo, p.CodigoBarras,
        p.Ativo, p.EstoqueAtual <= p.EstoqueMinimo);
}
```

- [ ] **Step 5: Rodar testes — confirmar que passam**

```bash
dotnet test tests/GestorAI.Tests --filter "ProdutoServiceTests"
```

Expected: `Passed! - Failed: 0, Passed: 4`

---

## Task 4: ClienteService com testes

**Files:**
- Create: `src/GestorAI.API/Services/Clientes/ClienteService.cs`
- Test: `tests/GestorAI.Tests/Services/ClienteServiceTests.cs`

- [ ] **Step 1: Escrever testes (TDD — red)**

`tests/GestorAI.Tests/Services/ClienteServiceTests.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Clientes;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Clientes;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ClienteServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ClienteService service) Setup()
    {
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new ClienteService(db, tenantContext));
    }

    [Fact]
    public async Task CreateAsync_PersistsCliente()
    {
        var (_, service) = Setup();
        var req = new CreateClienteRequest("Maria", "11999990001", null, null);

        var result = await service.CreateAsync(req, default);

        Assert.Equal("Maria", result.Nome);
        Assert.Equal("11999990001", result.Whatsapp);
    }

    [Fact]
    public async Task GetAsync_ThrowsWhenNotFound()
    {
        var (_, service) = Setup();

        await Assert.ThrowsAsync<AppException>(() => service.GetAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task ListAsync_FiltersByBusca()
    {
        var (db, service) = Setup();
        db.Clientes.AddRange(
            new Cliente { EmpresaId = _empresaId, Nome = "Ana Silva", Whatsapp = "11999990001" },
            new Cliente { EmpresaId = _empresaId, Nome = "Carlos", Whatsapp = "11999990002" });
        await db.SaveChangesAsync();

        var result = await service.ListAsync("Ana", default);

        Assert.Single(result);
        Assert.Equal("Ana Silva", result[0].Nome);
    }
}
```

- [ ] **Step 2: Rodar — confirmar que falham**

```bash
dotnet test tests/GestorAI.Tests --filter "ClienteServiceTests"
```

Expected: Erro de compilação.

- [ ] **Step 3: Criar ClienteService.cs**

`src/GestorAI.API/Services/Clientes/ClienteService.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Clientes;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Clientes;

public class ClienteService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ClienteResponse>> ListAsync(string? busca, CancellationToken ct)
    {
        var query = db.Clientes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(c => c.Nome.Contains(busca) || c.Whatsapp.Contains(busca));

        return await query
            .OrderBy(c => c.Nome)
            .Select(c => ToResponse(c))
            .ToListAsync(ct);
    }

    public async Task<ClienteResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Clientes.FindAsync([id], ct)
            ?? throw new AppException("Cliente não encontrado", 404);
        return ToResponse(c);
    }

    public async Task<ClienteResponse> CreateAsync(CreateClienteRequest req, CancellationToken ct)
    {
        var cliente = new Cliente
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            Whatsapp = req.Whatsapp,
            Email = req.Email,
            Observacoes = req.Observacoes,
        };
        db.Clientes.Add(cliente);
        await db.SaveChangesAsync(ct);
        return ToResponse(cliente);
    }

    public async Task<ClienteResponse> UpdateAsync(Guid id, UpdateClienteRequest req, CancellationToken ct)
    {
        var cliente = await db.Clientes.FindAsync([id], ct)
            ?? throw new AppException("Cliente não encontrado", 404);

        cliente.Nome = req.Nome;
        cliente.Whatsapp = req.Whatsapp;
        cliente.Email = req.Email;
        cliente.Observacoes = req.Observacoes;

        await db.SaveChangesAsync(ct);
        return ToResponse(cliente);
    }

    private static ClienteResponse ToResponse(Cliente c) =>
        new(c.Id, c.Nome, c.Whatsapp, c.Email, c.Observacoes, c.DataCadastro);
}
```

- [ ] **Step 4: Rodar todos os testes**

```bash
dotnet test tests/GestorAI.Tests
```

Expected: `Passed! - Failed: 0, Passed: 12`

- [ ] **Step 5: Commit**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add backend/
git commit -m "feat: services de Estoque e Clientes com testes"
```

---

## Task 5: Endpoints

**Files:**
- Create: `src/GestorAI.API/Endpoints/EstoqueEndpoints.cs`
- Create: `src/GestorAI.API/Endpoints/ClientesEndpoints.cs`
- Modify: `src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar EstoqueEndpoints.cs**

```bash
mkdir -p src/GestorAI.API/Endpoints
```

`src/GestorAI.API/Endpoints/EstoqueEndpoints.cs`:
```csharp
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.Services.Estoque;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class EstoqueEndpoints
{
    public static void MapEstoque(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization("EstoqueAccess");

        // Categorias
        group.MapGet("/categorias", async (CategoriaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPost("/categorias", async (CreateCategoriaRequest req, CategoriaService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/categorias/{result.Id}", result);
        });

        // Produtos
        group.MapGet("/produtos", async (
            string? busca, Guid? categoriaId, bool? estoqueBaixo,
            ProdutoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(busca, categoriaId, estoqueBaixo, ct)));

        group.MapGet("/produtos/{id:guid}", async (Guid id, ProdutoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/produtos", async (CreateProdutoRequest req, ProdutoService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/produtos/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateProdutoRequest>>();

        group.MapPut("/produtos/{id:guid}", async (
            Guid id, UpdateProdutoRequest req, ProdutoService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        // Estoque
        group.MapPost("/estoque/movimentar", async (
            EntradaEstoqueRequest req, ProdutoService svc, CancellationToken ct) =>
            Results.Ok(await svc.EntradaEstoqueAsync(req, ct)))
            .AddEndpointFilter<ValidationFilter<EntradaEstoqueRequest>>();

        group.MapGet("/estoque/movimentacoes", async (
            Guid? produtoId, ProdutoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListMovimentacoesAsync(produtoId, ct)));
    }
}
```

- [ ] **Step 2: Criar ClientesEndpoints.cs**

`src/GestorAI.API/Endpoints/ClientesEndpoints.cs`:
```csharp
using GestorAI.API.DTOs.Clientes;
using GestorAI.API.Services.Clientes;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class ClientesEndpoints
{
    public static void MapClientes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes").RequireAuthorization();

        group.MapGet("/", async (string? busca, ClienteService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(busca, ct)));

        group.MapGet("/{id:guid}", async (Guid id, ClienteService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateClienteRequest req, ClienteService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/clientes/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateClienteRequest>>();

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateClienteRequest req, ClienteService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));
    }
}
```

- [ ] **Step 3: Registrar services e endpoints em Program.cs**

Adicionar antes de `var app = builder.Build();` em `Program.cs`:
```csharp
// Services — Estoque
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<ProdutoService>();
// Services — Clientes
builder.Services.AddScoped<ClienteService>();
// Validators
builder.Services.AddScoped<IValidator<CreateProdutoRequest>, CreateProdutoValidator>();
builder.Services.AddScoped<IValidator<EntradaEstoqueRequest>, EntradaEstoqueValidator>();
builder.Services.AddScoped<IValidator<CreateClienteRequest>, CreateClienteValidator>();
```

Adicionar os usings necessários:
```csharp
using FluentValidation;
using GestorAI.API.DTOs.Clientes;
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.Endpoints;
using GestorAI.API.Services.Clientes;
using GestorAI.API.Services.Estoque;
using GestorAI.API.Services.Estoque;
```

Adicionar após `app.UseAuthorization();`:
```csharp
app.MapEstoque();
app.MapClientes();
```

- [ ] **Step 4: Build e testes**

```bash
dotnet build && dotnet test tests/GestorAI.Tests
```

Expected: `Build succeeded.` e todos os testes passando.

- [ ] **Step 5: Commit**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add backend/
git commit -m "feat: endpoints de Estoque e Clientes"
```

---

## Task 6: Types e hooks do frontend

**Files:**
- Create: `src/types/estoque.ts`
- Create: `src/types/clientes.ts`
- Create: `src/hooks/useEstoque.ts`
- Create: `src/hooks/useClientes.ts`

- [ ] **Step 1: Criar src/types/estoque.ts**

```bash
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/types
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/hooks
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/pages/estoque
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/pages/clientes
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/components/estoque
mkdir -p /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend/src/components/clientes
```

`src/types/estoque.ts`:
```ts
export interface CategoriaResponse { id: string; nome: string }

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
}

export interface CreateProdutoRequest {
  categoriaId: string
  nome: string
  descricao?: string
  precoVenda: number
  custoMedio: number
  estoqueAtual: number
  estoqueMinimo: number
  codigoBarras?: string
}

export interface UpdateProdutoRequest {
  categoriaId: string
  nome: string
  descricao?: string
  precoVenda: number
  estoqueMinimo: number
  codigoBarras?: string
  ativo: boolean
}

export interface EntradaEstoqueRequest {
  produtoId: string
  quantidade: number
  custoUnitario?: number
  observacao?: string
}

export interface MovimentacaoResponse {
  id: string
  produtoId: string
  produtoNome: string
  tipo: string
  quantidade: number
  origem: string
  dataHora: string
  observacao: string | null
}
```

- [ ] **Step 2: Criar src/types/clientes.ts**

`src/types/clientes.ts`:
```ts
export interface ClienteResponse {
  id: string
  nome: string
  whatsapp: string
  email: string | null
  observacoes: string | null
  dataCadastro: string
}

export interface CreateClienteRequest {
  nome: string
  whatsapp: string
  email?: string
  observacoes?: string
}

export interface UpdateClienteRequest {
  nome: string
  whatsapp: string
  email?: string
  observacoes?: string
}
```

- [ ] **Step 3: Criar src/hooks/useEstoque.ts**

`src/hooks/useEstoque.ts`:
```ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  ProdutoResponse, CategoriaResponse, MovimentacaoResponse,
  CreateProdutoRequest, UpdateProdutoRequest, EntradaEstoqueRequest,
} from '@/types/estoque'

export function useEstoque() {
  const [produtos, setProdutos] = useState<ProdutoResponse[]>([])
  const [categorias, setCategorias] = useState<CategoriaResponse[]>([])
  const [movimentacoes, setMovimentacoes] = useState<MovimentacaoResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const listProdutos = useCallback(async (params?: {
    busca?: string; categoriaId?: string; estoqueBaixo?: boolean
  }) => {
    setLoading(true)
    try {
      const qs = new URLSearchParams()
      if (params?.busca) qs.set('busca', params.busca)
      if (params?.categoriaId) qs.set('categoriaId', params.categoriaId)
      if (params?.estoqueBaixo) qs.set('estoqueBaixo', 'true')
      const data = await api.get<ProdutoResponse[]>(`/api/produtos?${qs}`)
      setProdutos(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar produtos')
    } finally {
      setLoading(false)
    }
  }, [])

  const listCategorias = useCallback(async () => {
    const data = await api.get<CategoriaResponse[]>('/api/categorias')
    setCategorias(data)
  }, [])

  const createProduto = useCallback(async (req: CreateProdutoRequest) => {
    const result = await api.post<ProdutoResponse>('/api/produtos', req)
    setProdutos(prev => [...prev, result])
    return result
  }, [])

  const updateProduto = useCallback(async (id: string, req: UpdateProdutoRequest) => {
    const result = await api.put<ProdutoResponse>(`/api/produtos/${id}`, req)
    setProdutos(prev => prev.map(p => p.id === id ? result : p))
    return result
  }, [])

  const entradaEstoque = useCallback(async (req: EntradaEstoqueRequest) => {
    const result = await api.post<ProdutoResponse>('/api/estoque/movimentar', req)
    setProdutos(prev => prev.map(p => p.id === req.produtoId ? result : p))
    return result
  }, [])

  const listMovimentacoes = useCallback(async (produtoId?: string) => {
    const qs = produtoId ? `?produtoId=${produtoId}` : ''
    const data = await api.get<MovimentacaoResponse[]>(`/api/estoque/movimentacoes${qs}`)
    setMovimentacoes(data)
  }, [])

  return {
    produtos, categorias, movimentacoes, loading, error,
    listProdutos, listCategorias, createProduto, updateProduto,
    entradaEstoque, listMovimentacoes,
  }
}
```

- [ ] **Step 4: Criar src/hooks/useClientes.ts**

`src/hooks/useClientes.ts`:
```ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { ClienteResponse, CreateClienteRequest, UpdateClienteRequest } from '@/types/clientes'

export function useClientes() {
  const [clientes, setClientes] = useState<ClienteResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (busca?: string) => {
    setLoading(true)
    try {
      const qs = busca ? `?busca=${encodeURIComponent(busca)}` : ''
      const data = await api.get<ClienteResponse[]>(`/api/clientes${qs}`)
      setClientes(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar clientes')
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CreateClienteRequest) => {
    const result = await api.post<ClienteResponse>('/api/clientes', req)
    setClientes(prev => [...prev, result])
    return result
  }, [])

  const update = useCallback(async (id: string, req: UpdateClienteRequest) => {
    const result = await api.put<ClienteResponse>(`/api/clientes/${id}`, req)
    setClientes(prev => prev.map(c => c.id === id ? result : c))
    return result
  }, [])

  return { clientes, loading, error, list, create, update }
}
```

- [ ] **Step 5: Verificar build TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npm run build
```

Expected: Sem erros TypeScript.

---

## Task 7: Páginas de Estoque

**Files:**
- Create: `src/components/estoque/ProdutoForm.tsx`
- Create: `src/components/estoque/EntradaEstoqueDialog.tsx`
- Create: `src/pages/estoque/Produtos.tsx`
- Create: `src/pages/estoque/Movimentacoes.tsx`

- [ ] **Step 1: Criar ProdutoForm.tsx**

`src/components/estoque/ProdutoForm.tsx`:
```tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CategoriaResponse, CreateProdutoRequest } from '@/types/estoque'

const schema = z.object({
  categoriaId: z.string().min(1, 'Selecione uma categoria'),
  nome: z.string().min(1, 'Nome obrigatório').max(200),
  descricao: z.string().optional(),
  precoVenda: z.coerce.number().positive('Preço deve ser maior que zero'),
  custoMedio: z.coerce.number().min(0),
  estoqueAtual: z.coerce.number().min(0),
  estoqueMinimo: z.coerce.number().min(0),
  codigoBarras: z.string().optional(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  categorias: CategoriaResponse[]
  defaultValues?: Partial<FormValues>
  onSubmit: (data: CreateProdutoRequest) => Promise<void>
  onCancel: () => void
}

export default function ProdutoForm({ categorias, defaultValues, onSubmit, onCancel }: Props) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({ resolver: zodResolver(schema), defaultValues })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Categoria</Label>
        <select {...register('categoriaId')}
          className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
          <option value="">Selecione...</option>
          {categorias.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
        </select>
        {errors.categoriaId && <p className="text-xs text-destructive">{errors.categoriaId.message}</p>}
      </div>

      <div className="grid gap-2">
        <Label>Nome</Label>
        <Input {...register('nome')} placeholder="Nome do produto" />
        {errors.nome && <p className="text-xs text-destructive">{errors.nome.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Preço de Venda (R$)</Label>
          <Input type="number" step="0.01" {...register('precoVenda')} />
          {errors.precoVenda && <p className="text-xs text-destructive">{errors.precoVenda.message}</p>}
        </div>
        <div className="grid gap-2">
          <Label>Custo Médio (R$)</Label>
          <Input type="number" step="0.01" {...register('custoMedio')} />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="grid gap-2">
          <Label>Estoque Inicial</Label>
          <Input type="number" step="0.01" {...register('estoqueAtual')} />
        </div>
        <div className="grid gap-2">
          <Label>Estoque Mínimo</Label>
          <Input type="number" step="0.01" {...register('estoqueMinimo')} />
        </div>
      </div>

      <div className="grid gap-2">
        <Label>Código de Barras (opcional)</Label>
        <Input {...register('codigoBarras')} placeholder="EAN-13" />
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

- [ ] **Step 2: Criar EntradaEstoqueDialog.tsx**

`src/components/estoque/EntradaEstoqueDialog.tsx`:
```tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { ProdutoResponse } from '@/types/estoque'

const schema = z.object({
  quantidade: z.coerce.number().positive('Quantidade deve ser maior que zero'),
  custoUnitario: z.coerce.number().min(0).optional(),
  observacao: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

interface Props {
  produto: ProdutoResponse | null
  open: boolean
  onClose: () => void
  onConfirm: (produtoId: string, data: FormValues) => Promise<void>
}

export default function EntradaEstoqueDialog({ produto, open, onClose, onConfirm }: Props) {
  const { register, handleSubmit, reset, formState: { errors, isSubmitting } } =
    useForm<FormValues>({ resolver: zodResolver(schema) })

  async function handleConfirm(data: FormValues) {
    if (!produto) return
    await onConfirm(produto.id, data)
    reset()
    onClose()
  }

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Entrada de Estoque — {produto?.nome}</DialogTitle>
        </DialogHeader>
        <form onSubmit={handleSubmit(handleConfirm)} className="space-y-4">
          <div className="grid gap-2">
            <Label>Quantidade</Label>
            <Input type="number" step="0.01" {...register('quantidade')} autoFocus />
            {errors.quantidade && <p className="text-xs text-destructive">{errors.quantidade.message}</p>}
          </div>
          <div className="grid gap-2">
            <Label>Custo Unitário (R$) — opcional</Label>
            <Input type="number" step="0.01" {...register('custoUnitario')} />
          </div>
          <div className="grid gap-2">
            <Label>Observação</Label>
            <Input {...register('observacao')} placeholder="Ex: Reposição de fornecedor" />
          </div>
          <div className="flex justify-end gap-2">
            <Button type="button" variant="outline" onClick={onClose}>Cancelar</Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Salvando...' : 'Confirmar Entrada'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  )
}
```

- [ ] **Step 3: Instalar componentes shadcn necessários**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npx shadcn@latest add button input label dialog badge table
```

Responder "yes" para todas as perguntas (componentes serão criados em `src/components/ui/`).

- [ ] **Step 4: Criar pages/estoque/Produtos.tsx**

`src/pages/estoque/Produtos.tsx`:
```tsx
import { useEffect, useState } from 'react'
import { Plus, PackageX, ArrowDownToLine } from 'lucide-react'
import { useEstoque } from '@/hooks/useEstoque'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ProdutoForm from '@/components/estoque/ProdutoForm'
import EntradaEstoqueDialog from '@/components/estoque/EntradaEstoqueDialog'
import type { ProdutoResponse, CreateProdutoRequest } from '@/types/estoque'

export default function Produtos() {
  const { produtos, categorias, loading, listProdutos, listCategorias, createProduto, entradaEstoque } = useEstoque()
  const [busca, setBusca] = useState('')
  const [modalAberto, setModalAberto] = useState(false)
  const [produtoEntrada, setProdutoEntrada] = useState<ProdutoResponse | null>(null)

  useEffect(() => { listProdutos(); listCategorias() }, [])

  async function handleCreate(data: CreateProdutoRequest) {
    await createProduto(data)
    setModalAberto(false)
  }

  const produtosFiltrados = produtos.filter(p =>
    p.nome.toLowerCase().includes(busca.toLowerCase()) ||
    (p.codigoBarras?.includes(busca) ?? false))

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Produtos</h1>
        <Button onClick={() => setModalAberto(true)}>
          <Plus size={16} className="mr-2" /> Novo Produto
        </Button>
      </div>

      <Input
        placeholder="Buscar por nome ou código de barras..."
        value={busca}
        onChange={e => setBusca(e.target.value)}
        className="max-w-sm"
      />

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Nome</th>
                <th className="px-4 py-3 text-left font-medium">Categoria</th>
                <th className="px-4 py-3 text-right font-medium">Preço</th>
                <th className="px-4 py-3 text-right font-medium">Estoque</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {produtosFiltrados.map(p => (
                <tr key={p.id} className="border-b">
                  <td className="px-4 py-3 font-medium">{p.nome}</td>
                  <td className="px-4 py-3 text-muted-foreground">{p.categoriaNome}</td>
                  <td className="px-4 py-3 text-right">
                    {p.precoVenda.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}
                  </td>
                  <td className="px-4 py-3 text-right">{p.estoqueAtual}</td>
                  <td className="px-4 py-3">
                    {p.estoqueBaixo ? (
                      <Badge variant="destructive" className="gap-1">
                        <PackageX size={12} /> Estoque baixo
                      </Badge>
                    ) : (
                      <Badge variant="secondary">OK</Badge>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <Button size="sm" variant="outline" onClick={() => setProdutoEntrada(p)}>
                      <ArrowDownToLine size={14} className="mr-1" /> Entrada
                    </Button>
                  </td>
                </tr>
              ))}
              {produtosFiltrados.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum produto encontrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={modalAberto} onOpenChange={setModalAberto}>
        <DialogContent className="max-w-lg">
          <DialogHeader><DialogTitle>Novo Produto</DialogTitle></DialogHeader>
          <ProdutoForm
            categorias={categorias}
            onSubmit={handleCreate}
            onCancel={() => setModalAberto(false)}
          />
        </DialogContent>
      </Dialog>

      <EntradaEstoqueDialog
        produto={produtoEntrada}
        open={!!produtoEntrada}
        onClose={() => setProdutoEntrada(null)}
        onConfirm={async (id, data) => {
          await entradaEstoque({ produtoId: id, ...data })
          setProdutoEntrada(null)
        }}
      />
    </div>
  )
}
```

- [ ] **Step 5: Criar pages/estoque/Movimentacoes.tsx**

`src/pages/estoque/Movimentacoes.tsx`:
```tsx
import { useEffect } from 'react'
import { useEstoque } from '@/hooks/useEstoque'
import { Badge } from '@/components/ui/badge'

export default function Movimentacoes() {
  const { movimentacoes, loading, listMovimentacoes } = useEstoque()

  useEffect(() => { listMovimentacoes() }, [])

  return (
    <div className="space-y-4">
      <h1 className="text-2xl font-bold">Movimentações de Estoque</h1>

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Produto</th>
                <th className="px-4 py-3 text-left font-medium">Tipo</th>
                <th className="px-4 py-3 text-right font-medium">Quantidade</th>
                <th className="px-4 py-3 text-left font-medium">Origem</th>
                <th className="px-4 py-3 text-left font-medium">Data</th>
                <th className="px-4 py-3 text-left font-medium">Observação</th>
              </tr>
            </thead>
            <tbody>
              {movimentacoes.map(m => (
                <tr key={m.id} className="border-b">
                  <td className="px-4 py-3 font-medium">{m.produtoNome}</td>
                  <td className="px-4 py-3">
                    <Badge variant={m.tipo === 'Entrada' ? 'secondary' : 'destructive'}>
                      {m.tipo}
                    </Badge>
                  </td>
                  <td className="px-4 py-3 text-right">{m.quantidade}</td>
                  <td className="px-4 py-3 text-muted-foreground">{m.origem}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {new Date(m.dataHora).toLocaleString('pt-BR')}
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{m.observacao ?? '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
```

---

## Task 8: Página de Clientes

**Files:**
- Create: `src/components/clientes/ClienteForm.tsx`
- Create: `src/pages/clientes/Clientes.tsx`

- [ ] **Step 1: Criar ClienteForm.tsx**

`src/components/clientes/ClienteForm.tsx`:
```tsx
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { CreateClienteRequest } from '@/types/clientes'

const schema = z.object({
  nome: z.string().min(1, 'Nome obrigatório').max(200),
  whatsapp: z.string().min(8, 'WhatsApp obrigatório').max(20),
  email: z.string().email('E-mail inválido').optional().or(z.literal('')),
  observacoes: z.string().optional(),
})
type FormValues = z.infer<typeof schema>

interface Props {
  defaultValues?: Partial<FormValues>
  onSubmit: (data: CreateClienteRequest) => Promise<void>
  onCancel: () => void
}

export default function ClienteForm({ defaultValues, onSubmit, onCancel }: Props) {
  const { register, handleSubmit, formState: { errors, isSubmitting } } =
    useForm<FormValues>({ resolver: zodResolver(schema), defaultValues })

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid gap-2">
        <Label>Nome</Label>
        <Input {...register('nome')} placeholder="Nome completo" />
        {errors.nome && <p className="text-xs text-destructive">{errors.nome.message}</p>}
      </div>
      <div className="grid gap-2">
        <Label>WhatsApp</Label>
        <Input {...register('whatsapp')} placeholder="11999990000" />
        {errors.whatsapp && <p className="text-xs text-destructive">{errors.whatsapp.message}</p>}
      </div>
      <div className="grid gap-2">
        <Label>E-mail (opcional)</Label>
        <Input {...register('email')} type="email" placeholder="email@exemplo.com" />
        {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
      </div>
      <div className="grid gap-2">
        <Label>Observações</Label>
        <Input {...register('observacoes')} placeholder="Anotações rápidas" />
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

- [ ] **Step 2: Criar pages/clientes/Clientes.tsx**

`src/pages/clientes/Clientes.tsx`:
```tsx
import { useEffect, useState } from 'react'
import { Plus, Search } from 'lucide-react'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import ClienteForm from '@/components/clientes/ClienteForm'
import type { CreateClienteRequest } from '@/types/clientes'

export default function Clientes() {
  const { clientes, loading, list, create } = useClientes()
  const [busca, setBusca] = useState('')
  const [modalAberto, setModalAberto] = useState(false)

  useEffect(() => { list() }, [])

  async function handleCreate(data: CreateClienteRequest) {
    await create(data)
    setModalAberto(false)
  }

  const filtrados = clientes.filter(c =>
    c.nome.toLowerCase().includes(busca.toLowerCase()) ||
    c.whatsapp.includes(busca))

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Clientes</h1>
        <Button onClick={() => setModalAberto(true)}>
          <Plus size={16} className="mr-2" /> Novo Cliente
        </Button>
      </div>

      <div className="relative max-w-sm">
        <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Buscar por nome ou WhatsApp..."
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
                <th className="px-4 py-3 text-left font-medium">WhatsApp</th>
                <th className="px-4 py-3 text-left font-medium">E-mail</th>
                <th className="px-4 py-3 text-left font-medium">Cadastrado em</th>
              </tr>
            </thead>
            <tbody>
              {filtrados.map(c => (
                <tr key={c.id} className="border-b hover:bg-muted/30 cursor-pointer">
                  <td className="px-4 py-3 font-medium">{c.nome}</td>
                  <td className="px-4 py-3 text-muted-foreground">{c.whatsapp}</td>
                  <td className="px-4 py-3 text-muted-foreground">{c.email ?? '—'}</td>
                  <td className="px-4 py-3 text-muted-foreground">
                    {new Date(c.dataCadastro).toLocaleDateString('pt-BR')}
                  </td>
                </tr>
              ))}
              {filtrados.length === 0 && (
                <tr>
                  <td colSpan={4} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum cliente encontrado
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      <Dialog open={modalAberto} onOpenChange={setModalAberto}>
        <DialogContent>
          <DialogHeader><DialogTitle>Novo Cliente</DialogTitle></DialogHeader>
          <ClienteForm onSubmit={handleCreate} onCancel={() => setModalAberto(false)} />
        </DialogContent>
      </Dialog>
    </div>
  )
}
```

---

## Task 9: Wiring de rotas e verificação final

**Files:**
- Modify: `src/router/index.tsx`

- [ ] **Step 1: Atualizar router com as novas páginas**

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

export const router = createBrowserRouter([
  { path: '/auth', element: <Auth /> },
  { path: '/auth/callback', element: <AuthCallback /> },
  {
    element: <AppLayout />,
    children: [
      { path: '/', element: <Dashboard /> },
      { path: '/vendas', element: <div>Vendas — Plano 3</div> },
      { path: '/estoque', element: <Produtos /> },
      { path: '/estoque/movimentacoes', element: <Movimentacoes /> },
      { path: '/financeiro', element: <div>Financeiro — Plano 4</div> },
      { path: '/clientes', element: <Clientes /> },
      { path: '/relatorios', element: <div>Relatórios — Plano 5</div> },
    ],
  },
])
```

- [ ] **Step 2: Verificar build TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
npm run build
```

Expected: Sem erros.

- [ ] **Step 3: Rodar todos os testes**

```bash
npx vitest run
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
dotnet test
```

Expected: Todos os testes passando nos dois projetos.

- [ ] **Step 4: Atualizar Sidebar com subrotas de estoque**

Em `src/components/layout/Sidebar.tsx`, substituir o link de Estoque por:
```tsx
const links = [
  { to: '/', icon: LayoutDashboard, label: 'Dashboard' },
  { to: '/vendas', icon: ShoppingCart, label: 'Vendas' },
  { to: '/estoque', icon: Package, label: 'Produtos' },
  { to: '/estoque/movimentacoes', icon: ArrowDownToLine, label: 'Movimentações' },
  { to: '/financeiro', icon: Wallet, label: 'Financeiro' },
  { to: '/clientes', icon: Users, label: 'Clientes' },
  { to: '/relatorios', icon: BarChart3, label: 'Relatórios' },
]
```

Adicionar import: `import { ArrowDownToLine } from 'lucide-react'`

- [ ] **Step 5: Commit final**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp
git add .
git commit -m "feat: Plano 2 completo — Estoque e Clientes funcionais"
```

---

## Checklist de entrega

- [ ] `GET /api/produtos` retorna produtos do tenant com filtro de estoque baixo
- [ ] `POST /api/produtos` cria produto e registra MovimentacaoEstoque inicial
- [ ] `POST /api/estoque/movimentar` atualiza EstoqueAtual e recalcula CustoMedio
- [ ] `GET /api/clientes` retorna clientes do tenant com busca
- [ ] Testes passando: ProdutoServiceTests (4), ClienteServiceTests (3)
- [ ] Página Produtos carrega, filtra, abre modal de criação e entrada de estoque
- [ ] Página Movimentações exibe histórico com tipo e origem
- [ ] Página Clientes carrega, filtra e cria novos clientes
- [ ] Build TypeScript sem erros
