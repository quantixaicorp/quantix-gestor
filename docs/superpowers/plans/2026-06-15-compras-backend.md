# Módulo de Compras — Backend — Plano de Implementação

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar o backend completo do módulo de Compras: entidades, DTOs, serviços, endpoints e migração de banco de dados.

**Architecture:** Abordagem C — novo modelo `Parcelamento` criado como fundação; `Lancamento` recebe FK `ParcelamentoId`; `CompraService.ConfirmarAsync` executa estoque + parcelas em transação única. Sem breaking changes em endpoints existentes.

**Tech Stack:** .NET 10 / C#, EF Core + PostgreSQL (snake_case), Minimal API, xUnit + EF InMemory, FluentValidation.

---

## Mapa de Arquivos

### Criar
- `Domain/Enums/StatusCompra.cs`
- `Domain/Enums/StatusPedidoCompra.cs`
- `Domain/Enums/DestinoCompra.cs`
- `Domain/Enums/StatusParcelamento.cs`
- `Domain/Enums/StatusFornecedor.cs`
- `Domain/Entities/Parcelamento.cs`
- `Domain/Entities/Compra.cs`
- `Domain/Entities/ItemCompra.cs`
- `Domain/Entities/PedidoCompra.cs`
- `Domain/Entities/ItemPedidoCompra.cs`
- `DTOs/Compras/CompraDto.cs`
- `DTOs/Compras/PedidoCompraDto.cs`
- `DTOs/Compras/ParcelamentoDto.cs`
- `DTOs/Compras/ComprasDashboardDto.cs`
- `Services/Compras/VencimentoCalculator.cs`
- `Services/Compras/ParcelamentoService.cs`
- `Services/Compras/CompraService.cs`
- `Services/Compras/CreateCompraValidator.cs`
- `Services/Compras/PedidoCompraService.cs`
- `Services/Compras/CreatePedidoCompraValidator.cs`
- `Services/Compras/ComprasDashboardService.cs`
- `Endpoints/ComprasEndpoints.cs`
- `Endpoints/PedidosCompraEndpoints.cs`
- `Endpoints/ParcelamentosEndpoints.cs`
- `tests/Services/ParcelamentoServiceTests.cs`
- `tests/Services/CompraServiceTests.cs`
- `tests/Services/PedidoCompraServiceTests.cs`

### Modificar
- `Domain/Entities/Fornecedor.cs` — adicionar 5 campos
- `Domain/Entities/Lancamento.cs` — adicionar `ParcelamentoId`, `NumeroParcela`
- `DTOs/Fornecedores/FornecedorDto.cs` — adicionar campos novos
- `Services/Fornecedores/FornecedorService.cs` — atualizar Create/Update/ToResponse
- `Services/Financeiro/LancamentoService.cs` — chamar `RecalcularStatusAsync` em `PagarAsync`
- `Infrastructure/Data/AppDbContext.cs` — novos DbSets + relacionamentos
- `Program.cs` — registrar serviços + mapear endpoints

Todos os prefixos de caminho omitem `backend/src/GestorAI.API/` e `backend/tests/GestorAI.Tests/` para brevidade.

---

## Task 1: Enums de Domínio

**Files:**
- Create: `Domain/Enums/StatusCompra.cs`
- Create: `Domain/Enums/StatusPedidoCompra.cs`
- Create: `Domain/Enums/DestinoCompra.cs`
- Create: `Domain/Enums/StatusParcelamento.cs`
- Create: `Domain/Enums/StatusFornecedor.cs`

- [ ] **Step 1: Criar os 5 arquivos de enum**

```csharp
// Domain/Enums/StatusCompra.cs
namespace GestorAI.API.Domain.Enums;
public enum StatusCompra { Rascunho, Confirmada, Cancelada }
```

```csharp
// Domain/Enums/StatusPedidoCompra.cs
namespace GestorAI.API.Domain.Enums;
public enum StatusPedidoCompra
{
    Rascunho, AguardandoAprovacao, Aprovado,
    RecebidoParcialmente, RecebidoTotalmente, Cancelado
}
```

```csharp
// Domain/Enums/DestinoCompra.cs
namespace GestorAI.API.Domain.Enums;
public enum DestinoCompra { EstoqueParaVenda, ConsumoInterno, AtivoImobilizado }
```

```csharp
// Domain/Enums/StatusParcelamento.cs
namespace GestorAI.API.Domain.Enums;
public enum StatusParcelamento { EmAberto, PagoParcialmente, PagoTotal, Cancelado }
```

```csharp
// Domain/Enums/StatusFornecedor.cs
namespace GestorAI.API.Domain.Enums;
public enum StatusFornecedor { Ativo, Inativo }
```

- [ ] **Step 2: Build para verificar**

```bash
cd backend/src/GestorAI.API && dotnet build -q
```
Esperado: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Step 3: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Enums/StatusCompra.cs \
        backend/src/GestorAI.API/Domain/Enums/StatusPedidoCompra.cs \
        backend/src/GestorAI.API/Domain/Enums/DestinoCompra.cs \
        backend/src/GestorAI.API/Domain/Enums/StatusParcelamento.cs \
        backend/src/GestorAI.API/Domain/Enums/StatusFornecedor.cs
git commit -m "feat(compras): add domain enums for compras module"
```

---

## Task 2: Novas Entidades de Domínio

**Files:**
- Create: `Domain/Entities/Parcelamento.cs`
- Create: `Domain/Entities/Compra.cs`
- Create: `Domain/Entities/ItemCompra.cs`
- Create: `Domain/Entities/PedidoCompra.cs`
- Create: `Domain/Entities/ItemPedidoCompra.cs`

- [ ] **Step 1: Criar Parcelamento.cs**

```csharp
using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;

public class Parcelamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? CompraId { get; set; }
    public required string Descricao { get; set; }
    public decimal ValorTotal { get; set; }
    public int QtdParcelas { get; set; }
    public StatusParcelamento Status { get; set; } = StatusParcelamento.EmAberto;
    public Compra? Compra { get; set; }
    public ICollection<Lancamento> Parcelas { get; set; } = [];
}
```

- [ ] **Step 2: Criar Compra.cs**

```csharp
using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;

public class Compra : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public int Numero { get; set; }
    public DateTime Data { get; set; }
    public Guid FornecedorId { get; set; }
    public Guid? PedidoCompraId { get; set; }
    public required string TipoCompra { get; set; }
    public string? NumeroNota { get; set; }
    public required string CondicaoPagamento { get; set; }
    public required string FormaPagamento { get; set; }
    public int? QtdParcelas { get; set; }
    public string? VencimentosJson { get; set; }
    public StatusCompra Status { get; set; } = StatusCompra.Rascunho;
    public decimal ValorTotal { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public Fornecedor? Fornecedor { get; set; }
    public PedidoCompra? PedidoCompra { get; set; }
    public ICollection<ItemCompra> Itens { get; set; } = [];
    public Parcelamento? Parcelamento { get; set; }
}
```

- [ ] **Step 3: Criar ItemCompra.cs**

```csharp
using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;

