# Contratos e Cobranças — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Adicionar módulos de Contratos e Cobranças ao GestorAI ERP, desacoplados mas integráveis via "Gerar Cobranças".

**Architecture:** Opção B (ContaAzul-style) — contratos existem como documentos independentes; cobranças podem ser geradas a partir de contratos (com ContratoId) ou criadas avulsas. PDF de contrato retorna HTML (mesmo padrão do OrcamentoService). Segue padrões existentes: ITenantEntity, AppDbContext query filters, Minimal APIs, records DTOs, InMemory DB nos testes.

**Tech Stack:** .NET 10 Minimal APIs, EF Core + PostgreSQL, React + TailwindCSS, padrão existente do projeto.

---

## Mapa de arquivos

**Backend — criar:**
- `Domain/Enums/ContratoStatus.cs`
- `Domain/Enums/TipoCobranca.cs`
- `Domain/Enums/Periodicidade.cs`
- `Domain/Enums/CobrancaStatus.cs`
- `Domain/Entities/Contrato.cs`
- `Domain/Entities/ContratoItem.cs`
- `Domain/Entities/Cobranca.cs`
- `DTOs/Contratos/ContratoDto.cs`
- `DTOs/Cobrancas/CobrancaDto.cs`
- `Services/Contratos/ContratoService.cs`
- `Services/Cobrancas/CobrancaService.cs`
- `Endpoints/ContratosEndpoints.cs`
- `Endpoints/CobrancasEndpoints.cs`

**Backend — modificar:**
- `Infrastructure/Data/AppDbContext.cs` — adicionar DbSets + query filters
- `Program.cs` — registrar services + endpoints

**Testes — criar:**
- `tests/GestorAI.Tests/Services/ContratoServiceTests.cs`
- `tests/GestorAI.Tests/Services/CobrancaServiceTests.cs`

**Frontend — criar:**
- `src/types/contrato.ts`
- `src/types/cobranca.ts`
- `src/hooks/useContratos.ts`
- `src/hooks/useCobrancas.ts`
- `src/pages/contratos/Contratos.tsx`
- `src/pages/contratos/NovoContrato.tsx`
- `src/pages/contratos/DetalheContrato.tsx`
- `src/pages/cobrancas/Cobrancas.tsx`
- `src/pages/cobrancas/NovaCobranca.tsx`
- `src/pages/cobrancas/DetalheCobranca.tsx`

**Frontend — modificar:**
- `src/router/index.tsx`
- `src/components/layout/Sidebar.tsx`

---

### Task 1: Enums e Entidades de Domínio

**Files:**
- Create: `backend/src/GestorAI.API/Domain/Enums/ContratoStatus.cs`
- Create: `backend/src/GestorAI.API/Domain/Enums/TipoCobranca.cs`
- Create: `backend/src/GestorAI.API/Domain/Enums/Periodicidade.cs`
- Create: `backend/src/GestorAI.API/Domain/Enums/CobrancaStatus.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/Contrato.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/ContratoItem.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/Cobranca.cs`

- [ ] **Step 1: Criar enums**

```csharp
// Domain/Enums/ContratoStatus.cs
namespace GestorAI.API.Domain.Enums;
public enum ContratoStatus { Rascunho, Ativo, Encerrado, Cancelado }

// Domain/Enums/TipoCobranca.cs
namespace GestorAI.API.Domain.Enums;
public enum TipoCobranca { Recorrente, ParceladoPrazoFixo }

// Domain/Enums/Periodicidade.cs
namespace GestorAI.API.Domain.Enums;
public enum Periodicidade { Mensal, Trimestral, Semestral, Anual }

// Domain/Enums/CobrancaStatus.cs
namespace GestorAI.API.Domain.Enums;
public enum CobrancaStatus { Pendente, Pago, Cancelado }
```

- [ ] **Step 2: Criar entidade Contrato e ContratoItem**

```csharp
// Domain/Entities/Contrato.cs
using GestorAI.API.Domain.Enums;
using GestorAI.API.Shared.MultiTenancy;

namespace GestorAI.API.Domain.Entities;

public class Contrato : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public int Numero { get; set; }
    public Guid ClienteId { get; set; }
    public required string Titulo { get; set; }
    public required string Objeto { get; set; }
    public TipoCobranca TipoCobranca { get; set; }
    public decimal Valor { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public Periodicidade Periodicidade { get; set; }
    public int DiaVencimento { get; set; }
    public ContratoStatus Status { get; set; } = ContratoStatus.Rascunho;
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public ICollection<ContratoItem> Itens { get; set; } = [];
}

// Domain/Entities/ContratoItem.cs
namespace GestorAI.API.Domain.Entities;

public class ContratoItem
{
    public Guid Id { get; set; }
    public Guid ContratoId { get; set; }
    public required string Descricao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
}
```

- [ ] **Step 3: Criar entidade Cobranca**

```csharp
// Domain/Entities/Cobranca.cs
using GestorAI.API.Domain.Enums;
using GestorAI.API.Shared.MultiTenancy;

namespace GestorAI.API.Domain.Entities;

public class Cobranca : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid? ContratoId { get; set; }
    public required string Referencia { get; set; }
    public decimal Valor { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public CobrancaStatus Status { get; set; } = CobrancaStatus.Pendente;
    public FormaPagamento? FormaPagamento { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public Contrato? Contrato { get; set; }
}
```

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Domain/
git commit -m "feat: add Contrato, ContratoItem, Cobranca entities and enums"
```

---

### Task 2: AppDbContext + Migração

**Files:**
- Modify: `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`

- [ ] **Step 1: Adicionar DbSets e query filters ao AppDbContext**

Adicionar ao final das propriedades DbSet (após linha `public DbSet<ConfiguracaoEmpresa> ConfiguracoesEmpresa...`):

```csharp
public DbSet<Contrato> Contratos => Set<Contrato>();
public DbSet<ContratoItem> ContratoItens => Set<ContratoItem>();
public DbSet<Cobranca> Cobrancas => Set<Cobranca>();
```

Adicionar ao final do `OnModelCreating` (após a linha do `ConfiguracaoEmpresa`):

```csharp
modelBuilder.Entity<Contrato>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
modelBuilder.Entity<Cobranca>().HasQueryFilter(e => e.EmpresaId == tenantContext.EmpresaId);
```

- [ ] **Step 2: Criar e aplicar migration**

```bash
cd backend
/usr/local/share/dotnet/dotnet ef migrations add AddContratosCobrancas \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API
/usr/local/share/dotnet/dotnet ef database update \
  --project src/GestorAI.API \
  --startup-project src/GestorAI.API
```

Expected: migration file criado em `src/GestorAI.API/Migrations/` e banco atualizado sem erros.

- [ ] **Step 3: Commit**

```bash
git add backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs
git add backend/src/GestorAI.API/Migrations/
git commit -m "feat: add Contratos and Cobrancas tables to database"
```

---

### Task 3: DTOs

**Files:**
- Create: `backend/src/GestorAI.API/DTOs/Contratos/ContratoDto.cs`
- Create: `backend/src/GestorAI.API/DTOs/Cobrancas/CobrancaDto.cs`

- [ ] **Step 1: Criar DTOs de Contratos**

```csharp
// DTOs/Contratos/ContratoDto.cs
namespace GestorAI.API.DTOs.Contratos;

public record ContratoItemRequest(
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario);

public record CreateContratoRequest(
    Guid ClienteId,
    string Titulo,
    string Objeto,
    string TipoCobranca,
    decimal Valor,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string Periodicidade,
    int DiaVencimento,
    string? Observacao,
    List<ContratoItemRequest> Itens);

public record UpdateContratoRequest(
    string? Observacao,
    List<ContratoItemRequest>? Itens);

public record GerarCobrancasRequest(
    DateOnly De,
    DateOnly Ate);

public record ContratoItemResponse(
    Guid Id,
    string Descricao,
    decimal Quantidade,
    decimal ValorUnitario);

public record ContratoResponse(
    Guid Id,
    int Numero,
    string ClienteNome,
    string ClienteWhatsapp,
    string Titulo,
    string Objeto,
    string TipoCobranca,
    decimal Valor,
    DateOnly DataInicio,
    DateOnly? DataFim,
    string Periodicidade,
    int DiaVencimento,
    string Status,
    string? Observacao,
    DateTime CriadoEm,
    List<ContratoItemResponse> Itens,
    decimal Total);

public record ContratoListItem(
    Guid Id,
    int Numero,
    string ClienteNome,
    string Titulo,
    string TipoCobranca,
    decimal Valor,
    string Status,
    DateOnly DataInicio,
    DateOnly? DataFim);
```

- [ ] **Step 2: Criar DTOs de Cobranças**

```csharp
// DTOs/Cobrancas/CobrancaDto.cs
namespace GestorAI.API.DTOs.Cobrancas;

public record CreateCobrancaRequest(
    Guid ClienteId,
    string Referencia,
    decimal Valor,
    DateOnly DataVencimento,
    string? Observacao);

public record PagarCobrancaRequest(
    DateTime DataPagamento,
    string FormaPagamento);

public record CobrancaResponse(
    Guid Id,
    string ClienteNome,
    string ClienteWhatsapp,
    Guid? ContratoId,
    string? ContratoTitulo,
    string Referencia,
    decimal Valor,
    DateOnly DataVencimento,
    DateTime? DataPagamento,
    string Status,
    string? FormaPagamento,
    string? Observacao,
    DateTime CriadoEm);

public record CobrancaListItem(
    Guid Id,
    string ClienteNome,
    Guid? ContratoId,
    string? ContratoTitulo,
    string Referencia,
    decimal Valor,
    DateOnly DataVencimento,
    string Status);

