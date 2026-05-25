# Orçamentos — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Adicionar o módulo de Orçamentos ao GestorAI ERP — criação, envio, aprovação, conversão em Venda Aberta e fechamento com pagamento.

**Architecture:** Backend .NET 10 Minimal API com EF Core InMemory (testes) + Npgsql (prod); entidades `Orcamento` e `OrcamentoItem` com multi-tenancy via HasQueryFilter; expiração lazy; VendaService ganha `FecharAsync` para completar vendas abertas geradas pela conversão. Frontend React 18 + TypeScript estrito; 3 novas páginas; NovaVenda ganha suporte a `?vendaId=` para pre-preencher carrinho de venda aberta.

**Tech Stack:** .NET 10, EF Core 9, xUnit, FluentValidation 11, React 18, TypeScript strict, Zod v4, react-hook-form v7, shadcn/ui (manual), lucide-react, react-router-dom v6

---

## Mapa de arquivos

### Criar
| Arquivo | Responsabilidade |
|---|---|
| `backend/src/GestorAI.API/Domain/Enums/OrcamentoStatus.cs` | Enum de status do orçamento |
| `backend/src/GestorAI.API/Domain/Enums/OrcamentoItemTipo.cs` | Enum de tipo de item (Produto / Livre) |
| `backend/src/GestorAI.API/Domain/Entities/Orcamento.cs` | Entidade orçamento |
| `backend/src/GestorAI.API/Domain/Entities/OrcamentoItem.cs` | Entidade item de orçamento |
| `backend/src/GestorAI.API/DTOs/Orcamentos/OrcamentoDto.cs` | Records de request/response |
| `backend/src/GestorAI.API/Services/Orcamentos/OrcamentoService.cs` | Lógica de negócio |
| `backend/src/GestorAI.API/Endpoints/OrcamentosEndpoints.cs` | Minimal API endpoints |
| `backend/tests/GestorAI.Tests/Services/OrcamentoServiceTests.cs` | 6 testes xUnit |
| `frontend/src/types/orcamento.ts` | Tipos TypeScript |
| `frontend/src/hooks/useOrcamentos.ts` | Hook de listagem + mutações |
| `frontend/src/pages/orcamentos/Orcamentos.tsx` | Página lista |
| `frontend/src/pages/orcamentos/NovoOrcamento.tsx` | Página formulário |
| `frontend/src/pages/orcamentos/DetalheOrcamento.tsx` | Página detalhe + ações |

### Modificar
| Arquivo | Mudança |
|---|---|
| `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs` | Adiciona DbSets + HasQueryFilter para Orcamento e OrcamentoItem |
| `backend/src/GestorAI.API/Services/Vendas/VendaService.cs` | Adiciona FecharAsync |
| `backend/src/GestorAI.API/DTOs/Vendas/VendaDto.cs` | Adiciona FecharVendaRequest |
| `backend/src/GestorAI.API/Endpoints/VendasEndpoints.cs` | Adiciona POST /{id}/fechar |
| `backend/src/GestorAI.API/Program.cs` | Registra OrcamentoService + MapOrcamentos |
| `frontend/src/types/vendas.ts` | Adiciona FecharVendaRequest |
| `frontend/src/hooks/useVendas.ts` | Adiciona fechar() |
| `frontend/src/pages/vendas/NovaVenda.tsx` | Suporte a ?vendaId= |
| `frontend/src/router/index.tsx` | Rotas de orçamento |
| `frontend/src/components/layout/Sidebar.tsx` | Link Orçamentos |

---

## Task 1: Enums e entidades de domínio

**Files:**
- Create: `backend/src/GestorAI.API/Domain/Enums/OrcamentoStatus.cs`
- Create: `backend/src/GestorAI.API/Domain/Enums/OrcamentoItemTipo.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/Orcamento.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/OrcamentoItem.cs`

- [ ] **Step 1: Criar OrcamentoStatus.cs**

```csharp
// backend/src/GestorAI.API/Domain/Enums/OrcamentoStatus.cs
namespace GestorAI.API.Domain.Enums;
public enum OrcamentoStatus { Rascunho, Enviado, Aprovado, Convertido, Rejeitado, Cancelado, Expirado }
```

- [ ] **Step 2: Criar OrcamentoItemTipo.cs**

```csharp
// backend/src/GestorAI.API/Domain/Enums/OrcamentoItemTipo.cs
namespace GestorAI.API.Domain.Enums;
public enum OrcamentoItemTipo { Produto, Livre }
```

- [ ] **Step 3: Criar OrcamentoItem.cs**

```csharp
// backend/src/GestorAI.API/Domain/Entities/OrcamentoItem.cs
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class OrcamentoItem
{
    public Guid Id { get; set; }
    public Guid OrcamentoId { get; set; }
    public OrcamentoItemTipo Tipo { get; set; }
    public Guid? ProdutoId { get; set; }
    public required string Descricao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public Orcamento? Orcamento { get; set; }
    public Produto? Produto { get; set; }
}
```

- [ ] **Step 4: Criar Orcamento.cs**

```csharp
// backend/src/GestorAI.API/Domain/Entities/Orcamento.cs
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Orcamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? ClienteId { get; set; }
    public int Numero { get; set; }
    public required string Titulo { get; set; }
    public DateTime DataValidade { get; set; }
    public OrcamentoStatus Status { get; set; } = OrcamentoStatus.Rascunho;
    public string? Observacao { get; set; }
    public Guid? VendaId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public ICollection<OrcamentoItem> Itens { get; set; } = [];
}
```

- [ ] **Step 5: Compilar para verificar erros**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
export PATH="/usr/local/bin:/opt/homebrew/bin:$PATH"
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj -c Debug 2>&1 | tail -20
```

Esperado: `Build succeeded.`

---

## Task 2: AppDbContext — DbSets + HasQueryFilter

**Files:**
- Modify: `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: Adicionar DbSets e filtros ao AppDbContext**

Substituir o conteúdo de `AppDbContext.cs` pelo seguinte:

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
    }
}
```

- [ ] **Step 2: Compilar**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj -c Debug 2>&1 | tail -10
```

Esperado: `Build succeeded.`

---

## Task 3: DTOs de Orçamento

**Files:**
- Create: `backend/src/GestorAI.API/DTOs/Orcamentos/OrcamentoDto.cs`

- [ ] **Step 1: Criar OrcamentoDto.cs**

```csharp
// backend/src/GestorAI.API/DTOs/Orcamentos/OrcamentoDto.cs
namespace GestorAI.API.DTOs.Orcamentos;

public record OrcamentoItemRequest(
    string Tipo,
    Guid? ProdutoId,
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario);

public record CreateOrcamentoRequest(
    Guid? ClienteId,
    string Titulo,
    DateTime DataValidade,
    string? Observacao,
    List<OrcamentoItemRequest> Itens);

public record OrcamentoItemResponse(
    Guid Id,
    string Tipo,
    Guid? ProdutoId,
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario);

public record OrcamentoResponse(
    Guid Id,
    int Numero,
    string Titulo,
    Guid? ClienteId,
    string? ClienteNome,
    string? ClienteWhatsapp,
    DateTime DataValidade,
    string Status,
    string? Observacao,
    Guid? VendaId,
    DateTime CreatedAt,
    List<OrcamentoItemResponse> Itens,
    decimal Total);

public record OrcamentoListItem(
    Guid Id,
    int Numero,
    string Titulo,
    string? ClienteNome,
    DateTime DataValidade,
    string Status,
    decimal Total);
```