public class ItemCompra
{
    public Guid Id { get; set; }
    public Guid CompraId { get; set; }
    public Guid? ProdutoId { get; set; }
    public required string Descricao { get; set; }
    public DestinoCompra DestinoCompra { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal FreteRateado { get; set; }
    public decimal Impostos { get; set; }
    public decimal ValorTotal { get; set; }
    public string? CategoriaFinanceira { get; set; }
    public string? CentroCusto { get; set; }
    public Compra? Compra { get; set; }
    public Produto? Produto { get; set; }
}
```

- [ ] **Step 4: Criar PedidoCompra.cs**

```csharp
using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;

public class PedidoCompra : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public int Numero { get; set; }
    public DateTime Data { get; set; }
    public Guid FornecedorId { get; set; }
    public StatusPedidoCompra Status { get; set; } = StatusPedidoCompra.Rascunho;
    public decimal ValorEstimado { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Fornecedor? Fornecedor { get; set; }
    public ICollection<ItemPedidoCompra> Itens { get; set; } = [];
}
```

- [ ] **Step 5: Criar ItemPedidoCompra.cs**

```csharp
namespace GestorAI.API.Domain.Entities;

public class ItemPedidoCompra
{
    public Guid Id { get; set; }
    public Guid PedidoCompraId { get; set; }
    public Guid? ProdutoId { get; set; }
    public required string Descricao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorEstimado { get; set; }
    public PedidoCompra? PedidoCompra { get; set; }
    public Produto? Produto { get; set; }
}
```

- [ ] **Step 6: Build**

```bash
cd backend/src/GestorAI.API && dotnet build -q
```
Esperado: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Entities/Parcelamento.cs \
        backend/src/GestorAI.API/Domain/Entities/Compra.cs \
        backend/src/GestorAI.API/Domain/Entities/ItemCompra.cs \
        backend/src/GestorAI.API/Domain/Entities/PedidoCompra.cs \
        backend/src/GestorAI.API/Domain/Entities/ItemPedidoCompra.cs
git commit -m "feat(compras): add new domain entities"
```

---

## Task 3: Modificar Entidades Existentes

**Files:**
- Modify: `Domain/Entities/Fornecedor.cs`
- Modify: `Domain/Entities/Lancamento.cs`

- [ ] **Step 1: Adicionar campos a Fornecedor.cs**

Adicionar ao final da classe, antes do `}` de fechamento:

```csharp
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? Whatsapp { get; set; }
    public StatusFornecedor Status { get; set; } = StatusFornecedor.Ativo;
```

Adicionar o `using` no topo do arquivo:
```csharp
using GestorAI.API.Domain.Enums;
```

- [ ] **Step 2: Adicionar campos a Lancamento.cs**

Adicionar ao final da classe, antes do `}` de fechamento:

```csharp
    public Guid? ParcelamentoId { get; set; }
    public int? NumeroParcela { get; set; }
```

- [ ] **Step 3: Build**

```bash
cd backend/src/GestorAI.API && dotnet build -q
```
Esperado: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Entities/Fornecedor.cs \
        backend/src/GestorAI.API/Domain/Entities/Lancamento.cs
git commit -m "feat(compras): extend Fornecedor and Lancamento entities"
```

---

## Task 4: DTOs

**Files:**
- Create: `DTOs/Compras/CompraDto.cs`
- Create: `DTOs/Compras/PedidoCompraDto.cs`
- Create: `DTOs/Compras/ParcelamentoDto.cs`
- Create: `DTOs/Compras/ComprasDashboardDto.cs`
- Modify: `DTOs/Fornecedores/FornecedorDto.cs`

- [ ] **Step 1: Criar CompraDto.cs**

```csharp
namespace GestorAI.API.DTOs.Compras;

public record ItemCompraRequest(
    Guid? ProdutoId,
    string Descricao,
    string DestinoCompra,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal Desconto,
    decimal FreteRateado,
    decimal Impostos,
    string? CategoriaFinanceira,
    string? CentroCusto);

public record ParcelaPersonalizadaRequest(int Numero, DateTime DataVencimento);

public record CreateCompraRequest(
    Guid FornecedorId,
    DateTime Data,
    string TipoCompra,
    string? NumeroNota,
    string CondicaoPagamento,
    string FormaPagamento,
    int? QtdParcelas,
    List<ParcelaPersonalizadaRequest>? ParcelasPersonalizadas,
    Guid? PedidoCompraId,
    string? Observacoes,
    List<ItemCompraRequest> Itens);

public record UpdateCompraRequest(
    Guid FornecedorId,
    DateTime Data,
    string TipoCompra,
    string? NumeroNota,
    string CondicaoPagamento,
    string FormaPagamento,
    int? QtdParcelas,
    List<ParcelaPersonalizadaRequest>? ParcelasPersonalizadas,
    Guid? PedidoCompraId,
    string? Observacoes,
    List<ItemCompraRequest> Itens);

public record ItemCompraResponse(
    Guid Id,
    Guid? ProdutoId,
    string Descricao,
    string DestinoCompra,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal Desconto,
    decimal FreteRateado,
    decimal Impostos,
    decimal ValorTotal,
    string? CategoriaFinanceira,
    string? CentroCusto);

public record CompraResponse(
    Guid Id,
    int Numero,
    DateTime Data,
    Guid FornecedorId,
    string FornecedorNome,
    Guid? PedidoCompraId,
    string TipoCompra,
    string? NumeroNota,
    string CondicaoPagamento,
    string FormaPagamento,
    string Status,
    decimal ValorTotal,
    string? Observacoes,
    DateTime CriadaEm,
    List<ItemCompraResponse> Itens,
    Guid? ParcelamentoId);

public record CompraResumoResponse(
    decimal TotalMes,
    int QtdComprasMes,
    decimal ContasAPagarGeradas,
    int FornecedoresAtivos);
```

- [ ] **Step 2: Criar PedidoCompraDto.cs**

```csharp
namespace GestorAI.API.DTOs.Compras;

public record ItemPedidoCompraRequest(
    Guid? ProdutoId,
    string Descricao,
    decimal Quantidade,
    decimal ValorEstimado);

public record CreatePedidoCompraRequest(
    Guid FornecedorId,
    DateTime Data,
    string? Observacoes,
    List<ItemPedidoCompraRequest> Itens);

public record UpdatePedidoCompraRequest(
    Guid FornecedorId,
    DateTime Data,
    string? Observacoes,
    List<ItemPedidoCompraRequest> Itens);

public record ItemPedidoCompraResponse(
    Guid Id,
    Guid? ProdutoId,
    string Descricao,
    decimal Quantidade,
    decimal ValorEstimado);

public record PedidoCompraResponse(
    Guid Id,
    int Numero,
    DateTime Data,
    Guid FornecedorId,
    string FornecedorNome,
    string Status,
    decimal ValorEstimado,
    string? Observacoes,
    DateTime CriadoEm,
    List<ItemPedidoCompraResponse> Itens);
```

- [ ] **Step 3: Criar ParcelamentoDto.cs**

```csharp
namespace GestorAI.API.DTOs.Compras;

public record ParcelaResponse(
    Guid Id,
    int NumeroParcela,
    decimal Valor,
    DateTime DataVencimento,
    DateTime? DataPagamento,
    string Status);

public record ParcelamentoResponse(
    Guid Id,
    Guid? CompraId,
    string Descricao,
    decimal ValorTotal,
    int QtdParcelas,
    string Status,
    List<ParcelaResponse> Parcelas);
```

- [ ] **Step 4: Criar ComprasDashboardDto.cs**

```csharp
namespace GestorAI.API.DTOs.Compras;

public record ComprasDashboardResponse(
    decimal TotalMes,
    decimal TotalAno,
    decimal TicketMedio,
    int QtdCompras,
    int FornecedoresAtivos,
    List<EvolucaoMensalCompraItem> EvolucaoMensal,
    List<ComprasPorFornecedorItem> PorFornecedor,
    List<TopProdutoCompradoItem> TopProdutos);

public record EvolucaoMensalCompraItem(string Mes, decimal Total, int QtdCompras);
public record ComprasPorFornecedorItem(string Fornecedor, decimal Total);
public record TopProdutoCompradoItem(string Produto, decimal Quantidade);
```

- [ ] **Step 5: Atualizar FornecedorDto.cs**

Substituir todo o conteúdo do arquivo:

```csharp
namespace GestorAI.API.DTOs.Fornecedores;

public record FornecedorResponse(
    Guid Id,
    string Nome,
    string? RazaoSocial,
    string? NomeFantasia,
    string? CnpjCpf,
    string? InscricaoEstadual,
    string? Telefone,
    string? Whatsapp,
    string? Email,
    string? Logradouro,
    string? Cidade,
    string? Uf,
    string? Cep,
    string? Contato,
    string? Observacoes,
    string Status,
    DateTime DataCadastro);

public record CreateFornecedorRequest(
    string Nome,
    string? RazaoSocial,
    string? NomeFantasia,
    string? CnpjCpf,
    string? InscricaoEstadual,
    string? Telefone,
    string? Whatsapp,
    string? Email,
    string? Logradouro,
    string? Cidade,
    string? Uf,
    string? Cep,
    string? Contato,
    string? Observacoes);

public record UpdateFornecedorRequest(
    string Nome,
    string? RazaoSocial,
    string? NomeFantasia,
    string? CnpjCpf,
    string? InscricaoEstadual,
    string? Telefone,
    string? Whatsapp,
    string? Email,
    string? Logradouro,
    string? Cidade,
    string? Uf,
    string? Cep,
    string? Contato,
    string? Observacoes,
    string? Status);
```

- [ ] **Step 6: Build**

```bash
cd backend/src/GestorAI.API && dotnet build -q
```
Esperado: `Build succeeded.` (erros de compilação em FornecedorService são esperados — serão corrigidos na Task 8)

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/
git commit -m "feat(compras): add DTOs for compras, pedidos, parcelamentos and dashboard"
```

---

## Task 5: AppDbContext

**Files:**
- Modify: `Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: Adicionar DbSets novos**

Adicionar após a linha `public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();`:

```csharp
    public DbSet<Parcelamento> Parcelamentos => Set<Parcelamento>();
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<ItemCompra> ItensCompra => Set<ItemCompra>();
    public DbSet<PedidoCompra> PedidosCompra => Set<PedidoCompra>();
    public DbSet<ItemPedidoCompra> ItensPedidoCompra => Set<ItemPedidoCompra>();
```

- [ ] **Step 2: Adicionar query filters e relacionamentos em OnModelCreating**

Adicionar ao final do método `OnModelCreating`, antes do `}` de fechamento:

```csharp
        // Parcelamentos
        modelBuilder.Entity<Parcelamento>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Parcelamento>()
            .HasMany(p => p.Parcelas)
            .WithOne()
            .HasForeignKey(l => l.ParcelamentoId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        // Compras
        modelBuilder.Entity<Compra>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<Compra>()
            .HasMany(c => c.Itens)
            .WithOne(i => i.Compra)
            .HasForeignKey(i => i.CompraId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Compra>()
            .HasOne(c => c.Parcelamento)
            .WithOne(p => p.Compra)
            .HasForeignKey<Parcelamento>(p => p.CompraId)
            .IsRequired(false);

        // ItemCompra — sem tenant filter (acessado via Compra)
        modelBuilder.Entity<ItemCompra>().ToTable("ItensCompra");

        // PedidosCompra
        modelBuilder.Entity<PedidoCompra>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
        modelBuilder.Entity<PedidoCompra>()
            .HasMany(p => p.Itens)
            .WithOne(i => i.PedidoCompra)
            .HasForeignKey(i => i.PedidoCompraId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ItemPedidoCompra>().ToTable("ItensPedidoCompra");
```

- [ ] **Step 3: Build**

```bash
cd backend/src/GestorAI.API && dotnet build -q
```
Esperado: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs
git commit -m "feat(compras): register new entities in AppDbContext"
```

---

## Task 6: Migração EF Core

**Files:**
- Create: `Migrations/AddModuloCompras.cs` (gerado automaticamente)

- [ ] **Step 1: Gerar a migração**

```bash
cd backend/src/GestorAI.API && dotnet ef migrations add AddModuloCompras
```
Esperado: `Done. To undo this action, use 'ef migrations remove'`

- [ ] **Step 2: Verificar o arquivo gerado**

```bash
ls backend/src/GestorAI.API/Migrations/ | grep AddModuloCompras
```
Deve aparecer `*_AddModuloCompras.cs` e `*_AddModuloCompras.Designer.cs`.

Abrir o arquivo `*_AddModuloCompras.cs` e confirmar que contém:
- `CreateTable("gestor.Compras", ...)`
- `CreateTable("gestor.ItensCompra", ...)`
- `CreateTable("gestor.Parcelamentos", ...)`
- `CreateTable("gestor.PedidosCompra", ...)`
- `CreateTable("gestor.ItensPedidoCompra", ...)`
- `AddColumn("gestor.Fornecedores", "razao_social", ...)`
- `AddColumn("gestor.Lancamentos", "parcelamento_id", ...)`

- [ ] **Step 3: Aplicar a migração (ambiente de dev)**

```bash
cd backend/src/GestorAI.API && dotnet ef database update
```
Esperado: `Done.`

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Migrations/
git commit -m "feat(compras): add EF Core migration AddModuloCompras"
```

---

## Task 7: ParcelamentoService + Tests

**Files:**
- Create: `Services/Compras/VencimentoCalculator.cs`
- Create: `Services/Compras/ParcelamentoService.cs`
- Create: `tests/Services/ParcelamentoServiceTests.cs`

- [ ] **Step 1: Criar VencimentoCalculator.cs**

```csharp
namespace GestorAI.API.Services.Compras;

public static class VencimentoCalculator
{
    public static List<DateTime> Calcular(
        string condicao, DateTime dataBase, int? qtdParcelas,
        List<DateTime>? personalizadas) => condicao switch
    {
        "AVista"        => [dataBase],
        "30d"           => [dataBase.AddDays(30)],
        "30_60_90d"     => [dataBase.AddDays(30), dataBase.AddDays(60), dataBase.AddDays(90)],
        "Parcelado"     => Enumerable.Range(1, qtdParcelas ?? 1)
                            .Select(i => dataBase.AddMonths(i)).ToList(),
        "Personalizado" => personalizadas ?? [dataBase],
        _               => [dataBase],
    };
}
```

- [ ] **Step 2: Escrever testes de VencimentoCalculator**

```csharp
// tests/Services/ParcelamentoServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Compras;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ParcelamentoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ParcelamentoService svc) Setup()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        return (db, new ParcelamentoService(db, tenant));
    }

    [Fact]
    public void VencimentoCalculator_AVista_RetornaUmaDataIgualABase()
    {
        var data = new DateTime(2026, 1, 10);
        var result = VencimentoCalculator.Calcular("AVista", data, null, null);
        Assert.Single(result);
        Assert.Equal(data, result[0]);
    }

    [Fact]
    public void VencimentoCalculator_30d_RetornaDataMais30()
    {
        var data = new DateTime(2026, 1, 10);
        var result = VencimentoCalculator.Calcular("30d", data, null, null);
        Assert.Single(result);
        Assert.Equal(data.AddDays(30), result[0]);
    }

    [Fact]
    public void VencimentoCalculator_30_60_90d_Retorna3Datas()
    {
        var data = new DateTime(2026, 1, 10);
        var result = VencimentoCalculator.Calcular("30_60_90d", data, null, null);
        Assert.Equal(3, result.Count);
        Assert.Equal(data.AddDays(30), result[0]);
        Assert.Equal(data.AddDays(60), result[1]);
        Assert.Equal(data.AddDays(90), result[2]);
    }

    [Fact]
    public void VencimentoCalculator_Parcelado3x_Retorna3MesesConsecutivos()
    {
        var data = new DateTime(2026, 1, 10);
        var result = VencimentoCalculator.Calcular("Parcelado", data, 3, null);
        Assert.Equal(3, result.Count);
        Assert.Equal(data.AddMonths(1), result[0]);
        Assert.Equal(data.AddMonths(2), result[1]);
        Assert.Equal(data.AddMonths(3), result[2]);
    }

    [Fact]
    public async Task RecalcularStatusAsync_TodasPagas_DefineStatusPagoTotal()
    {
        var (db, svc) = Setup();
        var parcelamento = new Parcelamento
        {
            EmpresaId = _empresaId,
            Descricao = "Teste",
            ValorTotal = 100m,
            QtdParcelas = 2,
        };
        db.Parcelamentos.Add(parcelamento);
        db.Lancamentos.AddRange(
            new Lancamento { EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
                Descricao = "P1", Valor = 50m, DataVencimento = DateTime.Today,
                Status = StatusLancamento.Pago, Categoria = "Compras",
                ParcelamentoId = parcelamento.Id, NumeroParcela = 1 },
            new Lancamento { EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
                Descricao = "P2", Valor = 50m, DataVencimento = DateTime.Today,
                Status = StatusLancamento.Pago, Categoria = "Compras",
                ParcelamentoId = parcelamento.Id, NumeroParcela = 2 });
        await db.SaveChangesAsync();

        await svc.RecalcularStatusAsync(parcelamento.Id, default);

        var saved = await db.Parcelamentos.IgnoreQueryFilters()
            .FirstAsync(p => p.Id == parcelamento.Id);
        Assert.Equal(StatusParcelamento.PagoTotal, saved.Status);
    }

    [Fact]
    public async Task RecalcularStatusAsync_AlgumasPagas_DefineStatusPagoParcialmente()
    {
        var (db, svc) = Setup();
        var parcelamento = new Parcelamento
        {
            EmpresaId = _empresaId,
            Descricao = "Teste",
            ValorTotal = 100m,
            QtdParcelas = 2,
        };
        db.Parcelamentos.Add(parcelamento);
        db.Lancamentos.AddRange(
            new Lancamento { EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
                Descricao = "P1", Valor = 50m, DataVencimento = DateTime.Today,
                Status = StatusLancamento.Pago, Categoria = "Compras",
                ParcelamentoId = parcelamento.Id, NumeroParcela = 1 },
            new Lancamento { EmpresaId = _empresaId, Tipo = TipoLancamento.Despesa,
                Descricao = "P2", Valor = 50m, DataVencimento = DateTime.Today,
                Status = StatusLancamento.Pendente, Categoria = "Compras",
                ParcelamentoId = parcelamento.Id, NumeroParcela = 2 });
        await db.SaveChangesAsync();

        await svc.RecalcularStatusAsync(parcelamento.Id, default);

        var saved = await db.Parcelamentos.IgnoreQueryFilters()
            .FirstAsync(p => p.Id == parcelamento.Id);
        Assert.Equal(StatusParcelamento.PagoParcialmente, saved.Status);
    }
}
```

- [ ] **Step 3: Rodar testes (devem falhar)**

```bash
cd backend && dotnet test tests/GestorAI.Tests/ --filter "ParcelamentoServiceTests" -v
```
Esperado: Falhas porque `ParcelamentoService` ainda não existe.

- [ ] **Step 4: Criar ParcelamentoService.cs**

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Compras;

public class ParcelamentoService(AppDbContext db, TenantContext tenantContext)
{
    public Parcelamento Criar(
        Guid? compraId, string descricao, decimal valorTotal,
        List<DateTime> vencimentos, string categoria)
    {
        var parcelamento = new Parcelamento
        {
            EmpresaId = tenantContext.EmpresaId,
            CompraId = compraId,
            Descricao = descricao,
            ValorTotal = valorTotal,
            QtdParcelas = vencimentos.Count,
            Status = StatusParcelamento.EmAberto,
        };
        db.Parcelamentos.Add(parcelamento);

        var valorParcela = Math.Round(valorTotal / vencimentos.Count, 2);
        var resto = valorTotal - valorParcela * vencimentos.Count;

        for (int i = 0; i < vencimentos.Count; i++)
        {
            var valor = i == vencimentos.Count - 1 ? valorParcela + resto : valorParcela;
            db.Lancamentos.Add(new Lancamento
            {
                EmpresaId = tenantContext.EmpresaId,
                Tipo = TipoLancamento.Despesa,
                Descricao = $"{descricao} - Parcela {i + 1}/{vencimentos.Count}",
                Valor = valor,
                DataVencimento = vencimentos[i],
                Status = StatusLancamento.Pendente,
                Categoria = categoria,
                ParcelamentoId = parcelamento.Id,
                NumeroParcela = i + 1,
            });
        }

        return parcelamento;
    }

    public async Task RecalcularStatusAsync(Guid parcelamentoId, CancellationToken ct)
    {
        var parcelamento = await db.Parcelamentos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == parcelamentoId, ct);
        if (parcelamento is null || parcelamento.Status == StatusParcelamento.Cancelado)
            return;

        var parcelas = await db.Lancamentos.IgnoreQueryFilters()
            .Where(l => l.ParcelamentoId == parcelamentoId)
            .ToListAsync(ct);

        parcelamento.Status = parcelas.All(l => l.Status == StatusLancamento.Pago)
            ? StatusParcelamento.PagoTotal
            : parcelas.Any(l => l.Status == StatusLancamento.Pago)
                ? StatusParcelamento.PagoParcialmente
                : StatusParcelamento.EmAberto;

        await db.SaveChangesAsync(ct);
    }

    public void Cancelar(Guid parcelamentoId)
    {
        var parcelas = db.Lancamentos
            .Where(l => l.ParcelamentoId == parcelamentoId && l.Status == StatusLancamento.Pendente)
            .ToList();
        foreach (var p in parcelas) p.Status = StatusLancamento.Cancelado;

        var parcelamento = db.Parcelamentos.Local
            .FirstOrDefault(p => p.Id == parcelamentoId);
        if (parcelamento is not null) parcelamento.Status = StatusParcelamento.Cancelado;
    }

    public async Task<List<ParcelamentoResponse>> ListAsync(
        Guid? compraId, string? status, CancellationToken ct)
    {
        var query = db.Parcelamentos
            .Include(p => p.Parcelas)
            .AsQueryable();

        if (compraId.HasValue)
            query = query.Where(p => p.CompraId == compraId.Value);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusParcelamento>(status, out var s))
            query = query.Where(p => p.Status == s);

        var list = await query.ToListAsync(ct);
        return list.Select(ToResponse).ToList();
    }

    public async Task<ParcelamentoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Parcelamentos
            .Include(p => p.Parcelas)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("Parcelamento não encontrado", 404);
        return ToResponse(p);
    }

    public static ParcelamentoResponse ToResponse(Parcelamento p) => new(
        p.Id, p.CompraId, p.Descricao, p.ValorTotal, p.QtdParcelas,
        p.Status.ToString(),
        p.Parcelas.OrderBy(l => l.NumeroParcela).Select(l => new ParcelaResponse(
            l.Id, l.NumeroParcela ?? 0, l.Valor,
            l.DataVencimento, l.DataPagamento, l.Status.ToString()
        )).ToList());
}
```

- [ ] **Step 5: Rodar testes (devem passar)**

```bash
cd backend && dotnet test tests/GestorAI.Tests/ --filter "ParcelamentoServiceTests" -v
```
Esperado: `6 passed, 0 failed`

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Services/Compras/VencimentoCalculator.cs \
        backend/src/GestorAI.API/Services/Compras/ParcelamentoService.cs \
        backend/tests/GestorAI.Tests/Services/ParcelamentoServiceTests.cs
git commit -m "feat(compras): add ParcelamentoService and VencimentoCalculator with tests"
```

---

## Task 8: Atualizar FornecedorService

**Files:**
- Modify: `Services/Fornecedores/FornecedorService.cs`

- [ ] **Step 1: Atualizar o arquivo completo**

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
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
                (f.CnpjCpf != null && f.CnpjCpf.Contains(busca)) ||
                (f.NomeFantasia != null && f.NomeFantasia.Contains(busca)));

        return await query.OrderBy(f => f.Nome).Select(f => ToResponse(f)).ToListAsync(ct);
    }

    public async Task<FornecedorResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var f = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new AppException("Fornecedor não encontrado", 404);
        return ToResponse(f);
    }

    public async Task<FornecedorResponse> CreateAsync(CreateFornecedorRequest req, CancellationToken ct)
    {
        var fornecedor = new Fornecedor
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            RazaoSocial = req.RazaoSocial,
            NomeFantasia = req.NomeFantasia,
            CnpjCpf = req.CnpjCpf,
            InscricaoEstadual = req.InscricaoEstadual,
            Telefone = req.Telefone,
            Whatsapp = req.Whatsapp,
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
        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new AppException("Fornecedor não encontrado", 404);

        fornecedor.Nome = req.Nome;
        fornecedor.RazaoSocial = req.RazaoSocial;
        fornecedor.NomeFantasia = req.NomeFantasia;
        fornecedor.CnpjCpf = req.CnpjCpf;
        fornecedor.InscricaoEstadual = req.InscricaoEstadual;
        fornecedor.Telefone = req.Telefone;
        fornecedor.Whatsapp = req.Whatsapp;
        fornecedor.Email = req.Email;
        fornecedor.Logradouro = req.Logradouro;
        fornecedor.Cidade = req.Cidade;
        fornecedor.Uf = req.Uf;
        fornecedor.Cep = req.Cep;
        fornecedor.Contato = req.Contato;
        fornecedor.Observacoes = req.Observacoes;
        if (!string.IsNullOrEmpty(req.Status) && Enum.TryParse<StatusFornecedor>(req.Status, out var s))
            fornecedor.Status = s;

        await db.SaveChangesAsync(ct);
        return ToResponse(fornecedor);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var fornecedor = await db.Fornecedores.FirstOrDefaultAsync(f => f.Id == id, ct)
            ?? throw new AppException("Fornecedor não encontrado", 404);
        db.Fornecedores.Remove(fornecedor);
        await db.SaveChangesAsync(ct);
    }

    private static FornecedorResponse ToResponse(Fornecedor f) => new(
        f.Id, f.Nome, f.RazaoSocial, f.NomeFantasia, f.CnpjCpf,
        f.InscricaoEstadual, f.Telefone, f.Whatsapp, f.Email,
        f.Logradouro, f.Cidade, f.Uf, f.Cep,
        f.Contato, f.Observacoes, f.Status.ToString(), f.DataCadastro);
}
```

- [ ] **Step 2: Build e testes de fornecedor existentes**

```bash
cd backend && dotnet build -q && dotnet test tests/GestorAI.Tests/ --filter "FornecedorServiceTests" -v
```
Esperado: `Build succeeded`, testes passam (os testes existentes podem precisar atualizar os construtores de record — corrigir se necessário adicionando os novos campos opcionais como `null`).

- [ ] **Step 3: Commit**

```bash
git add backend/src/GestorAI.API/Services/Fornecedores/FornecedorService.cs
git commit -m "feat(compras): update FornecedorService with new fields"
```

---

## Task 9: Atualizar LancamentoService

**Files:**
- Modify: `Services/Financeiro/LancamentoService.cs`

- [ ] **Step 1: Injetar ParcelamentoService e atualizar PagarAsync**

Alterar a assinatura da classe para injetar `ParcelamentoService`:

```csharp
public class LancamentoService(AppDbContext db, TenantContext tenantContext, ParcelamentoService parcelamentoService)
```

Substituir o método `PagarAsync` pelo seguinte:

```csharp
    public async Task<LancamentoResponse> PagarAsync(
        Guid id, PagarLancamentoRequest req, CancellationToken ct)
    {
        var lancamento = await db.Lancamentos.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (lancamento.Status == StatusLancamento.Pago)
            throw new AppException("Lançamento já está pago.");
        if (lancamento.Status == StatusLancamento.Cancelado)
            throw new AppException("Lançamento cancelado não pode ser pago.");

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        lancamento.Status = StatusLancamento.Pago;
        lancamento.DataPagamento = req.DataPagamento;
        await db.SaveChangesAsync(ct);

        if (lancamento.ParcelamentoId.HasValue)
            await parcelamentoService.RecalcularStatusAsync(lancamento.ParcelamentoId.Value, ct);

        if (tx is not null) await tx.CommitAsync(ct);

        return ToResponse(lancamento, DateTime.UtcNow.Date);
    }
```

Adicionar `using GestorAI.API.Services.Compras;` no topo do arquivo.

- [ ] **Step 2: Atualizar Program.cs para injetar ParcelamentoService em LancamentoService**

Em `Program.cs`, adicionar após `builder.Services.AddScoped<CategoriaLancamentoService>();`:

```csharp
// Services — Compras
builder.Services.AddScoped<ParcelamentoService>();
```

Nota: o restante dos serviços de compras será adicionado na Task 13.

- [ ] **Step 3: Build**

```bash
cd backend/src/GestorAI.API && dotnet build -q
```
Esperado: `Build succeeded.`

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Services/Financeiro/LancamentoService.cs \
        backend/src/GestorAI.API/Program.cs
git commit -m "feat(compras): integrate ParcelamentoService into LancamentoService.PagarAsync"
```

---

## Task 10: CompraService + Tests

**Files:**
- Create: `Services/Compras/CompraService.cs`
- Create: `Services/Compras/CreateCompraValidator.cs`
- Create: `tests/Services/CompraServiceTests.cs`

- [ ] **Step 1: Escrever testes primeiro**

```csharp
// tests/Services/CompraServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Compras;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class CompraServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, CompraService svc) Setup()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        var parcelamentoSvc = new ParcelamentoService(db, tenant);
        return (db, new CompraService(db, tenant, parcelamentoSvc));
    }

    private Fornecedor SeedFornecedor(AppDbContext db)
    {
        var f = new Fornecedor { EmpresaId = _empresaId, Nome = "Dist. ABC" };
        db.Fornecedores.Add(f);
        db.SaveChanges();
        return f;
    }

    private Produto SeedProduto(AppDbContext db)
    {
        var cat = new Categoria { EmpresaId = _empresaId, Nome = "Geral" };
        db.Categorias.Add(cat);
        var p = new Produto
        {
            EmpresaId = _empresaId, CategoriaId = cat.Id,
            Nome = "Produto X", EstoqueAtual = 10m, CustoMedio = 5m,
        };
        db.Produtos.Add(p);
        db.SaveChanges();
        return p;
    }

    [Fact]
    public async Task CreateAsync_CriaCompraRascunho()
    {
        var (db, svc) = Setup();
        var fornecedor = SeedFornecedor(db);

        var req = new CreateCompraRequest(
            FornecedorId: fornecedor.Id,
            Data: new DateTime(2026, 6, 1),
            TipoCompra: "Mercadoria",
            NumeroNota: "001",
            CondicaoPagamento: "AVista",
            FormaPagamento: "PIX",
            QtdParcelas: null,
            ParcelasPersonalizadas: null,
            PedidoCompraId: null,
            Observacoes: null,
            Itens: [new ItemCompraRequest(null, "Produto Genérico", "ConsumoInterno",
                2m, 10m, 0m, 0m, 0m, "Material de Escritório", null)]);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal(StatusCompra.Rascunho.ToString(), result.Status);
        Assert.Equal(20m, result.ValorTotal);
        Assert.Equal(1, result.Numero);
    }

    [Fact]
    public async Task ConfirmarAsync_AtualizaEstoque_E_GeraParcelamento()
    {
        var (db, svc) = Setup();
        var fornecedor = SeedFornecedor(db);
        var produto = SeedProduto(db);

        var req = new CreateCompraRequest(
            FornecedorId: fornecedor.Id,
            Data: new DateTime(2026, 6, 1),
            TipoCompra: "Mercadoria",
            NumeroNota: null,
            CondicaoPagamento: "AVista",
            FormaPagamento: "PIX",
            QtdParcelas: null,
            ParcelasPersonalizadas: null,
            PedidoCompraId: null,
            Observacoes: null,
            Itens: [new ItemCompraRequest(produto.Id, "Produto X", "EstoqueParaVenda",
                5m, 8m, 0m, 0m, 0m, "Mercadorias", null)]);

        var compra = await svc.CreateAsync(req, default);
        var confirmada = await svc.ConfirmarAsync(compra.Id, default);

        Assert.Equal(StatusCompra.Confirmada.ToString(), confirmada.Status);
        Assert.NotNull(confirmada.ParcelamentoId);

        var produtoAtualizado = await db.Produtos.IgnoreQueryFilters()
            .FirstAsync(p => p.Id == produto.Id);
        Assert.Equal(15m, produtoAtualizado.EstoqueAtual);

        // custo medio: (10 * 5 + 5 * 8) / 15 = 90 / 15 = 6
        Assert.Equal(6m, produtoAtualizado.CustoMedio);

        var lancamentos = await db.Lancamentos.IgnoreQueryFilters()
            .Where(l => l.ParcelamentoId == confirmada.ParcelamentoId).ToListAsync();
        Assert.Single(lancamentos);
        Assert.Equal(40m, lancamentos[0].Valor);
    }

    [Fact]
    public async Task ConfirmarAsync_JaConfirmada_LancaException()
    {
        var (db, svc) = Setup();
        var fornecedor = SeedFornecedor(db);

        var req = new CreateCompraRequest(fornecedor.Id, DateTime.Today, "Mercadoria",
            null, "AVista", "PIX", null, null, null, null,
            [new ItemCompraRequest(null, "Item", "ConsumoInterno", 1m, 10m, 0m, 0m, 0m, null, null)]);

        var compra = await svc.CreateAsync(req, default);
        await svc.ConfirmarAsync(compra.Id, default);

        await Assert.ThrowsAsync<AppException>(() =>
            svc.ConfirmarAsync(compra.Id, default));
    }

    [Fact]
    public async Task CancelarAsync_CancelaParcelasPendentes()
    {
        var (db, svc) = Setup();
        var fornecedor = SeedFornecedor(db);

        var req = new CreateCompraRequest(fornecedor.Id, DateTime.Today, "Mercadoria",
            null, "30_60_90d", "Boleto", null, null, null, null,
            [new ItemCompraRequest(null, "Item", "ConsumoInterno", 1m, 300m, 0m, 0m, 0m, null, null)]);

        var compra = await svc.CreateAsync(req, default);
        var confirmada = await svc.ConfirmarAsync(compra.Id, default);
        await svc.CancelarAsync(compra.Id, default);

        var lancamentos = await db.Lancamentos.IgnoreQueryFilters()
            .Where(l => l.ParcelamentoId == confirmada.ParcelamentoId).ToListAsync();
        Assert.All(lancamentos, l => Assert.Equal(StatusLancamento.Cancelado, l.Status));
    }
}
```

- [ ] **Step 2: Rodar testes (devem falhar)**

```bash
cd backend && dotnet test tests/GestorAI.Tests/ --filter "CompraServiceTests" -v
```
Esperado: Erro de compilação ou falha — `CompraService` não existe.

- [ ] **Step 3: Criar CreateCompraValidator.cs**

```csharp
using FluentValidation;
using GestorAI.API.DTOs.Compras;