public record WhatsappUrlResponse(string Url);
```

- [ ] **Step 3: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/
git commit -m "feat: add Contratos and Cobrancas DTOs"
```

---

### Task 4: ContratoService + Testes

**Files:**
- Create: `backend/src/GestorAI.API/Services/Contratos/ContratoService.cs`
- Create: `backend/tests/GestorAI.Tests/Services/ContratoServiceTests.cs`

- [ ] **Step 1: Escrever testes que falham**

```csharp
// tests/GestorAI.Tests/Services/ContratoServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contratos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Contratos;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class ContratoServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, ContratoService svc) Setup()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        return (db, new ContratoService(db, tenant));
    }

    private Cliente CriarCliente(AppDbContext db)
    {
        var c = new Cliente { EmpresaId = _empresaId, Nome = "João", Whatsapp = "11999990000" };
        db.Clientes.Add(c);
        db.SaveChanges();
        return c;
    }

    [Fact]
    public async Task CreateAsync_PersistsAsRascunho()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var req = new CreateContratoRequest(
            cliente.Id, "Plano Mensal", "Serviços mensais", "Recorrente",
            500m, DateOnly.FromDateTime(DateTime.Today), null,
            "Mensal", 10, null,
            [new ContratoItemRequest("Consulta", 1, 500m)]);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal("Rascunho", result.Status);
        Assert.Equal(1, result.Numero);
        Assert.Equal(500m, result.Total);
    }

    [Fact]
    public async Task AtivarAsync_RascunhoVivoAtivo()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Numero = 1, Titulo = "T", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 100m,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 5,
            Status = ContratoStatus.Rascunho
        };
        contrato.Itens.Add(new ContratoItem { Descricao = "Serviço", Quantidade = 1, ValorUnitario = 100m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var result = await svc.AtivarAsync(contrato.Id, default);

        Assert.Equal("Ativo", result.Status);
    }

    [Fact]
    public async Task AtivarAsync_SemItens_LancaExcecao()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Numero = 1, Titulo = "T", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 100m,
            DataInicio = DateOnly.FromDateTime(DateTime.Today),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 5,
            Status = ContratoStatus.Rascunho
        };
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() => svc.AtivarAsync(contrato.Id, default));
    }

    [Fact]
    public async Task GerarCobrancasAsync_CriaCobrancasNoPeriodo()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Numero = 1, Titulo = "Mensal", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 200m,
            DataInicio = new DateOnly(2026, 1, 1),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 10,
            Status = ContratoStatus.Ativo
        };
        contrato.Itens.Add(new ContratoItem { Descricao = "S", Quantidade = 1, ValorUnitario = 200m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var req = new GerarCobrancasRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31));
        var result = await svc.GerarCobrancasAsync(contrato.Id, req, default);

        Assert.Equal(3, result.Count); // Jan, Fev, Mar
        Assert.All(result, c => Assert.Equal(200m, c.Valor));
        Assert.Equal(new DateOnly(2026, 1, 10), result[0].DataVencimento);
        Assert.Equal(new DateOnly(2026, 2, 10), result[1].DataVencimento);
        Assert.Equal(new DateOnly(2026, 3, 10), result[2].DataVencimento);
    }

    [Fact]
    public async Task GerarCobrancasAsync_NaoDuplica()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var contrato = new Contrato
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Numero = 1, Titulo = "Mensal", Objeto = "O",
            TipoCobranca = TipoCobranca.Recorrente, Valor = 200m,
            DataInicio = new DateOnly(2026, 1, 1),
            Periodicidade = Periodicidade.Mensal, DiaVencimento = 10,
            Status = ContratoStatus.Ativo
        };
        contrato.Itens.Add(new ContratoItem { Descricao = "S", Quantidade = 1, ValorUnitario = 200m });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync();

        var req = new GerarCobrancasRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 2, 28));
        await svc.GerarCobrancasAsync(contrato.Id, req, default);
        var result2 = await svc.GerarCobrancasAsync(contrato.Id, req, default);

        Assert.Empty(result2); // já existem, não duplica
        Assert.Equal(2, db.Cobrancas.Count());
    }
}
```

- [ ] **Step 2: Rodar testes — confirmar falha**

```bash
cd backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests --filter "ContratoServiceTests"
```

Expected: erro de compilação "ContratoService not found".

- [ ] **Step 3: Implementar ContratoService**