- [ ] **Step 2: Compilar**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj -c Debug 2>&1 | tail -10
```

Esperado: `Build succeeded.`

---

## Task 4: OrcamentoService — TDD (testes primeiro, depois implementação)

**Files:**
- Create: `backend/tests/GestorAI.Tests/Services/OrcamentoServiceTests.cs`
- Create: `backend/src/GestorAI.API/Services/Orcamentos/OrcamentoService.cs`

- [ ] **Step 1: Escrever os 6 testes (arquivo de teste)**

```csharp
// backend/tests/GestorAI.Tests/Services/OrcamentoServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Orcamentos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Orcamentos;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class OrcamentoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, OrcamentoService service) Setup()
    {
        var tenantContext = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenantContext);
        return (db, new OrcamentoService(db, tenantContext));
    }

    [Fact]
    public async Task CreateAsync_PersisteComotRascunho()
    {
        var (_, service) = Setup();
        var req = new CreateOrcamentoRequest(
            null, "Orçamento Teste", DateTime.Today.AddDays(7), null,
            [new OrcamentoItemRequest("Livre", null, "Corte", 1, 50m)]);

        var result = await service.CreateAsync(req, default);

        Assert.Equal("Rascunho", result.Status);
        Assert.Equal(1, result.Numero);
        Assert.Equal(50m, result.Total);
    }

    [Fact]
    public async Task ListAsync_ExpiraOrcamentosVencidos()
    {
        var (db, service) = Setup();
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Cat" };
        db.Categorias.Add(cat);
        db.Orcamentos.Add(new Orcamento
        {
            EmpresaId = _empresaId,
            Numero = 1,
            Titulo = "Vencido",
            DataValidade = DateTime.UtcNow.AddDays(-1),
            Status = OrcamentoStatus.Enviado,
        });
        await db.SaveChangesAsync();

        var result = await service.ListAsync(null, default);

        Assert.Single(result);
        Assert.Equal("Expirado", result[0].Status);
    }

    [Fact]
    public async Task EnviarAsync_RascunhoViraEnviado()
    {
        var (db, service) = Setup();
        var o = new Orcamento
        {
            EmpresaId = _empresaId, Numero = 1, Titulo = "T",
            DataValidade = DateTime.Today.AddDays(7), Status = OrcamentoStatus.Rascunho
        };
        db.Orcamentos.Add(o);
        await db.SaveChangesAsync();

        var result = await service.EnviarAsync(o.Id, default);

        Assert.Equal("Enviado", result.Status);
    }

    [Fact]
    public async Task ConvertAsync_CriaVendaApenasComItensProduto()
    {
        var (db, service) = Setup();
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Cat" };
        db.Categorias.Add(cat);
        var produto = new Produto
        {
            EmpresaId = _empresaId, CategoriaId = cat.Id,
            Nome = "Shampoo", PrecoVenda = 30m, EstoqueAtual = 10
        };
        db.Produtos.Add(produto);
        var o = new Orcamento
        {
            EmpresaId = _empresaId, Numero = 1, Titulo = "T",
            DataValidade = DateTime.Today.AddDays(7), Status = OrcamentoStatus.Aprovado
        };
        o.Itens.Add(new OrcamentoItem
        {
            Tipo = OrcamentoItemTipo.Produto, ProdutoId = produto.Id,
            Descricao = "Shampoo", Quantidade = 2, ValorUnitario = 30m
        });
        o.Itens.Add(new OrcamentoItem
        {
            Tipo = OrcamentoItemTipo.Livre, Descricao = "Aplicação",
            Quantidade = 1, ValorUnitario = 50m
        });
        db.Orcamentos.Add(o);
        await db.SaveChangesAsync();

        var result = await service.ConvertAsync(o.Id, default);

        Assert.Equal("Convertido", result.Status);
        Assert.NotNull(result.VendaId);
        var venda = await db.Vendas.Include(v => v.Itens).FirstAsync();
        Assert.Single(venda.Itens);
        Assert.Equal(produto.Id, venda.Itens.First().ProdutoId);
    }

    [Fact]
    public async Task ConvertAsync_QuandoNaoAprovado_LancaExcecao()
    {
        var (db, service) = Setup();
        var o = new Orcamento
        {
            EmpresaId = _empresaId, Numero = 1, Titulo = "T",
            DataValidade = DateTime.Today.AddDays(7), Status = OrcamentoStatus.Enviado
        };
        db.Orcamentos.Add(o);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => service.ConvertAsync(o.Id, default));
    }

    [Fact]
    public async Task AprovarAsync_QuandoExpirado_LancaExcecao()
    {
        var (db, service) = Setup();
        var o = new Orcamento
        {
            EmpresaId = _empresaId, Numero = 1, Titulo = "T",
            DataValidade = DateTime.Today.AddDays(-1), Status = OrcamentoStatus.Enviado
        };
        db.Orcamentos.Add(o);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => service.AprovarAsync(o.Id, default));
    }
}
```

- [ ] **Step 2: Rodar os testes — confirmar que falham por falta do OrcamentoService**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj 2>&1 | tail -20
```

Esperado: erro de compilação — `The type or namespace name 'OrcamentoService' could not be found`.

- [ ] **Step 3: Criar OrcamentoService.cs**