namespace GestorAI.API.Services.Compras;

public class CreateCompraValidator : AbstractValidator<CreateCompraRequest>
{
    public CreateCompraValidator()
    {
        RuleFor(x => x.FornecedorId).NotEmpty();
        RuleFor(x => x.TipoCompra).NotEmpty();
        RuleFor(x => x.CondicaoPagamento).NotEmpty();
        RuleFor(x => x.FormaPagamento).NotEmpty();
        RuleFor(x => x.Itens).NotEmpty().WithMessage("A compra deve ter pelo menos um item.");
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.Descricao).NotEmpty();
            item.RuleFor(i => i.Quantidade).GreaterThan(0);
            item.RuleFor(i => i.ValorUnitario).GreaterThanOrEqualTo(0);
        });
    }
}
```

- [ ] **Step 4: Criar CompraService.cs**

```csharp
using System.Text.Json;
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Compras;

public class CompraService(AppDbContext db, TenantContext tc, ParcelamentoService parcelamentoSvc)
{
    public async Task<List<CompraResponse>> ListAsync(
        string? status, Guid? fornecedorId, DateTime? de, DateTime? ate, CancellationToken ct)
    {
        var query = db.Compras.Include(c => c.Itens).Include(c => c.Fornecedor).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusCompra>(status, out var s))
            query = query.Where(c => c.Status == s);
        if (fornecedorId.HasValue)
            query = query.Where(c => c.FornecedorId == fornecedorId.Value);
        if (de.HasValue)
            query = query.Where(c => c.Data >= de.Value);
        if (ate.HasValue)
            query = query.Where(c => c.Data <= ate.Value);