```csharp
// Services/Contratos/ContratoService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contratos;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contratos;

public class ContratoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ContratoListItem>> ListAsync(string? status, CancellationToken ct)
    {
        var query = db.Contratos.Include(c => c.Cliente).AsQueryable();
        if (status != null && Enum.TryParse<ContratoStatus>(status, out var s))
            query = query.Where(c => c.Status == s);
        return await query
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => ToListItem(c))
            .ToListAsync(ct);
    }

    public async Task<ContratoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Contratos
            .Include(c => c.Cliente)
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contrato não encontrado.", 404);
        return ToResponse(c);
    }

    public async Task<ContratoResponse> CreateAsync(CreateContratoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoCobranca>(req.TipoCobranca, out var tipo))
            throw new AppException($"TipoCobranca inválido: {req.TipoCobranca}.", 400);
        if (!Enum.TryParse<Periodicidade>(req.Periodicidade, out var periodicidade))
            throw new AppException($"Periodicidade inválida: {req.Periodicidade}.", 400);
        if (req.DiaVencimento < 1 || req.DiaVencimento > 28)
            throw new AppException("DiaVencimento deve ser entre 1 e 28.", 400);

        _ = await db.Clientes.FirstOrDefaultAsync(c => c.Id == req.ClienteId, ct)
            ?? throw new AppException("Cliente não encontrado.", 404);

        var numero = (await db.Contratos.MaxAsync(c => (int?)c.Numero, ct) ?? 0) + 1;

        var contrato = new Contrato
        {
            EmpresaId = tenantContext.EmpresaId,
            Numero = numero,
            ClienteId = req.ClienteId,
            Titulo = req.Titulo,
            Objeto = req.Objeto,
            TipoCobranca = tipo,
            Valor = req.Valor,
            DataInicio = req.DataInicio,
            DataFim = req.DataFim,
            Periodicidade = periodicidade,
            DiaVencimento = req.DiaVencimento,
            Observacao = req.Observacao,
        };

        foreach (var item in req.Itens)
            contrato.Itens.Add(new ContratoItem
            {
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
            });

        db.Contratos.Add(contrato);
        await db.SaveChangesAsync(ct);
        return await GetAsync(contrato.Id, ct);
    }

    public async Task<ContratoResponse> AtivarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status != ContratoStatus.Rascunho)
            throw new AppException("Apenas rascunhos podem ser ativados.", 400);
        if (!c.Itens.Any())
            throw new AppException("Contrato precisa ter pelo menos um item.", 400);
        if (c.TipoCobranca == TipoCobranca.ParceladoPrazoFixo && c.DataFim == null)
            throw new AppException("Contratos parcelados requerem DataFim.", 400);
        c.Status = ContratoStatus.Ativo;
        await db.SaveChangesAsync(ct);
        return ToResponse(c);
    }

    public async Task<ContratoResponse> EncerrarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status != ContratoStatus.Ativo)
            throw new AppException("Apenas contratos ativos podem ser encerrados.", 400);
        c.Status = ContratoStatus.Encerrado;
        await db.SaveChangesAsync(ct);
        return ToResponse(c);
    }

    public async Task<ContratoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status == ContratoStatus.Encerrado || c.Status == ContratoStatus.Cancelado)
            throw new AppException("Contrato já está encerrado ou cancelado.", 400);
        c.Status = ContratoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return ToResponse(c);
    }

    public async Task<List<CobrancaListItem>> GerarCobrancasAsync(
        Guid id, GerarCobrancasRequest req, CancellationToken ct)
    {
        var contrato = await db.Contratos
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contrato não encontrado.", 404);

        if (contrato.Status != ContratoStatus.Ativo)
            throw new AppException("Apenas contratos ativos podem gerar cobranças.", 400);

        var vencimentos = CalcularVencimentos(contrato, req.De, req.Ate);
        var existentes = await db.Cobrancas
            .Where(c => c.ContratoId == id)
            .Select(c => c.DataVencimento)
            .ToListAsync(ct);

        var allVencimentos = CalcularVencimentos(contrato,
            contrato.DataInicio,
            contrato.DataFim ?? req.Ate.AddYears(10));
        var totalParcelas = allVencimentos.Count;

        var novas = new List<Cobranca>();
        foreach (var venc in vencimentos.Where(v => !existentes.Contains(v)))
        {
            var parcela = allVencimentos.IndexOf(venc) + 1;
            var referencia = contrato.TipoCobranca == TipoCobranca.ParceladoPrazoFixo
                ? $"Parcela {parcela}/{totalParcelas} — {contrato.Titulo}"
                : GerarReferenciaRecorrente(contrato, venc);

            var valorCobranca = contrato.TipoCobranca == TipoCobranca.ParceladoPrazoFixo
                ? Math.Round(contrato.Valor / totalParcelas, 2)
                : contrato.Valor;

            var cobranca = new Cobranca
            {
                EmpresaId = tenantContext.EmpresaId,
                ClienteId = contrato.ClienteId,
                ContratoId = contrato.Id,
                Referencia = referencia,
                Valor = valorCobranca,
                DataVencimento = venc,
            };
            novas.Add(cobranca);
            db.Cobrancas.Add(cobranca);
        }

        await db.SaveChangesAsync(ct);

        return novas.Select(c => new CobrancaListItem(
            c.Id, contrato.Cliente!.Nome, c.ContratoId,
            contrato.Titulo, c.Referencia, c.Valor,
            c.DataVencimento, c.Status.ToString())).ToList();
    }

    public async Task<string> GetPdfHtmlAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Contratos
            .Include(c => c.Cliente)
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contrato não encontrado.", 404);

        var total = c.Itens.Sum(i => i.Quantidade * i.ValorUnitario);
        var linhas = string.Join("", c.Itens.Select(i =>
            $"<tr><td>{i.Descricao}</td><td>{i.Quantidade:N2}</td>" +
            $"<td>R$ {i.ValorUnitario:N2}</td><td>R$ {i.Quantidade * i.ValorUnitario:N2}</td></tr>"));

        return $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8">
            <style>
              body { font-family: sans-serif; padding: 40px; color: #111; max-width: 800px; margin: auto; }
              h1 { font-size: 20px; margin-bottom: 4px; }
              .meta { color: #555; font-size: 13px; margin-bottom: 24px; }
              table { width: 100%; border-collapse: collapse; margin-bottom: 16px; }
              th, td { border: 1px solid #ddd; padding: 8px; text-align: left; font-size: 13px; }
              th { background: #f5f5f5; }
              .total { text-align: right; font-weight: bold; font-size: 15px; margin-top: 8px; }
              .objeto { background: #f9f9f9; border: 1px solid #ddd; padding: 16px; margin: 16px 0; font-size: 13px; white-space: pre-wrap; }
              .assinatura { margin-top: 60px; display: flex; gap: 60px; }
              .assinatura div { border-top: 1px solid #333; padding-top: 8px; font-size: 12px; min-width: 200px; }
            </style>
            </head>
            <body>
              <h1>CONTRATO {{c.Numero:D3}} — {{c.Titulo}}</h1>
              <div class="meta">
                Cliente: {{c.Cliente?.Nome}}<br>
                Início: {{c.DataInicio:dd/MM/yyyy}}{{(c.DataFim.HasValue ? $" | Término: {c.DataFim:dd/MM/yyyy}" : "")}}<br>
                Tipo: {{c.TipoCobranca}} | Periodicidade: {{c.Periodicidade}} | Valor: R$ {{c.Valor:N2}}
              </div>
              <h2 style="font-size:14px">Objeto</h2>
              <div class="objeto">{{c.Objeto}}</div>
              <h2 style="font-size:14px">Itens</h2>
              <table>
                <thead><tr><th>Descrição</th><th>Qtd</th><th>Unit.</th><th>Total</th></tr></thead>
                <tbody>{{linhas}}</tbody>
              </table>
              <div class="total">Total: R$ {{total:N2}}</div>
              <div class="assinatura">
                <div>Contratante<br>{{c.Cliente?.Nome}}</div>
                <div>Contratada</div>
              </div>
            </body>
            </html>
            """;
    }

    private static List<DateOnly> CalcularVencimentos(Contrato contrato, DateOnly de, DateOnly ate)
    {
        var dia = contrato.DiaVencimento;
        var daysInStartMonth = DateTime.DaysInMonth(de.Year, de.Month);
        var primeiro = new DateOnly(de.Year, de.Month, Math.Min(dia, daysInStartMonth));
        if (primeiro < de)
            primeiro = AvançarPeriodo(primeiro, contrato.Periodicidade, dia);

        var vencimentos = new List<DateOnly>();
        var cursor = primeiro;
        var limite = contrato.DataFim.HasValue && contrato.DataFim.Value < ate
            ? contrato.DataFim.Value
            : ate;

        while (cursor <= limite)
        {
            vencimentos.Add(cursor);
            cursor = AvançarPeriodo(cursor, contrato.Periodicidade, dia);
        }

        return vencimentos;
    }

    private static DateOnly AvançarPeriodo(DateOnly data, Periodicidade periodicidade, int dia)
    {
        var meses = periodicidade switch
        {
            Periodicidade.Mensal => 1,
            Periodicidade.Trimestral => 3,
            Periodicidade.Semestral => 6,
            Periodicidade.Anual => 12,
            _ => 1
        };
        var next = data.AddMonths(meses);
        return new DateOnly(next.Year, next.Month, Math.Min(dia, DateTime.DaysInMonth(next.Year, next.Month)));
    }

    private static string GerarReferenciaRecorrente(Contrato contrato, DateOnly vencimento)
    {
        var cultura = new System.Globalization.CultureInfo("pt-BR");
        var periodo = contrato.Periodicidade switch
        {
            Periodicidade.Mensal => $"Mensalidade {vencimento.ToString("MMM/yyyy", cultura)}",
            Periodicidade.Trimestral => $"Trimestral {vencimento.ToString("MMM/yyyy", cultura)}",
            Periodicidade.Semestral => $"Semestral {vencimento.ToString("MMM/yyyy", cultura)}",
            Periodicidade.Anual => $"Anuidade {vencimento.Year}",
            _ => vencimento.ToString("MMM/yyyy", cultura)
        };
        return $"{periodo} — {contrato.Titulo}";
    }

    private async Task<Contrato> FindAsync(Guid id, CancellationToken ct)
    {
        return await db.Contratos
            .Include(c => c.Cliente)
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contrato não encontrado.", 404);
    }

    private static ContratoListItem ToListItem(Contrato c) => new(
        c.Id, c.Numero, c.Cliente?.Nome ?? "", c.Titulo,
        c.TipoCobranca.ToString(), c.Valor, c.Status.ToString(),
        c.DataInicio, c.DataFim);

    private static ContratoResponse ToResponse(Contrato c) => new(
        c.Id, c.Numero, c.Cliente?.Nome ?? "", c.Cliente?.Whatsapp ?? "",
        c.Titulo, c.Objeto, c.TipoCobranca.ToString(), c.Valor,
        c.DataInicio, c.DataFim, c.Periodicidade.ToString(),
        c.DiaVencimento, c.Status.ToString(), c.Observacao, c.CriadoEm,
        c.Itens.Select(i => new ContratoItemResponse(i.Id, i.Descricao, i.Quantidade, i.ValorUnitario)).ToList(),
        c.Itens.Sum(i => i.Quantidade * i.ValorUnitario));
}
```

- [ ] **Step 4: Rodar testes — confirmar sucesso**

```bash
cd backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests --filter "ContratoServiceTests"
```

Expected: 5 testes passando.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Services/Contratos/
git add backend/tests/GestorAI.Tests/Services/ContratoServiceTests.cs
git commit -m "feat: implement ContratoService with full business logic and tests"
```

---

### Task 5: CobrancaService + Testes

**Files:**
- Create: `backend/src/GestorAI.API/Services/Cobrancas/CobrancaService.cs`
- Create: `backend/tests/GestorAI.Tests/Services/CobrancaServiceTests.cs`

- [ ] **Step 1: Escrever testes que falham**

```csharp
// tests/GestorAI.Tests/Services/CobrancaServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class CobrancaServiceTests
{
    private readonly Guid _empresaId = Guid.NewGuid();

    private (AppDbContext db, CobrancaService svc) Setup()
    {
        var tenant = new TenantContext { EmpresaId = _empresaId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);
        return (db, new CobrancaService(db, tenant));
    }

    private Cliente CriarCliente(AppDbContext db)
    {
        var c = new Cliente { EmpresaId = _empresaId, Nome = "Ana", Whatsapp = "11988880000" };
        db.Clientes.Add(c);
        db.SaveChanges();
        return c;
    }

    [Fact]
    public async Task CreateAsync_PersistsAsPendente()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var req = new CreateCobrancaRequest(
            cliente.Id, "Mensalidade Jun/2026", 300m,
            new DateOnly(2026, 6, 10), null);

        var result = await svc.CreateAsync(req, default);

        Assert.Equal("Pendente", result.Status);
        Assert.Equal(300m, result.Valor);
    }

    [Fact]
    public async Task PagarAsync_SetaStatusPago()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var cobranca = new Cobranca
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Referencia = "Test", Valor = 100m,
            DataVencimento = new DateOnly(2026, 6, 10),
            Status = CobrancaStatus.Pendente
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();

        var req = new PagarCobrancaRequest(DateTime.UtcNow, "Pix");
        var result = await svc.PagarAsync(cobranca.Id, req, default);

        Assert.Equal("Pago", result.Status);
        Assert.Equal("Pix", result.FormaPagamento);
        Assert.NotNull(result.DataPagamento);
    }

    [Fact]
    public async Task PagarAsync_QuandoCancelado_LancaExcecao()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db);
        var cobranca = new Cobranca
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Referencia = "Test", Valor = 100m,
            DataVencimento = new DateOnly(2026, 6, 10),
            Status = CobrancaStatus.Cancelado
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<AppException>(() =>
            svc.PagarAsync(cobranca.Id, new PagarCobrancaRequest(DateTime.UtcNow, "Pix"), default));
    }

    [Fact]
    public async Task GetWhatsappUrlAsync_RetornaUrlCorreta()
    {
        var (db, svc) = Setup();
        var cliente = CriarCliente(db); // Whatsapp = "11988880000"
        var cobranca = new Cobranca
        {
            EmpresaId = _empresaId, ClienteId = cliente.Id,
            Referencia = "Mensalidade Jun/2026", Valor = 300m,
            DataVencimento = new DateOnly(2026, 6, 10),
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync();

        var result = await svc.GetWhatsappUrlAsync(cobranca.Id, default);

        Assert.StartsWith("https://wa.me/5511988880000", result.Url);
        Assert.Contains("Mensalidade+Jun%2F2026", result.Url);
    }
}
```

- [ ] **Step 2: Rodar testes — confirmar falha**

```bash
cd backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests --filter "CobrancaServiceTests"
```

Expected: erro de compilação "CobrancaService not found".

- [ ] **Step 3: Implementar CobrancaService**

```csharp
// Services/Cobrancas/CobrancaService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Cobrancas;