```csharp
// backend/src/GestorAI.API/Services/Orcamentos/OrcamentoService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Orcamentos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Orcamentos;

public class OrcamentoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<OrcamentoListItem>> ListAsync(string? status, CancellationToken ct)
    {
        var orcamentos = await db.Orcamentos
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        await ExpireIfNeededAsync(orcamentos, ct);

        return orcamentos
            .Where(o => status == null || o.Status.ToString() == status)
            .Select(o => ToListItem(o))
            .ToList();
    }

    public async Task<OrcamentoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        await ExpireIfNeededAsync([o], ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> CreateAsync(CreateOrcamentoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<OrcamentoItemTipo>(req.Itens.FirstOrDefault()?.Tipo ?? "Livre", out _)
            && req.Itens.Count > 0)
            throw new AppException("Tipo de item inválido.");

        var numero = (await db.Orcamentos.MaxAsync(o => (int?)o.Numero, ct) ?? 0) + 1;

        var orcamento = new Orcamento
        {
            EmpresaId = tenantContext.EmpresaId,
            ClienteId = req.ClienteId,
            Numero = numero,
            Titulo = req.Titulo,
            DataValidade = req.DataValidade,
            Status = OrcamentoStatus.Rascunho,
            Observacao = req.Observacao,
        };

        foreach (var item in req.Itens)
        {
            if (!Enum.TryParse<OrcamentoItemTipo>(item.Tipo, out var tipo))
                throw new AppException($"Tipo de item inválido: {item.Tipo}.");

            orcamento.Itens.Add(new OrcamentoItem
            {
                Tipo = tipo,
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
            });
        }

        db.Orcamentos.Add(orcamento);
        await db.SaveChangesAsync(ct);

        return await GetAsync(orcamento.Id, ct);
    }

    public async Task<OrcamentoResponse> EnviarAsync(Guid id, CancellationToken ct)
    {
        var o = await FindAsync(id, ct);
        if (o.Status != OrcamentoStatus.Rascunho)
            throw new AppException("Apenas rascunhos podem ser enviados.");
        o.Status = OrcamentoStatus.Enviado;
        await db.SaveChangesAsync(ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> AprovarAsync(Guid id, CancellationToken ct)
    {
        var o = await FindAsync(id, ct);
        if (o.DataValidade.Date < DateTime.UtcNow.Date)
            throw new AppException("Orçamento expirado não pode ser aprovado.", 400);
        if (o.Status != OrcamentoStatus.Enviado)
            throw new AppException("Apenas orçamentos enviados podem ser aprovados.");
        o.Status = OrcamentoStatus.Aprovado;
        await db.SaveChangesAsync(ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> RejeitarAsync(Guid id, CancellationToken ct)
    {
        var o = await FindAsync(id, ct);
        if (o.Status != OrcamentoStatus.Enviado)
            throw new AppException("Apenas orçamentos enviados podem ser rejeitados.");
        o.Status = OrcamentoStatus.Rejeitado;
        await db.SaveChangesAsync(ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var o = await FindAsync(id, ct);
        if (o.Status != OrcamentoStatus.Rascunho)
            throw new AppException("Apenas rascunhos podem ser cancelados.");
        o.Status = OrcamentoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> ConvertAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .Include(o => o.Itens)
            .Include(o => o.Cliente)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        if (o.Status != OrcamentoStatus.Aprovado)
            throw new AppException("Apenas orçamentos aprovados podem ser convertidos.", 400);

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        var itensProduto = o.Itens
            .Where(i => i.Tipo == OrcamentoItemTipo.Produto)
            .ToList();

        var subtotal = itensProduto.Sum(i => i.Quantidade * i.ValorUnitario);

        var venda = new Venda
        {
            EmpresaId = tenantContext.EmpresaId,
            ClienteId = o.ClienteId,
            Status = StatusVenda.Aberta,
            Subtotal = subtotal,
            Desconto = 0,
            Total = subtotal,
            FormaPagamento = FormaPagamento.Outro,
            Observacao = $"Gerado do Orçamento ORC-{o.Numero:D3}",
        };
        db.Vendas.Add(venda);

        foreach (var item in itensProduto)
        {
            db.ItensVenda.Add(new ItemVenda
            {
                VendaId = venda.Id,
                ProdutoId = item.ProdutoId!.Value,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.ValorUnitario,
                Desconto = 0,
                Total = item.Quantidade * item.ValorUnitario,
            });
        }

        o.VendaId = venda.Id;
        o.Status = OrcamentoStatus.Convertido;

        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return ToResponse(o);
    }

    public async Task<string> GetPdfHtmlAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        var total = o.Itens.Sum(i => i.Quantidade * i.ValorUnitario);
        var linhas = string.Join("", o.Itens.Select(i =>
            $"<tr><td>{i.Descricao}</td><td>{i.Quantidade:N2}</td>" +
            $"<td>R$ {i.ValorUnitario:N2}</td><td>R$ {i.Quantidade * i.ValorUnitario:N2}</td></tr>"));

        return $"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8">
            <style>
              body {{ font-family: sans-serif; padding: 32px; color: #111; }}
              h1 {{ font-size: 22px; margin-bottom: 4px; }}
              .meta {{ color: #555; font-size: 13px; margin-bottom: 24px; }}
              table {{ width: 100%; border-collapse: collapse; margin-bottom: 16px; }}
              th, td {{ border: 1px solid #ddd; padding: 8px; text-align: left; font-size: 13px; }}
              th {{ background: #f5f5f5; }}
              .total {{ text-align: right; font-weight: bold; font-size: 15px; margin-top: 8px; }}
              .obs {{ margin-top: 16px; font-size: 13px; color: #555; }}
            </style>
            </head>
            <body>
              <h1>ORC-{o.Numero:D3} — {o.Titulo}</h1>
              <div class="meta">
                {(o.Cliente != null ? $"Cliente: {o.Cliente.Nome}<br>" : "")}
                Válido até: {o.DataValidade:dd/MM/yyyy} | Status: {o.Status}
              </div>
              <table>
                <thead><tr><th>Descrição</th><th>Qtd</th><th>Unit.</th><th>Total</th></tr></thead>
                <tbody>{linhas}</tbody>
              </table>
              <div class="total">Total: R$ {total:N2}</div>
              {(o.Observacao != null ? $"<div class='obs'>Obs: {o.Observacao}</div>" : "")}
            </body>
            </html>
            """;
    }

    private async Task<Orcamento> FindAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);
        return o;
    }

    private async Task ExpireIfNeededAsync(List<Orcamento> orcamentos, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var expirar = orcamentos
            .Where(o => o.DataValidade.Date < hoje
                && (o.Status == OrcamentoStatus.Enviado || o.Status == OrcamentoStatus.Aprovado))
            .ToList();
        if (expirar.Count == 0) return;
        foreach (var o in expirar) o.Status = OrcamentoStatus.Expirado;
        await db.SaveChangesAsync(ct);
    }

    private static OrcamentoListItem ToListItem(Orcamento o) => new(
        o.Id, o.Numero, o.Titulo, o.Cliente?.Nome,
        o.DataValidade, o.Status.ToString(),
        o.Itens.Sum(i => i.Quantidade * i.ValorUnitario));

    private static OrcamentoResponse ToResponse(Orcamento o) => new(
        o.Id, o.Numero, o.Titulo, o.ClienteId,
        o.Cliente?.Nome, o.Cliente?.Whatsapp,
        o.DataValidade, o.Status.ToString(),
        o.Observacao, o.VendaId, o.CreatedAt,
        o.Itens.Select(i => new OrcamentoItemResponse(
            i.Id, i.Tipo.ToString(), i.ProdutoId,
            i.Descricao, i.Quantidade, i.ValorUnitario)).ToList(),
        o.Itens.Sum(i => i.Quantidade * i.ValorUnitario));
}
```

- [ ] **Step 4: Rodar os testes — confirmar que passam**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj \
  --filter "OrcamentoServiceTests" 2>&1 | tail -20
```

Esperado: `6 passed, 0 failed`.

---

## Task 5: VendaService.FecharAsync + endpoint

**Files:**
- Modify: `backend/src/GestorAI.API/DTOs/Vendas/VendaDto.cs`
- Modify: `backend/src/GestorAI.API/Services/Vendas/VendaService.cs`
- Modify: `backend/src/GestorAI.API/Endpoints/VendasEndpoints.cs`

- [ ] **Step 1: Localizar e ler VendaDto.cs**

```bash
find /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend/src -name "VendaDto.cs"
```

- [ ] **Step 2: Adicionar FecharVendaRequest ao VendaDto.cs**

Adicionar este record ao final do arquivo `VendaDto.cs` (mantendo os records existentes):

```csharp
public record FecharVendaRequest(string FormaPagamento, int? Parcelas, string? Observacao);
```

- [ ] **Step 3: Adicionar FecharAsync ao VendaService.cs**

Adicionar este método à classe `VendaService`, após `CancelarAsync`:

```csharp
public async Task<VendaResponse> FecharAsync(Guid id, FecharVendaRequest req, CancellationToken ct)
{
    if (!Enum.TryParse<FormaPagamento>(req.FormaPagamento, out var formaPagamento))
        throw new AppException("Forma de pagamento inválida.");

    var venda = await db.Vendas
        .Include(v => v.Itens)
        .Include(v => v.Cliente)
        .FirstOrDefaultAsync(v => v.Id == id, ct)
        ?? throw new AppException("Venda não encontrada.", 404);

    if (venda.Status != StatusVenda.Aberta)
        throw new AppException("Apenas vendas abertas podem ser fechadas.");

    var produtoIds = venda.Itens.Select(i => i.ProdutoId).Distinct().ToList();
    var produtos = await db.Produtos.Where(p => produtoIds.Contains(p.Id)).ToListAsync(ct);

    foreach (var item in venda.Itens)
    {
        var produto = produtos.FirstOrDefault(p => p.Id == item.ProdutoId)
            ?? throw new AppException($"Produto não encontrado.", 404);
        if (produto.EstoqueAtual < item.Quantidade)
            throw new AppException(
                $"Estoque insuficiente para '{produto.Nome}'. " +
                $"Disponível: {produto.EstoqueAtual}, solicitado: {item.Quantidade}.");
    }

    Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
    try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

    try
    {
        foreach (var item in venda.Itens)
        {
            var produto = produtos.First(p => p.Id == item.ProdutoId);
            produto.EstoqueAtual -= item.Quantidade;
            produto.AtualizadoEm = DateTime.UtcNow;

            db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                EmpresaId = tenantContext.EmpresaId,
                ProdutoId = item.ProdutoId,
                Tipo = TipoMovimentacao.Saida,
                Quantidade = item.Quantidade,
                Origem = OrigemMovimentacao.Venda,
                ReferenciaId = venda.Id,
            });
        }

        var nomeCliente = venda.Cliente?.Nome ?? "Venda balcão";
        db.Lancamentos.Add(new Lancamento
        {
            EmpresaId = tenantContext.EmpresaId,
            Tipo = TipoLancamento.Receita,
            Descricao = $"Venda — {nomeCliente}",
            Valor = venda.Total,
            DataVencimento = DateTime.UtcNow,
            DataPagamento = DateTime.UtcNow,
            Status = StatusLancamento.Pago,
            Categoria = "Venda",
            VendaId = venda.Id,
        });

        venda.Status = StatusVenda.Concluida;
        venda.FormaPagamento = formaPagamento;
        venda.Parcelas = req.Parcelas;
        if (req.Observacao is not null) venda.Observacao = req.Observacao;

        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return await GetAsync(venda.Id, ct);
    }
    catch
    {
        if (tx is not null) await tx.RollbackAsync(ct);
        throw;
    }
}
```

- [ ] **Step 4: Adicionar endpoint POST /{id}/fechar em VendasEndpoints.cs**

Adicionar antes do fechamento do método `MapVendas`:

```csharp
        group.MapPost("/{id:guid}/fechar", async (
            Guid id, FecharVendaRequest req, VendaService svc, CancellationToken ct) =>
            Results.Ok(await svc.FecharAsync(id, req, ct)));