        return await query.OrderByDescending(c => c.CriadaEm)
            .Select(c => ToResponse(c)).ToListAsync(ct);
    }

    public async Task<CompraResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Compras
            .Include(c => c.Itens)
            .Include(c => c.Fornecedor)
            .Include(c => c.Parcelamento)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Compra não encontrada", 404);
        return ToResponse(c);
    }

    public async Task<CompraResumoResponse> GetResumoAsync(CancellationToken ct)
    {
        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var totalMes = await db.Compras
            .Where(c => c.Status == StatusCompra.Confirmada && c.Data >= inicioMes)
            .SumAsync(c => (decimal?)c.ValorTotal, ct) ?? 0m;
        var qtd = await db.Compras
            .CountAsync(c => c.Status == StatusCompra.Confirmada && c.Data >= inicioMes, ct);
        var contasAPagar = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa && l.Status == StatusLancamento.Pendente
                && l.ParcelamentoId != null)
            .SumAsync(l => (decimal?)l.Valor, ct) ?? 0m;
        var fornecedoresAtivos = await db.Fornecedores
            .CountAsync(f => f.Status == StatusFornecedor.Ativo, ct);

        return new CompraResumoResponse(totalMes, qtd, contasAPagar, fornecedoresAtivos);
    }

    public async Task<CompraResponse> CreateAsync(CreateCompraRequest req, CancellationToken ct)
    {
        var nextNumero = (await db.Compras.MaxAsync(c => (int?)c.Numero, ct) ?? 0) + 1;

        var valorTotal = req.Itens.Sum(i =>
            i.Quantidade * i.ValorUnitario - i.Desconto + i.FreteRateado + i.Impostos);

        string? vencimentosJson = null;
        if (req.CondicaoPagamento == "Personalizado" && req.ParcelasPersonalizadas?.Any() == true)
            vencimentosJson = JsonSerializer.Serialize(req.ParcelasPersonalizadas.Select(p => p.DataVencimento).ToList());

        var compra = new Compra
        {
            EmpresaId = tc.EmpresaId,
            Numero = nextNumero,
            Data = req.Data,
            FornecedorId = req.FornecedorId,
            PedidoCompraId = req.PedidoCompraId,
            TipoCompra = req.TipoCompra,
            NumeroNota = req.NumeroNota,
            CondicaoPagamento = req.CondicaoPagamento,
            FormaPagamento = req.FormaPagamento,
            QtdParcelas = req.QtdParcelas,
            VencimentosJson = vencimentosJson,
            Status = StatusCompra.Rascunho,
            ValorTotal = valorTotal,
            Observacoes = req.Observacoes,
        };
        db.Compras.Add(compra);

        foreach (var item in req.Itens)
        {
            if (!Enum.TryParse<DestinoCompra>(item.DestinoCompra, out var destino))
                throw new AppException($"Destino inválido: {item.DestinoCompra}", 400);

            db.ItensCompra.Add(new ItemCompra
            {
                CompraId = compra.Id,
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao,
                DestinoCompra = destino,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
                Desconto = item.Desconto,
                FreteRateado = item.FreteRateado,
                Impostos = item.Impostos,
                ValorTotal = item.Quantidade * item.ValorUnitario - item.Desconto + item.FreteRateado + item.Impostos,
                CategoriaFinanceira = item.CategoriaFinanceira,
                CentroCusto = item.CentroCusto,
            });
        }

        await db.SaveChangesAsync(ct);
        return await GetAsync(compra.Id, ct);
    }

    public async Task<CompraResponse> UpdateAsync(Guid id, UpdateCompraRequest req, CancellationToken ct)
    {
        var compra = await db.Compras.Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Compra não encontrada", 404);

        if (compra.Status != StatusCompra.Rascunho)
            throw new AppException("Apenas compras em rascunho podem ser editadas", 400);

        string? vencimentosJson = null;
        if (req.CondicaoPagamento == "Personalizado" && req.ParcelasPersonalizadas?.Any() == true)
            vencimentosJson = JsonSerializer.Serialize(req.ParcelasPersonalizadas.Select(p => p.DataVencimento).ToList());

        compra.FornecedorId = req.FornecedorId;
        compra.Data = req.Data;
        compra.TipoCompra = req.TipoCompra;
        compra.NumeroNota = req.NumeroNota;
        compra.CondicaoPagamento = req.CondicaoPagamento;
        compra.FormaPagamento = req.FormaPagamento;
        compra.QtdParcelas = req.QtdParcelas;
        compra.VencimentosJson = vencimentosJson;
        compra.PedidoCompraId = req.PedidoCompraId;
        compra.Observacoes = req.Observacoes;

        db.ItensCompra.RemoveRange(compra.Itens);
        foreach (var item in req.Itens)
        {
            if (!Enum.TryParse<DestinoCompra>(item.DestinoCompra, out var destino))
                throw new AppException($"Destino inválido: {item.DestinoCompra}", 400);

            db.ItensCompra.Add(new ItemCompra
            {
                CompraId = compra.Id,
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao,
                DestinoCompra = destino,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
                Desconto = item.Desconto,
                FreteRateado = item.FreteRateado,
                Impostos = item.Impostos,
                ValorTotal = item.Quantidade * item.ValorUnitario - item.Desconto + item.FreteRateado + item.Impostos,
                CategoriaFinanceira = item.CategoriaFinanceira,
                CentroCusto = item.CentroCusto,
            });
        }

        compra.ValorTotal = req.Itens.Sum(i =>
            i.Quantidade * i.ValorUnitario - i.Desconto + i.FreteRateado + i.Impostos);

        await db.SaveChangesAsync(ct);
        return await GetAsync(compra.Id, ct);
    }

    public async Task<CompraResponse> ConfirmarAsync(Guid id, CancellationToken ct)
    {
        var compra = await db.Compras.Include(c => c.Itens).Include(c => c.Fornecedor)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Compra não encontrada", 404);

        if (compra.Status != StatusCompra.Rascunho)
            throw new AppException("Apenas compras em rascunho podem ser confirmadas", 400);

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        foreach (var item in compra.Itens.Where(i => i.DestinoCompra == DestinoCompra.EstoqueParaVenda && i.ProdutoId.HasValue))
        {
            var produto = await db.Produtos.FindAsync([item.ProdutoId!.Value], ct)
                ?? throw new AppException($"Produto {item.ProdutoId} não encontrado", 404);

            var novoEstoque = produto.EstoqueAtual + item.Quantidade;
            if (novoEstoque > 0)
                produto.CustoMedio =
                    (produto.EstoqueAtual * produto.CustoMedio + item.Quantidade * item.ValorUnitario) / novoEstoque;
            produto.EstoqueAtual = novoEstoque;
            produto.AtualizadoEm = DateTime.UtcNow;

            db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                EmpresaId = tc.EmpresaId,
                ProdutoId = produto.Id,
                Tipo = TipoMovimentacao.Entrada,
                Quantidade = item.Quantidade,
                Origem = OrigemMovimentacao.Compra,
                Observacao = $"Compra #{compra.Numero}",
            });
        }

        List<DateTime>? personalizadas = compra.VencimentosJson != null
            ? JsonSerializer.Deserialize<List<DateTime>>(compra.VencimentosJson)
            : null;

        var vencimentos = VencimentoCalculator.Calcular(
            compra.CondicaoPagamento, compra.Data, compra.QtdParcelas, personalizadas);

        var descricao = $"Compra #{compra.Numero}";
        var categoria = compra.Itens.FirstOrDefault()?.CategoriaFinanceira ?? "Compras";

        parcelamentoSvc.Criar(compra.Id, descricao, compra.ValorTotal, vencimentos, categoria);

        compra.Status = StatusCompra.Confirmada;
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return await GetAsync(compra.Id, ct);
    }

    public async Task<CompraResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var compra = await db.Compras.Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Compra não encontrada", 404);

        if (compra.Status == StatusCompra.Cancelada)
            throw new AppException("Compra já está cancelada", 400);

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        if (compra.Status == StatusCompra.Confirmada)
        {
            foreach (var item in compra.Itens.Where(i => i.DestinoCompra == DestinoCompra.EstoqueParaVenda && i.ProdutoId.HasValue))
            {
                var produto = await db.Produtos.FindAsync([item.ProdutoId!.Value], ct);
                if (produto is not null)
                {
                    produto.EstoqueAtual -= item.Quantidade;
                    produto.AtualizadoEm = DateTime.UtcNow;
                    db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                    {
                        EmpresaId = tc.EmpresaId,
                        ProdutoId = produto.Id,
                        Tipo = TipoMovimentacao.Saida,
                        Quantidade = item.Quantidade,
                        Origem = OrigemMovimentacao.Manual,
                        Observacao = $"Cancelamento Compra #{compra.Numero}",
                    });
                }
            }

            var parcelamento = await db.Parcelamentos.FirstOrDefaultAsync(p => p.CompraId == compra.Id, ct);
            if (parcelamento is not null)
                parcelamentoSvc.Cancelar(parcelamento.Id);
        }

        compra.Status = StatusCompra.Cancelada;
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return await GetAsync(compra.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var compra = await db.Compras.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Compra não encontrada", 404);

        if (compra.Status != StatusCompra.Rascunho)
            throw new AppException("Apenas rascunhos podem ser removidos", 400);

        db.Compras.Remove(compra);
        await db.SaveChangesAsync(ct);
    }

    private static CompraResponse ToResponse(Compra c) => new(
        c.Id, c.Numero, c.Data,
        c.FornecedorId, c.Fornecedor?.Nome ?? "",
        c.PedidoCompraId, c.TipoCompra, c.NumeroNota,
        c.CondicaoPagamento, c.FormaPagamento, c.Status.ToString(),
        c.ValorTotal, c.Observacoes, c.CriadaEm,
        c.Itens.Select(i => new ItemCompraResponse(
            i.Id, i.ProdutoId, i.Descricao, i.DestinoCompra.ToString(),
            i.Quantidade, i.ValorUnitario, i.Desconto,
            i.FreteRateado, i.Impostos, i.ValorTotal,
            i.CategoriaFinanceira, i.CentroCusto)).ToList(),
        c.Parcelamento?.Id);
}
```

- [ ] **Step 5: Rodar testes**

```bash
cd backend && dotnet test tests/GestorAI.Tests/ --filter "CompraServiceTests" -v
```
Esperado: `4 passed, 0 failed`

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/Services/Compras/CompraService.cs \
        backend/src/GestorAI.API/Services/Compras/CreateCompraValidator.cs \
        backend/tests/GestorAI.Tests/Services/CompraServiceTests.cs
git commit -m "feat(compras): add CompraService with tests"
```