public class CobrancaService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<CobrancaListItem>> ListAsync(
        string? status, Guid? clienteId, string? mes, CancellationToken ct)
    {
        var query = db.Cobrancas
            .Include(c => c.Cliente)
            .Include(c => c.Contrato)
            .AsQueryable();

        if (clienteId.HasValue)
            query = query.Where(c => c.ClienteId == clienteId.Value);

        if (mes != null && DateOnly.TryParseExact(mes + "-01", "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var mesDate))
        {
            var fimMes = mesDate.AddMonths(1).AddDays(-1);
            query = query.Where(c => c.DataVencimento >= mesDate && c.DataVencimento <= fimMes);
        }

        var list = await query.OrderBy(c => c.DataVencimento).ToListAsync(ct);

        if (status != null)
        {
            var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
            list = status switch
            {
                "Vencido" => list.Where(c => c.Status == CobrancaStatus.Pendente && c.DataVencimento < hoje).ToList(),
                _ when Enum.TryParse<CobrancaStatus>(status, out var s) => list.Where(c => c.Status == s).ToList(),
                _ => list
            };
        }

        return list.Select(ToListItem).ToList();
    }

    public async Task<CobrancaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Cobrancas
            .Include(c => c.Cliente)
            .Include(c => c.Contrato)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);
        return ToResponse(c);
    }

    public async Task<CobrancaResponse> CreateAsync(CreateCobrancaRequest req, CancellationToken ct)
    {
        _ = await db.Clientes.FirstOrDefaultAsync(c => c.Id == req.ClienteId, ct)
            ?? throw new AppException("Cliente não encontrado.", 404);

        var cobranca = new Cobranca
        {
            EmpresaId = tenantContext.EmpresaId,
            ClienteId = req.ClienteId,
            Referencia = req.Referencia,
            Valor = req.Valor,
            DataVencimento = req.DataVencimento,
            Observacao = req.Observacao,
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync(ct);
        return await GetAsync(cobranca.Id, ct);
    }

    public async Task<CobrancaResponse> PagarAsync(Guid id, PagarCobrancaRequest req, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status != CobrancaStatus.Pendente)
            throw new AppException("Apenas cobranças pendentes podem ser pagas.", 400);
        if (!Enum.TryParse<FormaPagamento>(req.FormaPagamento, out var forma))
            throw new AppException($"FormaPagamento inválida: {req.FormaPagamento}.", 400);

        c.Status = CobrancaStatus.Pago;
        c.DataPagamento = req.DataPagamento;
        c.FormaPagamento = forma;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<CobrancaResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status == CobrancaStatus.Pago)
            throw new AppException("Cobranças pagas não podem ser canceladas.", 400);
        c.Status = CobrancaStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<WhatsappUrlResponse> GetWhatsappUrlAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Cobrancas
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);

        var fone = new string(c.Cliente!.Whatsapp.Where(char.IsDigit).ToArray());
        var msg = $"Olá {c.Cliente.Nome}, segue cobrança referente a {c.Referencia}: " +
                  $"R$ {c.Valor:N2} com vencimento em {c.DataVencimento:dd/MM/yyyy}. " +
                  "Em caso de dúvidas, entre em contato.";
        var url = $"https://wa.me/55{fone}?text={Uri.EscapeDataString(msg)}";
        return new WhatsappUrlResponse(url);
    }

    private async Task<Cobranca> FindAsync(Guid id, CancellationToken ct) =>
        await db.Cobrancas.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Cobrança não encontrada.", 404);

    private static CobrancaListItem ToListItem(Cobranca c)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var statusDisplay = c.Status == CobrancaStatus.Pendente && c.DataVencimento < hoje
            ? "Vencido"
            : c.Status.ToString();
        return new CobrancaListItem(
            c.Id, c.Cliente?.Nome ?? "", c.ContratoId,
            c.Contrato?.Titulo, c.Referencia, c.Valor,
            c.DataVencimento, statusDisplay);
    }

    private static CobrancaResponse ToResponse(Cobranca c)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var statusDisplay = c.Status == CobrancaStatus.Pendente && c.DataVencimento < hoje
            ? "Vencido"
            : c.Status.ToString();
        return new CobrancaResponse(
            c.Id, c.Cliente?.Nome ?? "", c.Cliente?.Whatsapp ?? "",
            c.ContratoId, c.Contrato?.Titulo,
            c.Referencia, c.Valor, c.DataVencimento,
            c.DataPagamento, statusDisplay,
            c.FormaPagamento?.ToString(), c.Observacao, c.CriadoEm);
    }
}
```

- [ ] **Step 4: Rodar testes — confirmar sucesso**

```bash
cd backend
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests --filter "CobrancaServiceTests"
```

Expected: 4 testes passando.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Services/Cobrancas/
git add backend/tests/GestorAI.Tests/Services/CobrancaServiceTests.cs
git commit -m "feat: implement CobrancaService with WhatsApp URL, payment and tests"
```

---

### Task 6: Endpoints + Program.cs

**Files:**
- Create: `backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs`
- Create: `backend/src/GestorAI.API/Endpoints/CobrancasEndpoints.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

- [ ] **Step 1: Criar ContratosEndpoints**

```csharp
// Endpoints/ContratosEndpoints.cs
using GestorAI.API.DTOs.Contratos;
using GestorAI.API.Services.Contratos;

namespace GestorAI.API.Endpoints;

public static class ContratosEndpoints
{
    public static void MapContratos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contratos").RequireAuthorization();