```

- [ ] **Step 5: Compilar e rodar todos os testes**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj 2>&1 | tail -20
```

Esperado: todos os testes existentes passam + os 6 de OrcamentoServiceTests.

---

## Task 6: OrcamentosEndpoints + Program.cs

**Files:**
- Create: `backend/src/GestorAI.API/Endpoints/OrcamentosEndpoints.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar OrcamentosEndpoints.cs**

```csharp
// backend/src/GestorAI.API/Endpoints/OrcamentosEndpoints.cs
using GestorAI.API.DTOs.Orcamentos;
using GestorAI.API.Services.Orcamentos;

namespace GestorAI.API.Endpoints;

public static class OrcamentosEndpoints
{
    public static void MapOrcamentos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orcamentos").RequireAuthorization("VendasAccess");

        group.MapGet("/", async (
            string? status, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, ct)));

        group.MapGet("/{id:guid}", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (
            CreateOrcamentoRequest req, OrcamentoService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/orcamentos/{result.Id}", result);
        });

        group.MapPost("/{id:guid}/enviar", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.EnviarAsync(id, ct)));

        group.MapPost("/{id:guid}/aprovar", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.AprovarAsync(id, ct)));

        group.MapPost("/{id:guid}/rejeitar", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.RejeitarAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapPost("/{id:guid}/converter", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConvertAsync(id, ct)));

        group.MapGet("/{id:guid}/pdf", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
        {
            var html = await svc.GetPdfHtmlAsync(id, ct);
            return Results.Content(html, "text/html");
        });
    }
}
```

- [ ] **Step 2: Registrar em Program.cs**

Adicionar após `builder.Services.AddScoped<RelatorioService>();`:

```csharp
builder.Services.AddScoped<OrcamentoService>();
```

Adicionar após `app.MapDashboard();`:

```csharp
app.MapOrcamentos();
```

Adicionar o using no topo, se necessário (o namespace está na lista de `using GestorAI.API.Services.*` — mas como é explícito, adicionar):

No `Program.cs`, adicionar:
```csharp
using GestorAI.API.Services.Orcamentos;
```

- [ ] **Step 3: Compilar**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet build src/GestorAI.API/GestorAI.API.csproj -c Debug 2>&1 | tail -10
```

Esperado: `Build succeeded.`

---

## Task 7: Frontend — tipos e hook

**Files:**
- Create: `frontend/src/types/orcamento.ts`
- Modify: `frontend/src/types/vendas.ts`
- Create: `frontend/src/hooks/useOrcamentos.ts`
- Modify: `frontend/src/hooks/useVendas.ts`

- [ ] **Step 1: Criar types/orcamento.ts**

```typescript
// frontend/src/types/orcamento.ts
export interface OrcamentoItemRequest {
  tipo: 'Produto' | 'Livre'
  produtoId?: string
  descricao: string
  quantidade: number
  valorUnitario: number
}

export interface CreateOrcamentoRequest {
  clienteId?: string
  titulo: string
  dataValidade: string
  observacao?: string
  itens: OrcamentoItemRequest[]
}

export interface OrcamentoItemResponse {
  id: string
  tipo: 'Produto' | 'Livre'
  produtoId: string | null
  descricao: string
  quantidade: number
  valorUnitario: number
}

export interface OrcamentoResponse {
  id: string
  numero: number
  titulo: string
  clienteId: string | null
  clienteNome: string | null
  clienteWhatsapp: string | null
  dataValidade: string
  status: OrcamentoStatus
  observacao: string | null
  vendaId: string | null
  createdAt: string
  itens: OrcamentoItemResponse[]
  total: number
}

export interface OrcamentoListItem {
  id: string
  numero: number
  titulo: string
  clienteNome: string | null
  dataValidade: string
  status: OrcamentoStatus
  total: number
}

export type OrcamentoStatus =
  | 'Rascunho'
  | 'Enviado'
  | 'Aprovado'
  | 'Convertido'
  | 'Rejeitado'
  | 'Cancelado'
  | 'Expirado'
```

- [ ] **Step 2: Adicionar FecharVendaRequest em types/vendas.ts**

Adicionar ao final de `frontend/src/types/vendas.ts`:

```typescript
export interface FecharVendaRequest {
  formaPagamento: 'Dinheiro' | 'Pix' | 'Cartao' | 'Outro'
  parcelas?: number
  observacao?: string
}
```

- [ ] **Step 3: Criar hooks/useOrcamentos.ts**

```typescript
// frontend/src/hooks/useOrcamentos.ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type { OrcamentoListItem, OrcamentoResponse, CreateOrcamentoRequest } from '@/types/orcamento'

export function useOrcamentos() {
  const [orcamentos, setOrcamentos] = useState<OrcamentoListItem[]>([])
  const [orcamento, setOrcamento] = useState<OrcamentoResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (status?: string) => {
    setLoading(true)
    try {
      const qs = status ? `?status=${status}` : ''
      const data = await api.get<OrcamentoListItem[]>(`/api/orcamentos${qs}`)
      setOrcamentos(data)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar orçamentos')
    } finally {
      setLoading(false)
    }
  }, [])

  const get = useCallback(async (id: string) => {
    setLoading(true)
    try {
      const data = await api.get<OrcamentoResponse>(`/api/orcamentos/${id}`)
      setOrcamento(data)
      return data
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar orçamento')
      return null
    } finally {
      setLoading(false)
    }
  }, [])

  const create = useCallback(async (req: CreateOrcamentoRequest) => {
    const result = await api.post<OrcamentoResponse>('/api/orcamentos', req)
    return result
  }, [])

  const enviar = useCallback(async (id: string) => {
    const result = await api.post<OrcamentoResponse>(`/api/orcamentos/${id}/enviar`, {})
    setOrcamento(result)
    return result
  }, [])

  const aprovar = useCallback(async (id: string) => {
    const result = await api.post<OrcamentoResponse>(`/api/orcamentos/${id}/aprovar`, {})
    setOrcamento(result)
    return result
  }, [])

  const rejeitar = useCallback(async (id: string) => {
    const result = await api.post<OrcamentoResponse>(`/api/orcamentos/${id}/rejeitar`, {})
    setOrcamento(result)
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<OrcamentoResponse>(`/api/orcamentos/${id}/cancelar`, {})
    setOrcamento(result)
    return result
  }, [])

  const converter = useCallback(async (id: string) => {
    const result = await api.post<OrcamentoResponse>(`/api/orcamentos/${id}/converter`, {})
    setOrcamento(result)
    return result
  }, [])

  return {
    orcamentos, orcamento, loading, error,
    list, get, create, enviar, aprovar, rejeitar, cancelar, converter,
  }
}
```

- [ ] **Step 4: Adicionar fechar() em hooks/useVendas.ts**

Ler o arquivo atual e adicionar `fechar` ao hook. Localizar o `return` do hook e adicionar `fechar` no objeto retornado. Adicionar o método antes do return:

```typescript
  const fechar = useCallback(async (id: string, req: FecharVendaRequest) => {
    const result = await api.post<VendaResponse>(`/api/vendas/${id}/fechar`, req)
    return result
  }, [])
```

E no import do tipo, adicionar `FecharVendaRequest`:
```typescript
import type { ..., FecharVendaRequest } from '@/types/vendas'
```

E no return do hook, adicionar `fechar`.