---

## Task 11: PedidoCompraService + Tests

**Files:**
- Create: `Services/Compras/PedidoCompraService.cs`
- Create: `Services/Compras/CreatePedidoCompraValidator.cs`
- Create: `tests/Services/PedidoCompraServiceTests.cs`

- [ ] **Step 1: Escrever testes**

```csharp
// tests/Services/PedidoCompraServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Compras;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class PedidoCompraServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, PedidoCompraService svc) Setup()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);
        var parcelamentoSvc = new ParcelamentoService(db, tenant);
        var compraSvc = new CompraService(db, tenant, parcelamentoSvc);
        return (db, new PedidoCompraService(db, tenant, compraSvc));
    }

    private Fornecedor SeedFornecedor(AppDbContext db)
    {
        var f = new Fornecedor { EmpresaId = _empresaId, Nome = "Dist. ABC" };
        db.Fornecedores.Add(f);
        db.SaveChanges();
        return f;
    }

    [Fact]
    public async Task CreateAsync_CriaPedidoRascunho()
    {
        var (db, svc) = Setup();
        var fornecedor = SeedFornecedor(db);

        var req = new CreatePedidoCompraRequest(
            FornecedorId: fornecedor.Id,
            Data: DateTime.Today,
            Observacoes: null,
            Itens: [new ItemPedidoCompraRequest(null, "Caneta", 10m, 2m)]);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal(StatusPedidoCompra.Rascunho.ToString(), result.Status);
        Assert.Equal(20m, result.ValorEstimado);
        Assert.Single(result.Itens);
    }

    [Fact]
    public async Task AprovarAsync_MudaStatusParaAprovado()
    {
        var (db, svc) = Setup();
        var fornecedor = SeedFornecedor(db);
        var req = new CreatePedidoCompraRequest(fornecedor.Id, DateTime.Today, null,
            [new ItemPedidoCompraRequest(null, "Item", 1m, 10m)]);
        var pedido = await svc.CreateAsync(req, default);

        var result = await svc.AprovarAsync(pedido.Id, default);

        Assert.Equal(StatusPedidoCompra.Aprovado.ToString(), result.Status);
    }

    [Fact]
    public async Task ConverterEmCompraAsync_CriaCompraVinculada()
    {
        var (db, svc) = Setup();
        var fornecedor = SeedFornecedor(db);
        var req = new CreatePedidoCompraRequest(fornecedor.Id, DateTime.Today, null,
            [new ItemPedidoCompraRequest(null, "Produto Y", 3m, 15m)]);
        var pedido = await svc.CreateAsync(req, default);
        await svc.AprovarAsync(pedido.Id, default);

        var compra = await svc.ConverterEmCompraAsync(pedido.Id, default);

        Assert.Equal(pedido.Id, compra.PedidoCompraId);
        Assert.Equal(StatusCompra.Rascunho.ToString(), compra.Status);
        Assert.Single(compra.Itens);
        Assert.Equal("Produto Y", compra.Itens[0].Descricao);
    }
}
```