        group.MapGet("/", async (string? status, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, ct)));

        group.MapGet("/{id:guid}", async (Guid id, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateContratoRequest req, ContratoService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/contratos/{result.Id}", result);
        });

        group.MapPost("/{id:guid}/ativar", async (Guid id, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.AtivarAsync(id, ct)));

        group.MapPost("/{id:guid}/encerrar", async (Guid id, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.EncerrarAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (Guid id, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapGet("/{id:guid}/pdf", async (Guid id, ContratoService svc, CancellationToken ct) =>
        {
            var html = await svc.GetPdfHtmlAsync(id, ct);
            return Results.Content(html, "text/html");
        });

        group.MapPost("/{id:guid}/gerar-cobranças", async (
            Guid id, GerarCobrancasRequest req, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GerarCobrancasAsync(id, req, ct)));
    }
}
```

- [ ] **Step 2: Criar CobrancasEndpoints**

```csharp
// Endpoints/CobrancasEndpoints.cs
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Services.Cobrancas;

namespace GestorAI.API.Endpoints;

public static class CobrancasEndpoints
{
    public static void MapCobrancas(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cobrancas").RequireAuthorization();

        group.MapGet("/", async (
            string? status, Guid? clienteId, string? mes,
            CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, clienteId, mes, ct)));

        group.MapGet("/{id:guid}", async (Guid id, CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateCobrancaRequest req, CobrancaService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/cobrancas/{result.Id}", result);
        });

        group.MapPost("/{id:guid}/pagar", async (
            Guid id, PagarCobrancaRequest req, CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.PagarAsync(id, req, ct)));

        group.MapPost("/{id:guid}/cancelar", async (Guid id, CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapGet("/{id:guid}/whatsapp", async (Guid id, CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetWhatsappUrlAsync(id, ct)));
    }
}
```

- [ ] **Step 3: Registrar em Program.cs**

No bloco de Services (após `builder.Services.AddScoped<ConfiguracaoEmpresaService>();`), adicionar:
```csharp
// Services — Contratos e Cobranças
builder.Services.AddScoped<ContratoService>();
builder.Services.AddScoped<CobrancaService>();
```

Adicionar os usings no topo:
```csharp
using GestorAI.API.Services.Contratos;
using GestorAI.API.Services.Cobrancas;
```

Após `app.MapFiscal();`, adicionar:
```csharp
app.MapContratos();
app.MapCobrancas();
```

- [ ] **Step 4: Build e rodar todos os testes**

```bash
cd backend
/usr/local/share/dotnet/dotnet build src/GestorAI.API
/usr/local/share/dotnet/dotnet test tests/GestorAI.Tests
```

Expected: build sem erros, todos os testes passando.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Endpoints/ContratosEndpoints.cs
git add backend/src/GestorAI.API/Endpoints/CobrancasEndpoints.cs
git add backend/src/GestorAI.API/Program.cs
git commit -m "feat: add Contratos and Cobrancas endpoints and register services"
```

---

### Task 7: Frontend Types e Hooks

**Files:**
- Create: `frontend/src/types/contrato.ts`
- Create: `frontend/src/types/cobranca.ts`
- Create: `frontend/src/hooks/useContratos.ts`
- Create: `frontend/src/hooks/useCobrancas.ts`

- [ ] **Step 1: Criar tipos**

```typescript
// frontend/src/types/contrato.ts
export type ContratoStatus = 'Rascunho' | 'Ativo' | 'Encerrado' | 'Cancelado'
export type TipoCobranca = 'Recorrente' | 'ParceladoPrazoFixo'
export type Periodicidade = 'Mensal' | 'Trimestral' | 'Semestral' | 'Anual'

export interface ContratoItemResponse {
  id: string
  descricao: string
  quantidade: number
  valorUnitario: number
}

export interface ContratoResponse {
  id: string
  numero: number
  clienteNome: string
  clienteWhatsapp: string
  titulo: string
  objeto: string
  tipoCobranca: TipoCobranca
  valor: number
  dataInicio: string
  dataFim: string | null
  periodicidade: Periodicidade
  diaVencimento: number
  status: ContratoStatus
  observacao: string | null
  criadoEm: string
  itens: ContratoItemResponse[]
  total: number
}

export interface ContratoListItem {
  id: string
  numero: number
  clienteNome: string
  titulo: string
  tipoCobranca: TipoCobranca
  valor: number
  status: ContratoStatus
  dataInicio: string
  dataFim: string | null
}

export interface ContratoItemRequest {
  descricao: string
  quantidade: number
  valorUnitario: number
}

export interface CreateContratoRequest {
  clienteId: string
  titulo: string
  objeto: string
  tipoCobranca: string
  valor: number
  dataInicio: string
  dataFim?: string
  periodicidade: string
  diaVencimento: number
  observacao?: string
  itens: ContratoItemRequest[]
}

export interface GerarCobrancasRequest {
  de: string
  ate: string
}
```

```typescript
// frontend/src/types/cobranca.ts
export type CobrancaStatus = 'Pendente' | 'Pago' | 'Cancelado' | 'Vencido'

export interface CobrancaResponse {
  id: string
  clienteNome: string
  clienteWhatsapp: string
  contratoId: string | null
  contratoTitulo: string | null
  referencia: string
  valor: number
  dataVencimento: string
  dataPagamento: string | null
  status: CobrancaStatus
  formaPagamento: string | null
  observacao: string | null
  criadoEm: string
}

export interface CobrancaListItem {
  id: string
  clienteNome: string
  contratoId: string | null
  contratoTitulo: string | null
  referencia: string
  valor: number
  dataVencimento: string
  status: CobrancaStatus
}

export interface CreateCobrancaRequest {
  clienteId: string
  referencia: string
  valor: number
  dataVencimento: string
  observacao?: string
}

export interface PagarCobrancaRequest {
  dataPagamento: string
  formaPagamento: string
}
```

- [ ] **Step 2: Criar useContratos**

```typescript
// frontend/src/hooks/useContratos.ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  ContratoListItem, ContratoResponse,
  CreateContratoRequest, GerarCobrancasRequest,
} from '@/types/contrato'
import type { CobrancaListItem } from '@/types/cobranca'

export function useContratos() {
  const [contratos, setContratos] = useState<ContratoListItem[]>([])
  const [contrato, setContrato] = useState<ContratoResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (status?: string) => {
    setLoading(true)
    setError(null)
    try {
      const params = status ? `?status=${status}` : ''
      const items = await api.get<ContratoListItem[]>(`/api/contratos${params}`)
      setContratos(items)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar contratos')
    } finally { setLoading(false) }
  }, [])

  const get = useCallback(async (id: string) => {
    setLoading(true)
    setError(null)
    try {
      const data = await api.get<ContratoResponse>(`/api/contratos/${id}`)
      setContrato(data)
      return data
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar contrato')
      return null
    } finally { setLoading(false) }
  }, [])

  const create = useCallback(async (req: CreateContratoRequest) => {
    return api.post<ContratoResponse>('/api/contratos', req)
  }, [])

  const ativar = useCallback(async (id: string) => {
    const result = await api.post<ContratoResponse>(`/api/contratos/${id}/ativar`, {})
    setContrato(result)
    return result
  }, [])

  const encerrar = useCallback(async (id: string) => {
    const result = await api.post<ContratoResponse>(`/api/contratos/${id}/encerrar`, {})
    setContrato(result)
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<ContratoResponse>(`/api/contratos/${id}/cancelar`, {})
    setContrato(result)
    return result
  }, [])

  const gerarCobranças = useCallback(async (id: string, req: GerarCobrancasRequest) => {
    return api.post<CobrancaListItem[]>(`/api/contratos/${id}/gerar-cobranças`, req)
  }, [])

  const downloadPdf = useCallback(async (id: string) => {
    const win = window.open(`${import.meta.env.VITE_API_URL}/api/contratos/${id}/pdf`, '_blank')
    if (!win) alert('Permite popups para abrir o PDF.')
  }, [])

  return { contratos, contrato, loading, error, list, get, create, ativar, encerrar, cancelar, gerarCobranças, downloadPdf }
}
```

- [ ] **Step 3: Criar useCobrancas**

```typescript
// frontend/src/hooks/useCobrancas.ts
import { useState, useCallback } from 'react'
import { api } from '@/services/api'
import type {
  CobrancaListItem, CobrancaResponse,
  CreateCobrancaRequest, PagarCobrancaRequest,
} from '@/types/cobranca'

export function useCobrancas() {
  const [cobrancas, setCobrancas] = useState<CobrancaListItem[]>([])
  const [cobranca, setCobranca] = useState<CobrancaResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const list = useCallback(async (params?: { status?: string; clienteId?: string; mes?: string }) => {
    setLoading(true)
    setError(null)
    try {
      const q = new URLSearchParams()
      if (params?.status) q.set('status', params.status)
      if (params?.clienteId) q.set('clienteId', params.clienteId)
      if (params?.mes) q.set('mes', params.mes)
      const items = await api.get<CobrancaListItem[]>(`/api/cobrancas?${q}`)
      setCobrancas(items)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar cobranças')
    } finally { setLoading(false) }
  }, [])

  const get = useCallback(async (id: string) => {
    setLoading(true)
    setError(null)
    try {
      const data = await api.get<CobrancaResponse>(`/api/cobrancas/${id}`)
      setCobranca(data)
      return data
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao carregar cobrança')
      return null
    } finally { setLoading(false) }
  }, [])

  const create = useCallback(async (req: CreateCobrancaRequest) => {
    return api.post<CobrancaResponse>('/api/cobrancas', req)
  }, [])

  const pagar = useCallback(async (id: string, req: PagarCobrancaRequest) => {
    const result = await api.post<CobrancaResponse>(`/api/cobrancas/${id}/pagar`, req)
    setCobranca(result)
    return result
  }, [])

  const cancelar = useCallback(async (id: string) => {
    const result = await api.post<CobrancaResponse>(`/api/cobrancas/${id}/cancelar`, {})
    setCobranca(result)
    return result
  }, [])

  const abrirWhatsapp = useCallback(async (id: string) => {
    const { url } = await api.get<{ url: string }>(`/api/cobrancas/${id}/whatsapp`)
    window.open(url, '_blank')
  }, [])

  return { cobrancas, cobranca, loading, error, list, get, create, pagar, cancelar, abrirWhatsapp }
}
```

- [ ] **Step 4: Verificar TypeScript**

```bash
cd frontend
npx tsc --noEmit
```

Expected: sem erros de tipo.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/types/ frontend/src/hooks/useContratos.ts frontend/src/hooks/useCobrancas.ts
git commit -m "feat: add Contrato and Cobranca types and hooks"
```

---

### Task 8: Páginas de Contratos

**Files:**
- Create: `frontend/src/pages/contratos/Contratos.tsx`
- Create: `frontend/src/pages/contratos/NovoContrato.tsx`
- Create: `frontend/src/pages/contratos/DetalheContrato.tsx`

- [ ] **Step 1: Criar página de listagem Contratos.tsx**

```tsx
// frontend/src/pages/contratos/Contratos.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { FileText, Plus } from 'lucide-react'
import { useContratos } from '@/hooks/useContratos'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { ContratoStatus } from '@/types/contrato'

const STATUS_STYLES: Record<string, string> = {
  Rascunho:  'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-200',
  Ativo:     'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200',
  Encerrado: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
  Cancelado: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
}

const TIPO_LABEL: Record<string, string> = {
  Recorrente: 'Recorrente',
  ParceladoPrazoFixo: 'Parcelado',
}

export default function Contratos() {
  const navigate = useNavigate()
  const { contratos, loading, error, list } = useContratos()
  const [filtroStatus, setFiltroStatus] = useState('')

  useEffect(() => { void list(filtroStatus || undefined) }, [list, filtroStatus])

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T00:00:00').toLocaleDateString('pt-BR')

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <FileText className="h-5 w-5 text-primary" />
        <h1 className="text-xl font-bold">Contratos</h1>
        <div className="ml-auto flex items-center gap-2">
          <select
            value={filtroStatus}
            onChange={e => setFiltroStatus(e.target.value)}
            className="h-9 rounded-lg border border-input bg-transparent px-3 text-sm"
          >
            <option value="">Todos os status</option>
            {['Rascunho', 'Ativo', 'Encerrado', 'Cancelado'].map(s => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
          <Button size="sm" onClick={() => navigate('/contratos/novo')}>
            <Plus className="h-4 w-4 mr-1" /> Novo Contrato
          </Button>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}

      <div className="rounded-xl border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 border-b">
            <tr>
              {['Nº', 'Cliente', 'Título', 'Tipo', 'Valor', 'Status', 'Início'].map(h => (
                <th key={h} className="px-4 py-3 text-left font-medium text-muted-foreground">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading && (
              <tr><td colSpan={7} className="text-center py-8 text-muted-foreground">Carregando...</td></tr>
            )}
            {!loading && contratos.length === 0 && (
              <tr><td colSpan={7} className="text-center py-8 text-muted-foreground">Nenhum contrato encontrado</td></tr>
            )}
            {contratos.map(c => (
              <tr key={c.id}
                className="border-b last:border-0 hover:bg-muted/30 cursor-pointer transition-colors"
                onClick={() => navigate(`/contratos/${c.id}`)}>
                <td className="px-4 py-3 font-mono text-xs text-muted-foreground">{String(c.numero).padStart(3, '0')}</td>
                <td className="px-4 py-3 font-medium">{c.clienteNome}</td>
                <td className="px-4 py-3">{c.titulo}</td>
                <td className="px-4 py-3 text-muted-foreground">{TIPO_LABEL[c.tipoCobranca]}</td>
                <td className="px-4 py-3 font-medium">{fmtVal(c.valor)}</td>
                <td className="px-4 py-3">
                  <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', STATUS_STYLES[c.status])}>
                    {c.status}
                  </span>
                </td>
                <td className="px-4 py-3 text-muted-foreground">{fmtDate(c.dataInicio)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Criar NovoContrato.tsx**

```tsx
// frontend/src/pages/contratos/NovoContrato.tsx
import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft, Plus, Trash2 } from 'lucide-react'
import { useContratos } from '@/hooks/useContratos'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'
import { useEffect } from 'react'
import type { ContratoItemRequest } from '@/types/contrato'

export default function NovoContrato() {
  const navigate = useNavigate()
  const { create } = useContratos()
  const { clientes, list: listClientes } = useClientes()

  useEffect(() => { void listClientes() }, [listClientes])

  const [clienteId, setClienteId] = useState('')
  const [titulo, setTitulo] = useState('')
  const [objeto, setObjeto] = useState('')
  const [tipoCobranca, setTipoCobranca] = useState('Recorrente')
  const [valor, setValor] = useState('')
  const [dataInicio, setDataInicio] = useState('')
  const [dataFim, setDataFim] = useState('')
  const [periodicidade, setPeriodicidade] = useState('Mensal')
  const [diaVencimento, setDiaVencimento] = useState('10')
  const [observacao, setObservacao] = useState('')
  const [itens, setItens] = useState<ContratoItemRequest[]>([{ descricao: '', quantidade: 1, valorUnitario: 0 }])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const addItem = () => setItens(prev => [...prev, { descricao: '', quantidade: 1, valorUnitario: 0 }])
  const removeItem = (i: number) => setItens(prev => prev.filter((_, idx) => idx !== i))
  const updateItem = (i: number, field: keyof ContratoItemRequest, val: string | number) =>
    setItens(prev => prev.map((item, idx) => idx === i ? { ...item, [field]: val } : item))

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setSaving(true)
    try {
      const result = await create({
        clienteId, titulo, objeto, tipoCobranca,
        valor: Number(valor),
        dataInicio,
        dataFim: dataFim || undefined,
        periodicidade,
        diaVencimento: Number(diaVencimento),
        observacao: observacao || undefined,
        itens,
      })
      navigate(`/contratos/${result.id}`)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao criar contrato')
    } finally { setSaving(false) }
  }

  const labelClass = 'block text-sm font-medium mb-1'
  const inputClass = 'w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm'

  return (
    <div className="flex flex-col gap-6 max-w-2xl">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={() => navigate('/contratos')}>
          <ChevronLeft className="h-5 w-5" />
        </Button>
        <h1 className="text-xl font-bold">Novo Contrato</h1>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      )}

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div>
          <label className={labelClass}>Cliente *</label>
          <select value={clienteId} onChange={e => setClienteId(e.target.value)} required className={inputClass}>
            <option value="">Selecione...</option>
            {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>

        <div>
          <label className={labelClass}>Título *</label>
          <input value={titulo} onChange={e => setTitulo(e.target.value)} required className={inputClass} placeholder="Ex: Plano Mensal de Manutenção" />
        </div>

        <div>
          <label className={labelClass}>Objeto do Contrato *</label>
          <textarea value={objeto} onChange={e => setObjeto(e.target.value)} required rows={4} className={inputClass} placeholder="Descrição detalhada do que o contrato cobre..." />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className={labelClass}>Tipo de Cobrança</label>
            <select value={tipoCobranca} onChange={e => setTipoCobranca(e.target.value)} className={inputClass}>
              <option value="Recorrente">Recorrente</option>
              <option value="ParceladoPrazoFixo">Parcelado (Prazo Fixo)</option>
            </select>
          </div>
          <div>
            <label className={labelClass}>Periodicidade</label>
            <select value={periodicidade} onChange={e => setPeriodicidade(e.target.value)} className={inputClass}>
              {['Mensal', 'Trimestral', 'Semestral', 'Anual'].map(p => <option key={p} value={p}>{p}</option>)}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-3 gap-4">
          <div>
            <label className={labelClass}>Valor (R$) *</label>
            <input type="number" step="0.01" min="0" value={valor} onChange={e => setValor(e.target.value)} required className={inputClass} />
          </div>
          <div>
            <label className={labelClass}>Data Início *</label>
            <input type="date" value={dataInicio} onChange={e => setDataInicio(e.target.value)} required className={inputClass} />
          </div>
          <div>
            <label className={labelClass}>Data Fim {tipoCobranca === 'ParceladoPrazoFixo' ? '*' : ''}</label>
            <input type="date" value={dataFim} onChange={e => setDataFim(e.target.value)}
              required={tipoCobranca === 'ParceladoPrazoFixo'} className={inputClass} />
          </div>
        </div>

        <div className="w-32">
          <label className={labelClass}>Dia Vencimento (1-28) *</label>
          <input type="number" min={1} max={28} value={diaVencimento} onChange={e => setDiaVencimento(e.target.value)} required className={inputClass} />
        </div>

        <div>
          <div className="flex items-center justify-between mb-2">
            <label className={labelClass + ' mb-0'}>Itens</label>
            <Button type="button" variant="outline" size="sm" onClick={addItem}>
              <Plus className="h-3 w-3 mr-1" /> Adicionar
            </Button>
          </div>
          <div className="flex flex-col gap-2">
            {itens.map((item, i) => (
              <div key={i} className="flex gap-2 items-end">
                <div className="flex-1">
                  {i === 0 && <label className="text-xs text-muted-foreground mb-1 block">Descrição</label>}
                  <input value={item.descricao} onChange={e => updateItem(i, 'descricao', e.target.value)}
                    className={inputClass} placeholder="Descrição do item" />
                </div>
                <div className="w-20">
                  {i === 0 && <label className="text-xs text-muted-foreground mb-1 block">Qtd</label>}
                  <input type="number" min="0" step="0.01" value={item.quantidade}
                    onChange={e => updateItem(i, 'quantidade', Number(e.target.value))} className={inputClass} />
                </div>
                <div className="w-28">
                  {i === 0 && <label className="text-xs text-muted-foreground mb-1 block">Valor Unit.</label>}
                  <input type="number" min="0" step="0.01" value={item.valorUnitario}
                    onChange={e => updateItem(i, 'valorUnitario', Number(e.target.value))} className={inputClass} />
                </div>
                <Button type="button" variant="ghost" size="icon" className="mb-0.5 text-destructive" onClick={() => removeItem(i)}>
                  <Trash2 className="h-4 w-4" />
                </Button>
              </div>
            ))}
          </div>
        </div>

        <div>
          <label className={labelClass}>Observações</label>
          <textarea value={observacao} onChange={e => setObservacao(e.target.value)} rows={2} className={inputClass} />
        </div>

        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Criar Contrato'}</Button>
          <Button type="button" variant="outline" onClick={() => navigate('/contratos')}>Cancelar</Button>
        </div>
      </form>
    </div>
  )
}
```

- [ ] **Step 3: Criar DetalheContrato.tsx**

```tsx
// frontend/src/pages/contratos/DetalheContrato.tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ChevronLeft, FileDown, Zap } from 'lucide-react'
import { useContratos } from '@/hooks/useContratos'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const STATUS_STYLES: Record<string, string> = {
  Rascunho:  'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-200',
  Ativo:     'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200',
  Encerrado: 'bg-gray-100 text-gray-600 dark:bg-gray-800 dark:text-gray-400',
  Cancelado: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
}

export default function DetalheContrato() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { contrato, loading, error, get, ativar, encerrar, cancelar, gerarCobranças, downloadPdf } = useContratos()

  const [modalGerar, setModalGerar] = useState(false)
  const [de, setDe] = useState('')
  const [ate, setAte] = useState('')
  const [gerandoMsg, setGerandoMsg] = useState('')
  const [actionError, setActionError] = useState('')

  useEffect(() => { if (id) void get(id) }, [id, get])

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T00:00:00').toLocaleDateString('pt-BR')

  const handleAcao = async (acao: () => Promise<unknown>) => {
    setActionError('')
    try { await acao() } catch (e) { setActionError(e instanceof Error ? e.message : 'Erro') }
  }

  const handleGerarCobranças = async () => {
    if (!id || !de || !ate) return
    setGerandoMsg('')
    setActionError('')
    try {
      const result = await gerarCobranças(id, { de, ate })
      setGerandoMsg(result.length === 0
        ? 'Nenhuma cobrança nova (já existem para o período).'
        : `${result.length} cobrança(s) gerada(s) com sucesso.`)
      setModalGerar(false)
    } catch (e) { setActionError(e instanceof Error ? e.message : 'Erro ao gerar cobranças') }
  }

  if (loading && !contrato) return <div className="text-muted-foreground p-8">Carregando...</div>
  if (error) return <div className="text-destructive p-8">{error}</div>
  if (!contrato) return null

  const c = contrato

  return (
    <div className="flex flex-col gap-6 max-w-2xl">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={() => navigate('/contratos')}>
          <ChevronLeft className="h-5 w-5" />
        </Button>
        <div className="flex items-center gap-2">
          <h1 className="text-xl font-bold">Contrato {String(c.numero).padStart(3, '0')}</h1>
          <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', STATUS_STYLES[c.status])}>
            {c.status}
          </span>
        </div>
        <div className="ml-auto flex gap-2">
          <Button variant="outline" size="sm" onClick={() => downloadPdf(c.id)}>
            <FileDown className="h-4 w-4 mr-1" /> PDF
          </Button>
          {c.status === 'Ativo' && (
            <Button size="sm" onClick={() => setModalGerar(true)}>
              <Zap className="h-4 w-4 mr-1" /> Gerar Cobranças
            </Button>
          )}
        </div>
      </div>

      {actionError && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{actionError}</div>
      )}
      {gerandoMsg && (
        <div className="rounded-lg border border-green-300 bg-green-50 dark:bg-green-950/30 px-4 py-3 text-sm text-green-700 dark:text-green-300">{gerandoMsg}</div>
      )}

      <div className="rounded-xl border p-4 flex flex-col gap-3 text-sm">
        <div className="grid grid-cols-2 gap-x-4 gap-y-2">
          <div><span className="text-muted-foreground">Cliente:</span> <span className="font-medium">{c.clienteNome}</span></div>
          <div><span className="text-muted-foreground">Tipo:</span> {c.tipoCobranca === 'ParceladoPrazoFixo' ? 'Parcelado' : 'Recorrente'}</div>
          <div><span className="text-muted-foreground">Valor:</span> <span className="font-medium">{fmtVal(c.valor)}</span></div>
          <div><span className="text-muted-foreground">Periodicidade:</span> {c.periodicidade}</div>
          <div><span className="text-muted-foreground">Início:</span> {fmtDate(c.dataInicio)}</div>
          <div><span className="text-muted-foreground">Término:</span> {c.dataFim ? fmtDate(c.dataFim) : '—'}</div>
          <div><span className="text-muted-foreground">Vencimento:</span> dia {c.diaVencimento}</div>
        </div>

        {c.objeto && (
          <div>
            <div className="text-muted-foreground text-xs font-medium uppercase tracking-wide mb-1">Objeto</div>
            <div className="rounded bg-muted/40 px-3 py-2 whitespace-pre-wrap text-sm">{c.objeto}</div>
          </div>
        )}

        {c.itens.length > 0 && (
          <div>
            <div className="text-muted-foreground text-xs font-medium uppercase tracking-wide mb-2">Itens</div>
            <table className="w-full text-sm">
              <thead><tr className="border-b text-muted-foreground text-xs">
                <th className="text-left pb-1">Descrição</th>
                <th className="text-right pb-1">Qtd</th>
                <th className="text-right pb-1">Unit.</th>
                <th className="text-right pb-1">Total</th>
              </tr></thead>
              <tbody>
                {c.itens.map(item => (
                  <tr key={item.id} className="border-b last:border-0">
                    <td className="py-1">{item.descricao}</td>
                    <td className="py-1 text-right">{item.quantidade}</td>
                    <td className="py-1 text-right">{fmtVal(item.valorUnitario)}</td>
                    <td className="py-1 text-right font-medium">{fmtVal(item.quantidade * item.valorUnitario)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div className="text-right font-bold mt-2">Total: {fmtVal(c.total)}</div>
          </div>
        )}
      </div>

      {/* Ações de status */}
      <div className="flex gap-2">
        {c.status === 'Rascunho' && (
          <>
            <Button onClick={() => handleAcao(() => ativar(c.id))} className="bg-green-600 hover:bg-green-700">Ativar</Button>
            <Button variant="outline" onClick={() => navigate(`/contratos/${c.id}/editar`)}>Editar</Button>
            <Button variant="outline" className="text-destructive" onClick={() => handleAcao(() => cancelar(c.id))}>Cancelar</Button>
          </>
        )}
        {c.status === 'Ativo' && (
          <>
            <Button variant="outline" onClick={() => handleAcao(() => encerrar(c.id))}>Encerrar</Button>
            <Button variant="outline" className="text-destructive" onClick={() => handleAcao(() => cancelar(c.id))}>Cancelar</Button>
          </>
        )}
      </div>

      {/* Modal Gerar Cobranças */}
      {modalGerar && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center">
          <div className="bg-background rounded-xl border p-6 w-full max-w-sm flex flex-col gap-4">
            <h2 className="font-bold">Gerar Cobranças</h2>
            <div>
              <label className="block text-sm mb-1">De</label>
              <input type="date" value={de} onChange={e => setDe(e.target.value)}
                className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-sm mb-1">Até</label>
              <input type="date" value={ate} onChange={e => setAte(e.target.value)}
                className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm" />
            </div>
            <div className="flex gap-2">
              <Button onClick={handleGerarCobranças} disabled={!de || !ate}>Gerar</Button>
              <Button variant="outline" onClick={() => setModalGerar(false)}>Cancelar</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 4: Verificar compilação**

```bash
cd frontend
npx tsc --noEmit
```

Expected: sem erros.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/contratos/
git commit -m "feat: add Contratos list, form and detail pages"
```

---

### Task 9: Páginas de Cobranças

**Files:**
- Create: `frontend/src/pages/cobrancas/Cobrancas.tsx`
- Create: `frontend/src/pages/cobrancas/NovaCobranca.tsx`
- Create: `frontend/src/pages/cobrancas/DetalheCobranca.tsx`

- [ ] **Step 1: Criar Cobrancas.tsx**

```tsx
// frontend/src/pages/cobrancas/Cobrancas.tsx
import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { DollarSign, Plus } from 'lucide-react'
import { useCobrancas } from '@/hooks/useCobrancas'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const STATUS_STYLES: Record<string, string> = {
  Pendente:  'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-200',
  Pago:      'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200',
  Vencido:   'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-200',
  Cancelado: 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400',
}

export default function Cobrancas() {
  const navigate = useNavigate()
  const { cobrancas, loading, error, list } = useCobrancas()
  const [filtroStatus, setFiltroStatus] = useState('')
  const [filtroMes, setFiltroMes] = useState('')

  useEffect(() => {
    void list({
      status: filtroStatus || undefined,
      mes: filtroMes || undefined,
    })
  }, [list, filtroStatus, filtroMes])

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T00:00:00').toLocaleDateString('pt-BR')

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-3">
        <DollarSign className="h-5 w-5 text-primary" />
        <h1 className="text-xl font-bold">Cobranças</h1>
        <div className="ml-auto flex items-center gap-2">
          <input type="month" value={filtroMes} onChange={e => setFiltroMes(e.target.value)}
            className="h-9 rounded-lg border border-input bg-transparent px-3 text-sm" />
          <select value={filtroStatus} onChange={e => setFiltroStatus(e.target.value)}
            className="h-9 rounded-lg border border-input bg-transparent px-3 text-sm">
            <option value="">Todos</option>
            {['Pendente', 'Pago', 'Vencido', 'Cancelado'].map(s => <option key={s} value={s}>{s}</option>)}
          </select>
          <Button size="sm" onClick={() => navigate('/cobrancas/nova')}>
            <Plus className="h-4 w-4 mr-1" /> Nova Cobrança
          </Button>
        </div>
      </div>

      {error && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>
      )}

      <div className="rounded-xl border overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-muted/50 border-b">
            <tr>
              {['Referência', 'Cliente', 'Contrato', 'Valor', 'Vencimento', 'Status'].map(h => (
                <th key={h} className="px-4 py-3 text-left font-medium text-muted-foreground">{h}</th>
              ))}
            </tr>
          </thead>
          <tbody>
            {loading && <tr><td colSpan={6} className="text-center py-8 text-muted-foreground">Carregando...</td></tr>}
            {!loading && cobrancas.length === 0 && (
              <tr><td colSpan={6} className="text-center py-8 text-muted-foreground">Nenhuma cobrança encontrada</td></tr>
            )}
            {cobrancas.map(c => (
              <tr key={c.id}
                className="border-b last:border-0 hover:bg-muted/30 cursor-pointer transition-colors"
                onClick={() => navigate(`/cobrancas/${c.id}`)}>
                <td className="px-4 py-3 font-medium">{c.referencia}</td>
                <td className="px-4 py-3">{c.clienteNome}</td>
                <td className="px-4 py-3 text-muted-foreground text-xs">{c.contratoTitulo ?? '—'}</td>
                <td className="px-4 py-3 font-medium">{fmtVal(c.valor)}</td>
                <td className="px-4 py-3 text-muted-foreground">{fmtDate(c.dataVencimento)}</td>
                <td className="px-4 py-3">
                  <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', STATUS_STYLES[c.status])}>
                    {c.status}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Criar NovaCobranca.tsx**

```tsx
// frontend/src/pages/cobrancas/NovaCobranca.tsx
import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { ChevronLeft } from 'lucide-react'
import { useCobrancas } from '@/hooks/useCobrancas'
import { useClientes } from '@/hooks/useClientes'
import { Button } from '@/components/ui/button'

export default function NovaCobranca() {
  const navigate = useNavigate()
  const { create } = useCobrancas()
  const { clientes, list: listClientes } = useClientes()

  useEffect(() => { void listClientes() }, [listClientes])

  const [clienteId, setClienteId] = useState('')
  const [referencia, setReferencia] = useState('')
  const [valor, setValor] = useState('')
  const [dataVencimento, setDataVencimento] = useState('')
  const [observacao, setObservacao] = useState('')
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')
    setSaving(true)
    try {
      const result = await create({ clienteId, referencia, valor: Number(valor), dataVencimento, observacao: observacao || undefined })
      navigate(`/cobrancas/${result.id}`)
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Erro ao criar cobrança')
    } finally { setSaving(false) }
  }

  const inputClass = 'w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm'

  return (
    <div className="flex flex-col gap-6 max-w-md">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={() => navigate('/cobrancas')}><ChevronLeft className="h-5 w-5" /></Button>
        <h1 className="text-xl font-bold">Nova Cobrança</h1>
      </div>
      {error && <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{error}</div>}
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div>
          <label className="block text-sm font-medium mb-1">Cliente *</label>
          <select value={clienteId} onChange={e => setClienteId(e.target.value)} required className={inputClass}>
            <option value="">Selecione...</option>
            {clientes.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">Referência *</label>
          <input value={referencia} onChange={e => setReferencia(e.target.value)} required className={inputClass} placeholder="Ex: Mensalidade Jun/2026" />
        </div>
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1">Valor (R$) *</label>
            <input type="number" step="0.01" min="0" value={valor} onChange={e => setValor(e.target.value)} required className={inputClass} />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Vencimento *</label>
            <input type="date" value={dataVencimento} onChange={e => setDataVencimento(e.target.value)} required className={inputClass} />
          </div>
        </div>
        <div>
          <label className="block text-sm font-medium mb-1">Observações</label>
          <textarea value={observacao} onChange={e => setObservacao(e.target.value)} rows={2} className={inputClass} />
        </div>
        <div className="flex gap-2 pt-2">
          <Button type="submit" disabled={saving}>{saving ? 'Salvando...' : 'Criar Cobrança'}</Button>
          <Button type="button" variant="outline" onClick={() => navigate('/cobrancas')}>Cancelar</Button>
        </div>
      </form>
    </div>
  )
}
```

- [ ] **Step 3: Criar DetalheCobranca.tsx**

```tsx
// frontend/src/pages/cobrancas/DetalheCobranca.tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { ChevronLeft, MessageCircle } from 'lucide-react'
import { useCobrancas } from '@/hooks/useCobrancas'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

const STATUS_STYLES: Record<string, string> = {
  Pendente:  'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/40 dark:text-yellow-200',
  Pago:      'bg-green-100 text-green-800 dark:bg-green-900/40 dark:text-green-200',
  Vencido:   'bg-red-100 text-red-800 dark:bg-red-900/40 dark:text-red-200',
  Cancelado: 'bg-gray-100 text-gray-500 dark:bg-gray-800 dark:text-gray-400',
}

export default function DetalheCobranca() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { cobranca, loading, error, get, pagar, cancelar, abrirWhatsapp } = useCobrancas()

  const [modalPagar, setModalPagar] = useState(false)
  const [dataPagamento, setDataPagamento] = useState(new Date().toISOString().split('T')[0])
  const [formaPagamento, setFormaPagamento] = useState('Pix')
  const [actionError, setActionError] = useState('')

  useEffect(() => { if (id) void get(id) }, [id, get])

  const fmtVal = (v: number) => v.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
  const fmtDate = (s: string) => new Date(s + 'T00:00:00').toLocaleDateString('pt-BR')

  const handlePagar = async () => {
    if (!id) return
    setActionError('')
    try {
      await pagar(id, { dataPagamento: new Date(dataPagamento).toISOString(), formaPagamento })
      setModalPagar(false)
    } catch (e) { setActionError(e instanceof Error ? e.message : 'Erro') }
  }

  const handleCancelar = async () => {
    if (!id) return
    setActionError('')
    try { await cancelar(id) } catch (e) { setActionError(e instanceof Error ? e.message : 'Erro') }
  }

  if (loading && !cobranca) return <div className="text-muted-foreground p-8">Carregando...</div>
  if (error) return <div className="text-destructive p-8">{error}</div>
  if (!cobranca) return null

  const c = cobranca

  return (
    <div className="flex flex-col gap-6 max-w-lg">
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="icon" onClick={() => navigate('/cobrancas')}><ChevronLeft className="h-5 w-5" /></Button>
        <div className="flex items-center gap-2">
          <h1 className="text-xl font-bold">Cobrança</h1>
          <span className={cn('px-2 py-0.5 rounded-full text-xs font-medium', STATUS_STYLES[c.status])}>{c.status}</span>
        </div>
        {(c.status === 'Pendente' || c.status === 'Vencido') && (
          <Button variant="outline" size="sm" className="ml-auto" onClick={() => abrirWhatsapp(c.id)}>
            <MessageCircle className="h-4 w-4 mr-1" /> WhatsApp
          </Button>
        )}
      </div>

      {actionError && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">{actionError}</div>
      )}

      <div className="rounded-xl border p-4 flex flex-col gap-3 text-sm">
        <div className="grid grid-cols-2 gap-2">
          <div><span className="text-muted-foreground">Referência:</span> <span className="font-medium">{c.referencia}</span></div>
          <div><span className="text-muted-foreground">Valor:</span> <span className="font-bold text-base">{fmtVal(c.valor)}</span></div>
          <div><span className="text-muted-foreground">Cliente:</span> {c.clienteNome}</div>
          <div><span className="text-muted-foreground">Vencimento:</span> {fmtDate(c.dataVencimento)}</div>
          {c.contratoTitulo && <div className="col-span-2"><span className="text-muted-foreground">Contrato:</span> {c.contratoTitulo}</div>}
          {c.dataPagamento && (
            <>
              <div><span className="text-muted-foreground">Pago em:</span> {new Date(c.dataPagamento).toLocaleDateString('pt-BR')}</div>
              <div><span className="text-muted-foreground">Forma:</span> {c.formaPagamento}</div>
            </>
          )}
          {c.observacao && <div className="col-span-2"><span className="text-muted-foreground">Obs:</span> {c.observacao}</div>}
        </div>
      </div>

      {(c.status === 'Pendente' || c.status === 'Vencido') && (
        <div className="flex gap-2">
          <Button className="bg-green-600 hover:bg-green-700" onClick={() => setModalPagar(true)}>Registrar Pagamento</Button>
          <Button variant="outline" className="text-destructive" onClick={handleCancelar}>Cancelar Cobrança</Button>
        </div>
      )}

      {modalPagar && (
        <div className="fixed inset-0 bg-black/50 z-50 flex items-center justify-center">
          <div className="bg-background rounded-xl border p-6 w-full max-w-sm flex flex-col gap-4">
            <h2 className="font-bold">Registrar Pagamento</h2>
            <div>
              <label className="block text-sm mb-1">Data do Pagamento</label>
              <input type="date" value={dataPagamento} onChange={e => setDataPagamento(e.target.value)}
                className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-sm mb-1">Forma de Pagamento</label>
              <select value={formaPagamento} onChange={e => setFormaPagamento(e.target.value)}
                className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm">
                {['Pix', 'Dinheiro', 'Cartao', 'Outro'].map(f => <option key={f} value={f}>{f}</option>)}
              </select>
            </div>
            <div className="flex gap-2">
              <Button className="bg-green-600 hover:bg-green-700" onClick={handlePagar}>Confirmar</Button>
              <Button variant="outline" onClick={() => setModalPagar(false)}>Cancelar</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 4: Verificar compilação**

```bash
cd frontend
npx tsc --noEmit
```

Expected: sem erros.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/cobrancas/
git commit -m "feat: add Cobrancas list, form and detail pages"
```

---

### Task 10: Router + Sidebar

**Files:**
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/Sidebar.tsx`

- [ ] **Step 1: Adicionar rotas em router/index.tsx**

Adicionar imports após as linhas existentes de importação de páginas:
```typescript
import Contratos from '@/pages/contratos/Contratos'
import NovoContrato from '@/pages/contratos/NovoContrato'
import DetalheContrato from '@/pages/contratos/DetalheContrato'
import Cobrancas from '@/pages/cobrancas/Cobrancas'
import NovaCobranca from '@/pages/cobrancas/NovaCobranca'
import DetalheCobranca from '@/pages/cobrancas/DetalheCobranca'
```

Adicionar as rotas no array `children`, após `/agenda`:
```typescript
{ path: '/contratos', element: <Contratos /> },
{ path: '/contratos/novo', element: <NovoContrato /> },
{ path: '/contratos/:id', element: <DetalheContrato /> },
{ path: '/cobrancas', element: <Cobrancas /> },
{ path: '/cobrancas/nova', element: <NovaCobranca /> },
{ path: '/cobrancas/:id', element: <DetalheCobranca /> },
```

- [ ] **Step 2: Adicionar grupo no Sidebar.tsx**

Adicionar import `DollarSign` na lista de imports do lucide-react (já existente na linha 1-26).

Adicionar novo grupo no array `menuGroups`, após o grupo `'Agenda'`:
```typescript
{
  label: 'Contratos',
  items: [
    { icon: FileText,    label: 'Contratos',  path: '/contratos' },
    { icon: DollarSign,  label: 'Cobranças',  path: '/cobrancas' },
  ],
},
```

`FileText` já está importado no lucide-react existente. Adicionar `DollarSign` na lista de imports existente.

- [ ] **Step 3: Verificar compilação completa**

```bash
cd frontend
npx tsc --noEmit
```

Expected: sem erros.

- [ ] **Step 4: Reiniciar backend e testar fluxo completo**

```bash
# Matar processo existente se necessário
pkill -f GestorAI.API

# Iniciar backend
cd backend
/usr/local/share/dotnet/dotnet run --project src/GestorAI.API --launch-profile http &
```

Abrir `http://localhost:5174` no browser e verificar:
1. Grupo "Contratos" aparece no sidebar
2. Clicar em "Contratos" → lista vazia
3. Clicar em "Novo Contrato" → formulário abre
4. Criar contrato → redireciona para detalhe
5. Clicar "Ativar" → status muda para Ativo
6. Clicar "Gerar Cobranças" → modal aparece, informar período → cobranças criadas
7. Ir para "Cobranças" → listar cobranças geradas
8. Clicar em uma cobrança → detalhe abre
9. Clicar "Registrar Pagamento" → modal, confirmar → status Pago
10. Clicar "WhatsApp" → abre `wa.me` em nova aba com mensagem

- [ ] **Step 5: Commit final**

```bash
git add frontend/src/router/index.tsx
git add frontend/src/components/layout/Sidebar.tsx
git commit -m "feat: wire Contratos and Cobrancas into router and sidebar"
```