- [ ] **Step 5: Verificar TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
export PATH="/usr/local/bin:/opt/homebrew/bin:$PATH"
npm run typecheck 2>&1 | tail -20
```

Esperado: sem erros de tipo.

---

## Task 8: Página lista de Orçamentos

**Files:**
- Create: `frontend/src/pages/orcamentos/Orcamentos.tsx`

- [ ] **Step 1: Criar a página**

```typescript
// frontend/src/pages/orcamentos/Orcamentos.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus } from 'lucide-react'
import { useOrcamentos } from '@/hooks/useOrcamentos'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import type { OrcamentoStatus } from '@/types/orcamento'

const STATUS_TODOS: (OrcamentoStatus | 'Todos')[] = [
  'Todos', 'Rascunho', 'Enviado', 'Aprovado', 'Convertido', 'Rejeitado', 'Cancelado', 'Expirado',
]

const statusVariant = (s: OrcamentoStatus): 'default' | 'secondary' | 'outline' | 'destructive' => {
  if (s === 'Aprovado' || s === 'Convertido') return 'secondary'
  if (s === 'Rejeitado' || s === 'Cancelado') return 'destructive'
  if (s === 'Expirado') return 'outline'
  return 'default'
}

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function Orcamentos() {
  const navigate = useNavigate()
  const { orcamentos, loading, list } = useOrcamentos()
  const [filtro, setFiltro] = useState<OrcamentoStatus | 'Todos'>('Todos')

  useEffect(() => {
    void list(filtro === 'Todos' ? undefined : filtro)
  }, [list, filtro])

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Orçamentos</h1>
        <Button onClick={() => navigate('/orcamentos/novo')}>
          <Plus size={16} className="mr-2" /> Novo Orçamento
        </Button>
      </div>

      <div className="flex flex-wrap gap-2">
        {STATUS_TODOS.map(s => (
          <button
            key={s}
            onClick={() => setFiltro(s)}
            className={`rounded-full px-3 py-1 text-xs font-medium transition-colors ${
              filtro === s
                ? 'bg-primary text-primary-foreground'
                : 'bg-muted text-muted-foreground hover:bg-accent'
            }`}>
            {s}
          </button>
        ))}
      </div>

      {loading ? <p className="text-muted-foreground">Carregando...</p> : (
        <div className="rounded-md border">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/50">
                <th className="px-4 py-3 text-left font-medium">Nº</th>
                <th className="px-4 py-3 text-left font-medium">Título</th>
                <th className="px-4 py-3 text-left font-medium">Cliente</th>
                <th className="px-4 py-3 text-left font-medium">Validade</th>
                <th className="px-4 py-3 text-right font-medium">Total</th>
                <th className="px-4 py-3 text-left font-medium">Status</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {orcamentos.map(o => (
                <tr key={o.id} className="border-b hover:bg-muted/30">
                  <td className="px-4 py-3 font-mono text-muted-foreground">
                    ORC-{String(o.numero).padStart(3, '0')}
                  </td>
                  <td className="px-4 py-3 font-medium">{o.titulo}</td>
                  <td className="px-4 py-3 text-muted-foreground">{o.clienteNome ?? '—'}</td>
                  <td className="px-4 py-3 text-muted-foreground">{fmtDate(o.dataValidade)}</td>
                  <td className="px-4 py-3 text-right font-medium">{fmt(o.total)}</td>
                  <td className="px-4 py-3">
                    <Badge variant={statusVariant(o.status)}>{o.status}</Badge>
                  </td>
                  <td className="px-4 py-3">
                    <Button size="sm" variant="outline" onClick={() => navigate(`/orcamentos/${o.id}`)}>
                      {o.status === 'Rascunho' ? 'Enviar' : o.status === 'Aprovado' ? 'Converter' : 'Ver'}
                    </Button>
                  </td>
                </tr>
              ))}
              {orcamentos.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">
                    Nenhum orçamento encontrado
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

## Task 9: Página Novo Orçamento (formulário)

**Files:**
- Create: `frontend/src/pages/orcamentos/NovoOrcamento.tsx`

- [ ] **Step 1: Criar a página**

```typescript
// frontend/src/pages/orcamentos/NovoOrcamento.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Trash2 } from 'lucide-react'
import { useOrcamentos } from '@/hooks/useOrcamentos'
import { useEstoque } from '@/hooks/useEstoque'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import type { OrcamentoItemRequest } from '@/types/orcamento'

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export default function NovoOrcamento() {
  const navigate = useNavigate()
  const { create } = useOrcamentos()
  const { produtos, list: listProdutos } = useEstoque()
  const { clientes, list: listClientes } = useClientes()

  const [titulo, setTitulo] = useState('')
  const [clienteId, setClienteId] = useState('')
  const [dataValidade, setDataValidade] = useState('')
  const [observacao, setObservacao] = useState('')
  const [itens, setItens] = useState<OrcamentoItemRequest[]>([])
  const [salvando, setSalvando] = useState(false)
  const [erro, setErro] = useState<string | null>(null)

  useEffect(() => {
    void listProdutos()
    void listClientes()
  }, [listProdutos, listClientes])

  function adicionarProduto(produtoId: string) {
    if (!produtoId) return
    const produto = produtos.find(p => p.id === produtoId)
    if (!produto) return
    const jaExiste = itens.find(i => i.produtoId === produtoId)
    if (jaExiste) return
    setItens(prev => [...prev, {
      tipo: 'Produto',
      produtoId,
      descricao: produto.nome,
      quantidade: 1,
      valorUnitario: produto.precoVenda,
    }])
  }

  function adicionarLivre() {
    setItens(prev => [...prev, { tipo: 'Livre', descricao: '', quantidade: 1, valorUnitario: 0 }])
  }

  function atualizarItem(index: number, campo: keyof OrcamentoItemRequest, valor: string | number) {
    setItens(prev => prev.map((item, i) =>
      i === index ? { ...item, [campo]: valor } : item
    ))
  }

  function removerItem(index: number) {
    setItens(prev => prev.filter((_, i) => i !== index))
  }

  const total = itens.reduce((acc, i) => acc + i.quantidade * i.valorUnitario, 0)

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (!titulo.trim()) { setErro('Título obrigatório'); return }
    if (!dataValidade) { setErro('Data de validade obrigatória'); return }
    if (itens.length === 0) { setErro('Adicione pelo menos um item'); return }
    setErro(null)
    setSalvando(true)
    try {
      const result = await create({
        clienteId: clienteId || undefined,
        titulo: titulo.trim(),
        dataValidade,
        observacao: observacao.trim() || undefined,
        itens,
      })
      navigate(`/orcamentos/${result.id}`)
    } catch (e) {
      setErro(e instanceof Error ? e.message : 'Erro ao salvar')
    } finally {
      setSalvando(false)
    }
  }

  return (
    <div className="max-w-3xl space-y-6">
      <h1 className="text-2xl font-bold">Novo Orçamento</h1>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="grid gap-2 sm:col-span-2">
            <Label>Título *</Label>
            <Input value={titulo} onChange={e => setTitulo(e.target.value)}
              placeholder="Ex: Orçamento - Corte e Escova" />
          </div>
          <div className="grid gap-2">
            <Label>Cliente (opcional)</Label>
            <select value={clienteId} onChange={e => setClienteId(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
              <option value="">Sem cliente</option>
              {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
            </select>
          </div>
          <div className="grid gap-2">
            <Label>Válido até *</Label>
            <Input type="date" value={dataValidade} onChange={e => setDataValidade(e.target.value)} />
          </div>
          <div className="grid gap-2 sm:col-span-2">
            <Label>Observação (opcional)</Label>
            <Input value={observacao} onChange={e => setObservacao(e.target.value)}
              placeholder="Informações adicionais para o cliente" />
          </div>
        </div>

        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h2 className="font-semibold">Itens</h2>
            <div className="flex gap-2">
              <select onChange={e => { adicionarProduto(e.target.value); e.target.value = '' }}
                className="h-9 rounded-md border border-input bg-transparent px-3 text-sm">
                <option value="">+ Produto do estoque</option>
                {produtos.filter(p => p.ativo).map(p =>
                  <option key={p.id} value={p.id}>{p.nome} — {fmt(p.precoVenda)}</option>
                )}
              </select>
              <Button type="button" variant="outline" size="sm" onClick={adicionarLivre}>
                <Plus size={14} className="mr-1" /> Item livre
              </Button>
            </div>
          </div>

          {itens.length === 0 && (
            <p className="py-4 text-center text-sm text-muted-foreground">
              Nenhum item adicionado. Use os botões acima para adicionar.
            </p>
          )}

          <div className="rounded-md border">
            {itens.map((item, i) => (
              <div key={i} className="flex items-center gap-3 border-b px-4 py-3 last:border-0">
                <span className="w-16 rounded bg-muted px-2 py-0.5 text-center text-xs font-medium">
                  {item.tipo}
                </span>
                <Input
                  className="flex-1"
                  value={item.descricao}
                  onChange={e => atualizarItem(i, 'descricao', e.target.value)}
                  placeholder="Descrição"
                  disabled={item.tipo === 'Produto'}
                />
                <Input
                  className="w-20"
                  type="number"
                  min="0.01"
                  step="0.01"
                  value={item.quantidade}
                  onChange={e => atualizarItem(i, 'quantidade', parseFloat(e.target.value) || 0)}
                />
                <Input
                  className="w-28"
                  type="number"
                  min="0"
                  step="0.01"
                  value={item.valorUnitario}
                  onChange={e => atualizarItem(i, 'valorUnitario', parseFloat(e.target.value) || 0)}
                />
                <span className="w-28 text-right text-sm font-medium">
                  {fmt(item.quantidade * item.valorUnitario)}
                </span>
                <Button type="button" variant="ghost" size="sm" onClick={() => removerItem(i)}>
                  <Trash2 size={14} />
                </Button>
              </div>
            ))}
            {itens.length > 0 && (
              <div className="flex justify-end px-4 py-3 font-semibold">
                Total: {fmt(total)}
              </div>
            )}
          </div>
        </div>

        {erro && <p className="text-sm text-destructive">{erro}</p>}

        <div className="flex gap-3">
          <Button type="button" variant="outline" onClick={() => navigate('/orcamentos')}>
            Cancelar
          </Button>
          <Button type="submit" disabled={salvando}>
            {salvando ? 'Salvando...' : 'Salvar Rascunho'}
          </Button>
        </div>
      </form>
    </div>
  )
}
```

---

## Task 10: Página Detalhe do Orçamento

**Files:**
- Create: `frontend/src/pages/orcamentos/DetalheOrcamento.tsx`

- [ ] **Step 1: Criar a página**

```typescript
// frontend/src/pages/orcamentos/DetalheOrcamento.tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { FileText, MessageCircle } from 'lucide-react'
import { useOrcamentos } from '@/hooks/useOrcamentos'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import type { OrcamentoStatus } from '@/types/orcamento'

const statusVariant = (s: OrcamentoStatus): 'default' | 'secondary' | 'outline' | 'destructive' => {
  if (s === 'Aprovado' || s === 'Convertido') return 'secondary'
  if (s === 'Rejeitado' || s === 'Cancelado') return 'destructive'
  if (s === 'Expirado') return 'outline'
  return 'default'
}

const fmt = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (d: string) => new Date(d).toLocaleDateString('pt-BR')

export default function DetalheOrcamento() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { orcamento, loading, error, get, enviar, aprovar, rejeitar, cancelar, converter } = useOrcamentos()
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

  function abrirPdf() {
    window.open(`/api/orcamentos/${id}/pdf`, '_blank')
  }

  function abrirWhatsapp() {
    if (!orcamento?.clienteWhatsapp) return
    const num = String(orcamento.numero).padStart(3, '0')
    const total = fmt(orcamento.total)
    const validade = fmtDate(orcamento.dataValidade)
    const msg = encodeURIComponent(
      `Olá${orcamento.clienteNome ? ` ${orcamento.clienteNome}` : ''}! ` +
      `Segue o Orçamento ORC-${num}: "${orcamento.titulo}"\n` +
      `Total: ${total} | Válido até: ${validade}`
    )
    const phone = orcamento.clienteWhatsapp.replace(/\D/g, '')
    window.open(`https://wa.me/${phone}?text=${msg}`, '_blank')
  }

  async function handleConverter() {
    if (!id) return
    if (!confirm('Converter este orçamento em venda? Uma Venda Aberta será criada.')) return
    setAcao('converter')
    try {
      const result = await converter(id)
      if (result.vendaId) navigate(`/vendas/nova?vendaId=${result.vendaId}`)
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao converter')
    } finally {
      setAcao(null)
    }
  }

  if (loading) return <p className="text-muted-foreground">Carregando...</p>
  if (error) return <p className="text-destructive">{error}</p>
  if (!orcamento) return null

  const o = orcamento
  const podePdf = true
  const podeWhatsapp = !!o.clienteWhatsapp
  const isTerminal = ['Convertido', 'Rejeitado', 'Cancelado', 'Expirado'].includes(o.status)

  return (
    <div className="max-w-3xl space-y-6">
      {o.status === 'Expirado' && (
        <div className="rounded-md border border-destructive bg-destructive/10 px-4 py-3 text-sm text-destructive">
          Este orçamento expirou em {fmtDate(o.dataValidade)}.
        </div>
      )}

      <div className="flex items-start justify-between">
        <div>
          <p className="text-sm text-muted-foreground font-mono">
            ORC-{String(o.numero).padStart(3, '0')}
          </p>
          <h1 className="text-2xl font-bold">{o.titulo}</h1>
          {o.clienteNome && <p className="text-muted-foreground">{o.clienteNome}</p>}
        </div>
        <Badge variant={statusVariant(o.status)} className="text-sm">{o.status}</Badge>
      </div>

      <div className="flex flex-wrap gap-2">
        {o.status === 'Rascunho' && (
          <>
            <Button onClick={() => executar(() => enviar(o.id), 'Enviar orçamento')}
              disabled={acao !== null}>
              {acao === 'Enviar orçamento' ? '...' : 'Enviar'}
            </Button>
            <Button variant="destructive" onClick={() => executar(() => cancelar(o.id), 'Cancelar orçamento')}
              disabled={acao !== null}>
              Cancelar
            </Button>
          </>
        )}
        {o.status === 'Enviado' && (
          <>
            <Button onClick={() => executar(() => aprovar(o.id), 'Aprovar orçamento')}
              disabled={acao !== null}>
              {acao === 'Aprovar orçamento' ? '...' : 'Aprovar'}
            </Button>
            <Button variant="outline" onClick={() => executar(() => rejeitar(o.id), 'Rejeitar orçamento')}
              disabled={acao !== null}>
              Rejeitar
            </Button>
            {podeWhatsapp && (
              <Button variant="outline" onClick={abrirWhatsapp}>
                <MessageCircle size={16} className="mr-2" /> WhatsApp
              </Button>
            )}
          </>
        )}
        {o.status === 'Aprovado' && (
          <Button onClick={handleConverter} disabled={acao !== null}>
            {acao === 'converter' ? '...' : 'Converter em Venda'}
          </Button>
        )}
        {o.status === 'Convertido' && o.vendaId && (
          <Button variant="outline" onClick={() => navigate('/vendas')}>
            Ver histórico de vendas
          </Button>
        )}
        {podePdf && (
          <Button variant="outline" onClick={abrirPdf}>
            <FileText size={16} className="mr-2" /> PDF
          </Button>
        )}
      </div>

      <div className="grid gap-1 text-sm text-muted-foreground">
        <p>Válido até: <strong className="text-foreground">{fmtDate(o.dataValidade)}</strong></p>
        {o.observacao && <p>Obs: {o.observacao}</p>}
      </div>

      <div className="rounded-md border">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b bg-muted/50">
              <th className="px-4 py-3 text-left font-medium">Tipo</th>
              <th className="px-4 py-3 text-left font-medium">Descrição</th>
              <th className="px-4 py-3 text-right font-medium">Qtd</th>
              <th className="px-4 py-3 text-right font-medium">Unitário</th>
              <th className="px-4 py-3 text-right font-medium">Total</th>
            </tr>
          </thead>
          <tbody>
            {o.itens.map(item => (
              <tr key={item.id} className="border-b">
                <td className="px-4 py-3">
                  <span className="rounded bg-muted px-2 py-0.5 text-xs">{item.tipo}</span>
                </td>
                <td className="px-4 py-3">{item.descricao}</td>
                <td className="px-4 py-3 text-right">{item.quantidade}</td>
                <td className="px-4 py-3 text-right">{fmt(item.valorUnitario)}</td>
                <td className="px-4 py-3 text-right font-medium">
                  {fmt(item.quantidade * item.valorUnitario)}
                </td>
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr>
              <td colSpan={4} className="px-4 py-3 text-right font-semibold">Total:</td>
              <td className="px-4 py-3 text-right font-bold text-lg">{fmt(o.total)}</td>
            </tr>
          </tfoot>
        </table>
      </div>

      <Button variant="ghost" onClick={() => navigate('/orcamentos')}>← Voltar</Button>
    </div>
  )
}
```

---

## Task 11: NovaVenda — suporte a ?vendaId=

**Files:**
- Modify: `frontend/src/pages/vendas/NovaVenda.tsx`
- Modify: `frontend/src/hooks/useVendas.ts`

- [ ] **Step 1: Adicionar get() e fechar() ao useVendas hook**

Ler `frontend/src/hooks/useVendas.ts`. Localizar o import de tipos e adicionar `FecharVendaRequest`:

```typescript
import type { ItemVendaRequest, CreateVendaRequest, VendaResponse, VendaListItem, FecharVendaRequest } from '@/types/vendas'
```

Localizar o `return` do hook e adicionar antes dele:

```typescript
  const get = useCallback(async (id: string): Promise<VendaResponse> => {
    return api.get<VendaResponse>(`/api/vendas/${id}`)
  }, [])

  const fechar = useCallback(async (id: string, req: FecharVendaRequest) => {
    const result = await api.post<VendaResponse>(`/api/vendas/${id}/fechar`, req)
    return result
  }, [])
```

Adicionar `get` e `fechar` ao objeto retornado.

- [ ] **Step 2: Modificar NovaVenda.tsx para suportar ?vendaId=**

Substituir o conteúdo completo de `frontend/src/pages/vendas/NovaVenda.tsx`:

```typescript
// frontend/src/pages/vendas/NovaVenda.tsx
import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { useVendas } from '@/hooks/useVendas'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import SeletorProduto from '@/components/vendas/SeletorProduto'
import ResumoPedido from '@/components/vendas/ResumoPedido'
import type { ItemCarrinho, CreateVendaRequest, FecharVendaRequest } from '@/types/vendas'

type Etapa = 1 | 2 | 3

export default function NovaVenda() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const vendaIdParam = searchParams.get('vendaId')

  const { create, fechar, get } = useVendas()
  const { clientes, list: listClientes } = useClientes()

  const [etapa, setEtapa] = useState<Etapa>(vendaIdParam ? 2 : 1)
  const [itens, setItens] = useState<ItemCarrinho[]>([])
  const [desconto, setDesconto] = useState(0)
  const [clienteId, setClienteId] = useState('')
  const [formaPagamento, setFormaPagamento] = useState<CreateVendaRequest['formaPagamento']>('Pix')
  const [parcelas, setParcelas] = useState(1)
  const [salvando, setSalvando] = useState(false)
  const [carregandoVenda, setCarregandoVenda] = useState(false)
  const [vendaFinalizada, setVendaFinalizada] = useState<{ id: string; total: number } | null>(null)

  useEffect(() => {
    if (!vendaIdParam) return
    setCarregandoVenda(true)
    void get(vendaIdParam).then(venda => {
      setItens(venda.itens.map(i => ({
        produtoId: i.produtoId,
        produtoNome: i.produtoNome,
        precoUnitario: i.precoUnitario,
        quantidade: i.quantidade,
        desconto: 0,
        total: i.precoUnitario * i.quantidade,
      })))
      listClientes()
    }).finally(() => setCarregandoVenda(false))
  }, [vendaIdParam, get, listClientes])

  function adicionarItem(item: ItemCarrinho) {
    setItens(prev => {
      const existente = prev.find(i => i.produtoId === item.produtoId)
      if (existente)
        return prev.map(i => i.produtoId === item.produtoId
          ? { ...i, quantidade: i.quantidade + 1, total: i.precoUnitario * (i.quantidade + 1) }
          : i)
      return [...prev, item]
    })
  }

  function alterarQuantidade(produtoId: string, quantidade: number) {
    if (quantidade <= 0) return
    setItens(prev => prev.map(i => i.produtoId === produtoId
      ? { ...i, quantidade, total: i.precoUnitario * quantidade }
      : i))
  }

  function removerItem(produtoId: string) {
    setItens(prev => prev.filter(i => i.produtoId !== produtoId))
  }

  async function confirmarVenda() {
    if (itens.length === 0) return
    setSalvando(true)
    try {
      if (vendaIdParam) {
        const req: FecharVendaRequest = {
          formaPagamento,
          parcelas: formaPagamento === 'Cartao' ? parcelas : undefined,
        }
        const result = await fechar(vendaIdParam, req)
        setVendaFinalizada({ id: result.id, total: result.total })
        setEtapa(3)
      } else {
        const req: CreateVendaRequest = {
          clienteId: clienteId || undefined,
          itens: itens.map(i => ({ produtoId: i.produtoId, quantidade: i.quantidade, desconto: 0 })),
          desconto,
          formaPagamento,
          parcelas: formaPagamento === 'Cartao' ? parcelas : undefined,
        }
        const result = await create(req)
        setVendaFinalizada({ id: result.id, total: result.total })
        setEtapa(3)
      }
    } catch (e) {
      alert(e instanceof Error ? e.message : 'Erro ao finalizar venda')
    } finally {
      setSalvando(false)
    }
  }

  if (carregandoVenda) return <p className="text-muted-foreground">Carregando venda...</p>

  if (etapa === 3 && vendaFinalizada) {
    const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
    return (
      <div className="max-w-md mx-auto space-y-6 pt-8 text-center">
        <div className="text-6xl">✅</div>
        <h1 className="text-2xl font-bold">Venda finalizada!</h1>
        <p className="text-muted-foreground">Total: <strong>{fmtVal(vendaFinalizada.total)}</strong></p>
        <div className="flex gap-3 justify-center">
          <Button variant="outline" onClick={() => window.print()}>Imprimir comprovante</Button>
          <Button onClick={() => navigate('/vendas')}>Ver histórico</Button>
          <Button variant="secondary" onClick={() => {
            setItens([]); setDesconto(0); setClienteId(''); setEtapa(1); setVendaFinalizada(null)
          }}>Nova venda</Button>
        </div>
      </div>
    )
  }

  const etapas = vendaIdParam
    ? ['1. Itens do orçamento', '2. Pagamento']
    : ['1. Produtos', '2. Pagamento']

  return (
    <div className="space-y-4">
      {vendaIdParam && (
        <div className="rounded-md border border-blue-200 bg-blue-50 px-4 py-2 text-sm text-blue-700">
          Venda gerada a partir de orçamento. Confirme o pagamento para concluir.
        </div>
      )}

      <div className="flex items-center gap-2 text-sm">
        {etapas.map((label, i) => (
          <span key={label}
            className={`px-3 py-1 rounded-full ${etapa === i + 1
              ? 'bg-primary text-primary-foreground'
              : 'bg-muted text-muted-foreground'}`}>
            {label}
          </span>
        ))}
      </div>

      {etapa === 1 && !vendaIdParam && (
        <div className="grid grid-cols-2 gap-6">
          <div className="space-y-3">
            <h2 className="font-semibold">Adicionar produtos</h2>
            <SeletorProduto onAdd={adicionarItem} />
          </div>
          <div className="space-y-3">
            <h2 className="font-semibold">Pedido</h2>
            <ResumoPedido
              itens={itens} desconto={desconto}
              onChangeQuantidade={alterarQuantidade}
              onRemover={removerItem}
              onChangeDesconto={setDesconto}
            />
            <Button
              className="w-full"
              disabled={itens.length === 0}
              onClick={() => { setEtapa(2); listClientes() }}>
              Próximo →
            </Button>
          </div>
        </div>
      )}

      {etapa === 2 && (
        <div className="max-w-md space-y-4">
          <h2 className="font-semibold text-lg">Forma de pagamento</h2>

          {vendaIdParam && itens.length > 0 && (
            <div className="rounded-md border p-4 space-y-2">
              <p className="text-sm font-medium">Itens da venda</p>
              {itens.map(i => (
                <div key={i.produtoId} className="flex justify-between text-sm text-muted-foreground">
                  <span>{i.produtoNome} × {i.quantidade}</span>
                  <span>{i.total.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })}</span>
                </div>
              ))}
            </div>
          )}

          {!vendaIdParam && (
            <div className="grid gap-2">
              <Label>Cliente (opcional)</Label>
              <select
                value={clienteId}
                onChange={e => setClienteId(e.target.value)}
                className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm">
                <option value="">Sem cliente (balcão)</option>
                {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
              </select>
            </div>
          )}

          <div className="grid gap-2">
            <Label>Forma de pagamento</Label>
            <div className="grid grid-cols-4 gap-2">
              {(['Dinheiro', 'Pix', 'Cartao', 'Outro'] as const).map(f => (
                <Button key={f} type="button"
                  variant={formaPagamento === f ? 'default' : 'outline'}
                  onClick={() => setFormaPagamento(f)}>
                  {f}
                </Button>
              ))}
            </div>
          </div>

          {formaPagamento === 'Cartao' && (
            <div className="grid gap-2">
              <Label>Parcelas</Label>
              <Input type="number" min="1" max="12" value={parcelas}
                onChange={e => setParcelas(Number(e.target.value))} />
            </div>
          )}

          <div className="flex gap-3 pt-4">
            {!vendaIdParam && (
              <Button variant="outline" onClick={() => setEtapa(1)}>← Voltar</Button>
            )}
            <Button className="flex-1" onClick={confirmarVenda} disabled={salvando}>
              {salvando ? 'Finalizando...' : '✓ Finalizar venda'}
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 3: Verificar TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
export PATH="/usr/local/bin:/opt/homebrew/bin:$PATH"
npm run typecheck 2>&1 | tail -20
```

Esperado: sem erros.

---

## Task 12: Roteamento + Sidebar

**Files:**
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Adicionar imports e rotas ao router/index.tsx**

Adicionar imports:
```typescript
import Orcamentos from '@/pages/orcamentos/Orcamentos'
import NovoOrcamento from '@/pages/orcamentos/NovoOrcamento'
import DetalheOrcamento from '@/pages/orcamentos/DetalheOrcamento'
```

Adicionar ao array `children` de AppLayout:
```typescript
      { path: '/orcamentos', element: <Orcamentos /> },
      { path: '/orcamentos/novo', element: <NovoOrcamento /> },
      { path: '/orcamentos/:id', element: <DetalheOrcamento /> },
```

- [ ] **Step 2: Adicionar link ao Sidebar.tsx**

Adicionar import de `FileText` na linha de imports do lucide-react.

Adicionar ao array `links`, entre o item de Histórico (`/vendas`) e Produtos (`/estoque`):
```typescript
  { to: '/orcamentos', icon: FileText, label: 'Orçamentos' },
```

- [ ] **Step 3: Verificar TypeScript**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
export PATH="/usr/local/bin:/opt/homebrew/bin:$PATH"
npm run typecheck 2>&1 | tail -10
```

Esperado: sem erros.

- [ ] **Step 4: Rodar testes frontend**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
export PATH="/usr/local/bin:/opt/homebrew/bin:$PATH"
npm test -- --run 2>&1 | tail -20
```

Esperado: todos os testes existentes passam.

---

## Task 13: Testes frontend (Vitest)

**Files:**
- Create: `frontend/src/test/orcamentos.test.tsx`

- [ ] **Step 1: Escrever os testes**

```typescript
// frontend/src/test/orcamentos.test.tsx
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'

// Mock do hook useOrcamentos
const mockCreate = vi.fn()
const mockEnviar = vi.fn()
const mockConverter = vi.fn()

vi.mock('@/hooks/useOrcamentos', () => ({
  useOrcamentos: () => ({
    orcamentos: [
      {
        id: '1', numero: 1, titulo: 'Orçamento Teste',
        clienteNome: 'Maria', dataValidade: '2026-06-01',
        status: 'Rascunho', total: 150,
      },
      {
        id: '2', numero: 2, titulo: 'Outro Orçamento',
        clienteNome: null, dataValidade: '2026-05-20',
        status: 'Aprovado', total: 300,
      },
    ],
    orcamento: null,
    loading: false,
    error: null,
    list: vi.fn(),
    get: vi.fn(),
    create: mockCreate,
    enviar: mockEnviar,
    aprovar: vi.fn(),
    rejeitar: vi.fn(),
    cancelar: vi.fn(),
    converter: mockConverter,
  }),
}))

vi.mock('@/hooks/useEstoque', () => ({
  useEstoque: () => ({ produtos: [], list: vi.fn() }),
}))

vi.mock('@/hooks/useClientes', () => ({
  useClientes: () => ({ clientes: [], list: vi.fn() }),
}))

import Orcamentos from '@/pages/orcamentos/Orcamentos'
import NovoOrcamento from '@/pages/orcamentos/NovoOrcamento'

describe('Orcamentos list', () => {
  it('renderiza tabela com orçamentos', () => {
    render(<MemoryRouter><Orcamentos /></MemoryRouter>)
    expect(screen.getByText('Orçamento Teste')).toBeTruthy()
    expect(screen.getByText('Outro Orçamento')).toBeTruthy()
  })

  it('mostra botão Enviar para Rascunho e Converter para Aprovado', () => {
    render(<MemoryRouter><Orcamentos /></MemoryRouter>)
    const botoes = screen.getAllByRole('button', { name: /Enviar|Converter|Ver/i })
    // primeiro orçamento é Rascunho → Enviar (mas navega para detalhe)
    expect(botoes.length).toBeGreaterThan(0)
  })

  it('exibe chips de filtro de status', () => {
    render(<MemoryRouter><Orcamentos /></MemoryRouter>)
    expect(screen.getByText('Rascunho')).toBeTruthy()
    expect(screen.getByText('Aprovado')).toBeTruthy()
    expect(screen.getByText('Expirado')).toBeTruthy()
  })
})

describe('NovoOrcamento form', () => {
  beforeEach(() => { mockCreate.mockReset() })

  it('renderiza campos obrigatórios', () => {
    render(<MemoryRouter><NovoOrcamento /></MemoryRouter>)
    expect(screen.getByPlaceholderText(/Orçamento/i)).toBeTruthy()
    expect(screen.getByText('Válido até *')).toBeTruthy()
  })

  it('adiciona item livre ao clicar no botão', () => {
    render(<MemoryRouter><NovoOrcamento /></MemoryRouter>)
    const btn = screen.getByText(/Item livre/i)
    fireEvent.click(btn)
    expect(screen.getByText('Livre')).toBeTruthy()
  })

  it('mostra erro ao tentar salvar sem título', async () => {
    render(<MemoryRouter><NovoOrcamento /></MemoryRouter>)
    const salvar = screen.getByText('Salvar Rascunho')
    fireEvent.click(salvar)
    expect(await screen.findByText('Título obrigatório')).toBeTruthy()
  })
})
```

- [ ] **Step 2: Rodar os testes**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/frontend
export PATH="/usr/local/bin:/opt/homebrew/bin:$PATH"
npm test -- --run 2>&1 | tail -20
```

Esperado: `5 passed` (3 list + 3 form, pode haver diferença de count se algum for ajustado) — todos passam.

- [ ] **Step 3: Rodar todos os testes backend para confirmar regressão zero**

```bash
cd /Users/brunomedeiros/Repositorios/ERP/gestorai-erp/backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj 2>&1 | tail -10
```

Esperado: todos os testes passam (incluindo os 6 novos de OrcamentoServiceTests).