- [ ] **Step 2: Criar CreatePedidoCompraValidator.cs**

```csharp
using FluentValidation;
using GestorAI.API.DTOs.Compras;

namespace GestorAI.API.Services.Compras;

public class CreatePedidoCompraValidator : AbstractValidator<CreatePedidoCompraRequest>
{
    public CreatePedidoCompraValidator()
    {
        RuleFor(x => x.FornecedorId).NotEmpty();
        RuleFor(x => x.Itens).NotEmpty().WithMessage("O pedido deve ter pelo menos um item.");
        RuleForEach(x => x.Itens).ChildRules(item =>
        {
            item.RuleFor(i => i.Descricao).NotEmpty();
            item.RuleFor(i => i.Quantidade).GreaterThan(0);
        });
    }
}
```

- [ ] **Step 3: Criar PedidoCompraService.cs**

```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Compras;

public class PedidoCompraService(AppDbContext db, TenantContext tc, CompraService compraSvc)
{
    public async Task<List<PedidoCompraResponse>> ListAsync(
        string? status, Guid? fornecedorId, CancellationToken ct)
    {
        var query = db.PedidosCompra.Include(p => p.Itens).Include(p => p.Fornecedor).AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusPedidoCompra>(status, out var s))
            query = query.Where(p => p.Status == s);
        if (fornecedorId.HasValue)
            query = query.Where(p => p.FornecedorId == fornecedorId.Value);

        return await query.OrderByDescending(p => p.CriadoEm)
            .Select(p => ToResponse(p)).ToListAsync(ct);
    }

    public async Task<PedidoCompraResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.PedidosCompra.Include(p => p.Itens).Include(p => p.Fornecedor)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("Pedido de compra não encontrado", 404);
        return ToResponse(p);
    }

    public async Task<PedidoCompraResponse> CreateAsync(CreatePedidoCompraRequest req, CancellationToken ct)
    {
        var nextNumero = (await db.PedidosCompra.MaxAsync(p => (int?)p.Numero, ct) ?? 0) + 1;
        var valorEstimado = req.Itens.Sum(i => i.Quantidade * i.ValorEstimado);

        var pedido = new PedidoCompra
        {
            EmpresaId = tc.EmpresaId,
            Numero = nextNumero,
            Data = req.Data,
            FornecedorId = req.FornecedorId,
            Status = StatusPedidoCompra.Rascunho,
            ValorEstimado = valorEstimado,
            Observacoes = req.Observacoes,
        };
        db.PedidosCompra.Add(pedido);

        foreach (var item in req.Itens)
            db.ItensPedidoCompra.Add(new ItemPedidoCompra
            {
                PedidoCompraId = pedido.Id,
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                ValorEstimado = item.ValorEstimado,
            });

        await db.SaveChangesAsync(ct);
        return await GetAsync(pedido.Id, ct);
    }

    public async Task<PedidoCompraResponse> UpdateAsync(Guid id, UpdatePedidoCompraRequest req, CancellationToken ct)
    {
        var pedido = await db.PedidosCompra.Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("Pedido de compra não encontrado", 404);

        if (pedido.Status == StatusPedidoCompra.Cancelado)
            throw new AppException("Pedido cancelado não pode ser editado", 400);

        pedido.FornecedorId = req.FornecedorId;
        pedido.Data = req.Data;
        pedido.Observacoes = req.Observacoes;
        db.ItensPedidoCompra.RemoveRange(pedido.Itens);

        foreach (var item in req.Itens)
            db.ItensPedidoCompra.Add(new ItemPedidoCompra
            {
                PedidoCompraId = pedido.Id,
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                ValorEstimado = item.ValorEstimado,
            });

        pedido.ValorEstimado = req.Itens.Sum(i => i.Quantidade * i.ValorEstimado);
        await db.SaveChangesAsync(ct);
        return await GetAsync(pedido.Id, ct);
    }

    public async Task<PedidoCompraResponse> AprovarAsync(Guid id, CancellationToken ct)
    {
        var pedido = await db.PedidosCompra.FindAsync([id], ct)
            ?? throw new AppException("Pedido de compra não encontrado", 404);

        if (pedido.Status == StatusPedidoCompra.Cancelado)
            throw new AppException("Pedido cancelado não pode ser aprovado", 400);

        pedido.Status = StatusPedidoCompra.Aprovado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<CompraResponse> ConverterEmCompraAsync(Guid id, CancellationToken ct)
    {
        var pedido = await db.PedidosCompra.Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("Pedido de compra não encontrado", 404);

        if (pedido.Status != StatusPedidoCompra.Aprovado)
            throw new AppException("Apenas pedidos aprovados podem ser convertidos em compra", 400);

        var req = new CreateCompraRequest(
            FornecedorId: pedido.FornecedorId,
            Data: DateTime.UtcNow,
            TipoCompra: "Mercadoria",
            NumeroNota: null,
            CondicaoPagamento: "AVista",
            FormaPagamento: "PIX",
            QtdParcelas: null,
            ParcelasPersonalizadas: null,
            PedidoCompraId: pedido.Id,
            Observacoes: $"Convertido do Pedido #{pedido.Numero}",
            Itens: pedido.Itens.Select(i => new ItemCompraRequest(
                i.ProdutoId, i.Descricao, "ConsumoInterno",
                i.Quantidade, i.ValorEstimado, 0m, 0m, 0m, null, null
            )).ToList());

        return await compraSvc.CreateAsync(req, ct);
    }

    public async Task<PedidoCompraResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var pedido = await db.PedidosCompra.FindAsync([id], ct)
            ?? throw new AppException("Pedido de compra não encontrado", 404);

        if (pedido.Status == StatusPedidoCompra.RecebidoTotalmente)
            throw new AppException("Pedido já recebido não pode ser cancelado", 400);

        pedido.Status = StatusPedidoCompra.Cancelado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    private static PedidoCompraResponse ToResponse(PedidoCompra p) => new(
        p.Id, p.Numero, p.Data, p.FornecedorId, p.Fornecedor?.Nome ?? "",
        p.Status.ToString(), p.ValorEstimado, p.Observacoes, p.CriadoEm,
        p.Itens.Select(i => new ItemPedidoCompraResponse(
            i.Id, i.ProdutoId, i.Descricao, i.Quantidade, i.ValorEstimado)).ToList());
}
```

- [ ] **Step 4: Rodar testes**

```bash
cd backend && dotnet test tests/GestorAI.Tests/ --filter "PedidoCompraServiceTests" -v
```
Esperado: `3 passed, 0 failed`

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Services/Compras/PedidoCompraService.cs \
        backend/src/GestorAI.API/Services/Compras/CreatePedidoCompraValidator.cs \
        backend/tests/GestorAI.Tests/Services/PedidoCompraServiceTests.cs
git commit -m "feat(compras): add PedidoCompraService with tests"
```

---

## Task 12: ComprasDashboardService

**Files:**
- Create: `Services/Compras/ComprasDashboardService.cs`

- [ ] **Step 1: Criar ComprasDashboardService.cs**

```csharp
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Compras;

public class ComprasDashboardService(AppDbContext db, TenantContext tc)
{
    public async Task<ComprasDashboardResponse> GetAsync(DateTime de, DateTime ate, CancellationToken ct)
    {
        var compras = await db.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.Itens)
            .Where(c => c.Status == StatusCompra.Confirmada && c.Data >= de && c.Data <= ate)
            .ToListAsync(ct);

        var inicioMes = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var inicioAno = new DateTime(DateTime.UtcNow.Year, 1, 1);

        var totalMes = compras.Where(c => c.Data >= inicioMes).Sum(c => c.ValorTotal);
        var totalAno = compras.Where(c => c.Data >= inicioAno).Sum(c => c.ValorTotal);
        var ticketMedio = compras.Any() ? compras.Average(c => c.ValorTotal) : 0m;
        var qtdCompras = compras.Count;
        var fornecedoresAtivos = await db.Fornecedores
            .CountAsync(f => f.Status == StatusFornecedor.Ativo, ct);

        var evolucao = compras
            .GroupBy(c => new { c.Data.Year, c.Data.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new EvolucaoMensalCompraItem(
                $"{g.Key.Month:D2}/{g.Key.Year}", g.Sum(c => c.ValorTotal), g.Count()))
            .ToList();

        var porFornecedor = compras
            .GroupBy(c => c.Fornecedor?.Nome ?? "Desconhecido")
            .Select(g => new ComprasPorFornecedorItem(g.Key, g.Sum(c => c.ValorTotal)))
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();

        var topProdutos = compras
            .SelectMany(c => c.Itens)
            .Where(i => i.ProdutoId.HasValue)
            .GroupBy(i => i.Descricao)
            .Select(g => new TopProdutoCompradoItem(g.Key, g.Sum(i => i.Quantidade)))
            .OrderByDescending(x => x.Quantidade)
            .Take(10)
            .ToList();

        return new ComprasDashboardResponse(
            totalMes, totalAno, ticketMedio, qtdCompras,
            fornecedoresAtivos, evolucao, porFornecedor, topProdutos);
    }
}
```

- [ ] **Step 2: Build**

```bash
cd backend/src/GestorAI.API && dotnet build -q
```
Esperado: `Build succeeded.`

- [ ] **Step 3: Commit**

```bash
git add backend/src/GestorAI.API/Services/Compras/ComprasDashboardService.cs
git commit -m "feat(compras): add ComprasDashboardService"
```

---

## Task 13: Endpoints + Program.cs

**Files:**
- Create: `Endpoints/ComprasEndpoints.cs`
- Create: `Endpoints/PedidosCompraEndpoints.cs`
- Create: `Endpoints/ParcelamentosEndpoints.cs`
- Modify: `Program.cs`

- [ ] **Step 1: Criar ComprasEndpoints.cs**

```csharp
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Services.Compras;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class ComprasEndpoints
{
    public static void MapCompras(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/compras").RequireAuthorization();

        group.MapGet("/", async (
            string? status, Guid? fornecedorId, DateTime? de, DateTime? ate,
            CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, fornecedorId, de, ate, ct)));

        group.MapGet("/resumo", async (CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetResumoAsync(ct)));

        group.MapGet("/dashboard", async (
            DateTime de, DateTime ate,
            ComprasDashboardService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(de, ate, ct)));

        group.MapGet("/{id:guid}", async (Guid id, CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateCompraRequest req, CompraService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/compras/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateCompraRequest>>();

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateCompraRequest req, CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapPost("/{id:guid}/confirmar", async (Guid id, CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConfirmarAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (Guid id, CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, CompraService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}
```

- [ ] **Step 2: Criar PedidosCompraEndpoints.cs**

```csharp
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Services.Compras;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class PedidosCompraEndpoints
{
    public static void MapPedidosCompra(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pedidos-compra").RequireAuthorization();

        group.MapGet("/", async (
            string? status, Guid? fornecedorId,
            PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, fornecedorId, ct)));

        group.MapGet("/{id:guid}", async (Guid id, PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreatePedidoCompraRequest req, PedidoCompraService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/pedidos-compra/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreatePedidoCompraRequest>>();

        group.MapPut("/{id:guid}", async (
            Guid id, UpdatePedidoCompraRequest req, PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapPost("/{id:guid}/aprovar", async (Guid id, PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.AprovarAsync(id, ct)));

        group.MapPost("/{id:guid}/converter", async (Guid id, PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConverterEmCompraAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (Guid id, PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));
    }
}
```

- [ ] **Step 3: Criar ParcelamentosEndpoints.cs**

```csharp
using GestorAI.API.Services.Compras;

namespace GestorAI.API.Endpoints;

public static class ParcelamentosEndpoints
{
    public static void MapParcelamentos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/parcelamentos").RequireAuthorization();

        group.MapGet("/", async (
            Guid? compraId, string? status,
            ParcelamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(compraId, status, ct)));

        group.MapGet("/{id:guid}", async (Guid id, ParcelamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));
    }
}
```

- [ ] **Step 4: Atualizar Program.cs**

Adicionar após `// Services — Compras` (já parcialmente adicionado na Task 9):

```csharp
builder.Services.AddScoped<CompraService>();
builder.Services.AddScoped<PedidoCompraService>();
builder.Services.AddScoped<ComprasDashboardService>();
builder.Services.AddScoped<IValidator<CreateCompraRequest>, CreateCompraValidator>();
builder.Services.AddScoped<IValidator<CreatePedidoCompraRequest>, CreatePedidoCompraValidator>();
```

Adicionar `using GestorAI.API.Services.Compras;` e `using GestorAI.API.DTOs.Compras;` no topo.

Adicionar antes de `app.Run()`:

```csharp
app.MapCompras();
app.MapPedidosCompra();
app.MapParcelamentos();
```

- [ ] **Step 5: Build final**

```bash
cd backend/src/GestorAI.API && dotnet build -q
```
Esperado: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Step 6: Rodar todos os testes**

```bash
cd backend && dotnet test tests/GestorAI.Tests/ -v
```
Esperado: todos passam.

- [ ] **Step 7: Commit final do backend**

```bash
git add backend/src/GestorAI.API/Endpoints/ComprasEndpoints.cs \
        backend/src/GestorAI.API/Endpoints/PedidosCompraEndpoints.cs \
        backend/src/GestorAI.API/Endpoints/ParcelamentosEndpoints.cs \
        backend/src/GestorAI.API/Program.cs
git commit -m "feat(compras): wire up all compras endpoints and DI registrations"
```

---

## Self-Review

**Cobertura da spec:**
- ✅ Fornecedor expandido (RazaoSocial, NomeFantasia, IE, Whatsapp, Status)
- ✅ PedidoCompra com fluxo de status e conversão em Compra
- ✅ Compra com itens, destinos e confirmação em transação única
- ✅ EstoqueParaVenda: atualiza estoque + custo médio + MovimentacaoEstoque
- ✅ Parcelamento criado ao confirmar com N Lancamentos
- ✅ LancamentoService.PagarAsync recalcula status do Parcelamento
- ✅ Cancelamento reverte estoque e cancela parcelas
- ✅ Dashboard de KPIs com evolução, fornecedores e top produtos
- ✅ Endpoint GET /api/compras/dashboard usa de/ate
- ✅ Numero auto-incremento por empresa (MAX+1 em transação)
- ✅ Testes para VencimentoCalculator, ParcelamentoService, CompraService, PedidoCompraService
