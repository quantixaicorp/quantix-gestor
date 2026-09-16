# Conciliação Bancária — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implementar conciliação bancária no módulo financeiro com importação OFX/CSV, matching automático de lançamentos e geração automática de lançamentos para transações sem correspondência.

**Architecture:** Quatro novas entidades EF Core (BankAccount, BankStatement, BankStatementItem, BankReconciliation) com serviços dedicados para parsing e matching. Frontend com 3 novas pages (ContasBancarias, Conciliacao, ConciliacaoRevisao) e hook useConciliacao.

**Tech Stack:** ASP.NET Core 10 Minimal API, EF Core + PostgreSQL, xUnit + InMemory DB, React 19 + TypeScript + Tailwind + shadcn/ui.

## Global Constraints

- Schema do banco: `gestor`, snake_case via EFCore.NamingConventions
- Multi-tenancy por CompanyId com global query filter em AppDbContext
- JSON: camelCase + JsonStringEnumConverter (configurado globalmente em Program.cs)
- Testes backend: xUnit + EF InMemory, padrão `Setup()` que retorna `(AppDbContext db, XService svc)`
- Frontend: hooks usam `useCallback`, chamam `api.get/post/put/delete` de `@/services/api`
- Commit ao final de cada task

---

## File Map

### Backend — criar
- `Domain/Enums/BankStatementFormat.cs`
- `Domain/Enums/BankStatementItemStatus.cs`
- `Domain/Enums/BankMatchType.cs`
- `Domain/Enums/TransactionSource.cs`
- `Domain/Entities/BankAccount.cs`
- `Domain/Entities/BankStatement.cs`
- `Domain/Entities/BankStatementItem.cs`
- `Domain/Entities/BankReconciliation.cs`
- `DTOs/Conciliacao/BankAccountDto.cs`
- `DTOs/Conciliacao/BankStatementDto.cs`
- `Services/Conciliacao/BankStatementParserService.cs`
- `Services/Conciliacao/BankAccountService.cs`
- `Services/Conciliacao/BankReconciliationService.cs`
- `Endpoints/ConciliacaoEndpoints.cs`
- `tests/.../Services/BankReconciliationServiceTests.cs`
- `tests/.../Services/BankStatementParserServiceTests.cs`

### Backend — modificar
- `Domain/Entities/Transaction.cs` — adicionar `Source` field
- `Infrastructure/Data/AppDbContext.cs` — DbSets + query filters + relationships
- `Program.cs` — registrar serviços + mapear endpoints

### Frontend — criar
- `src/types/conciliacao.ts`
- `src/hooks/useConciliacao.ts`
- `src/pages/financeiro/ContasBancarias.tsx`
- `src/pages/financeiro/Conciliacao.tsx`
- `src/pages/financeiro/ConciliacaoRevisao.tsx`

### Frontend — modificar
- `src/services/api.ts` — adicionar `postForm` para upload multipart
- `src/router/index.tsx` — novas rotas
- `src/components/layout/TopNav.tsx` — itens de menu

---

## Task 1: Backend — Entidades, Enums e Migration

**Files:**
- Create: `backend/src/GestorAI.API/Domain/Enums/BankStatementFormat.cs`
- Create: `backend/src/GestorAI.API/Domain/Enums/BankStatementItemStatus.cs`
- Create: `backend/src/GestorAI.API/Domain/Enums/BankMatchType.cs`
- Create: `backend/src/GestorAI.API/Domain/Enums/TransactionSource.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/BankAccount.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/BankStatement.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/BankStatementItem.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/BankReconciliation.cs`
- Modify: `backend/src/GestorAI.API/Domain/Entities/Transaction.cs`
- Modify: `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`

**Interfaces:**
- Produces: entidades e enums usados por todos os tasks seguintes

- [ ] **Step 1: Criar enums**

```csharp
// Domain/Enums/BankStatementFormat.cs
namespace GestorAI.API.Domain.Enums;
public enum BankStatementFormat { OFX, CSV }

// Domain/Enums/BankStatementItemStatus.cs
namespace GestorAI.API.Domain.Enums;
public enum BankStatementItemStatus
{
    AutoConciliated,
    PendingReview,
    ManuallyIgnored,
    Unmatched
}

// Domain/Enums/BankMatchType.cs
namespace GestorAI.API.Domain.Enums;
public enum BankMatchType { Auto, Manual }

// Domain/Enums/TransactionSource.cs
namespace GestorAI.API.Domain.Enums;
public enum TransactionSource { Manual, Sale, BankImport }
```

- [ ] **Step 2: Criar entidades**

```csharp
// Domain/Entities/BankAccount.cs
using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;
public class BankAccount : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string BankName { get; set; }
    public string? AccountNumber { get; set; }
    public string? Agency { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<BankStatement> Statements { get; set; } = [];
}
```

```csharp
// Domain/Entities/BankStatement.cs
using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;
public class BankStatement : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BankAccountId { get; set; }
    public required string FileName { get; set; }
    public BankStatementFormat Format { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
    public int ItemCount { get; set; }
    public BankAccount? BankAccount { get; set; }
    public ICollection<BankStatementItem> Items { get; set; } = [];
}
```

```csharp
// Domain/Entities/BankStatementItem.cs
using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;
public class BankStatementItem
{
    public Guid Id { get; set; }
    public Guid BankStatementId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public string? BankTransactionId { get; set; }
    public BankStatementItemStatus Status { get; set; } = BankStatementItemStatus.Unmatched;
    public BankStatement? BankStatement { get; set; }
    public BankReconciliation? Reconciliation { get; set; }
}
```

```csharp
// Domain/Entities/BankReconciliation.cs
using GestorAI.API.Domain.Enums;
namespace GestorAI.API.Domain.Entities;
public class BankReconciliation
{
    public Guid Id { get; set; }
    public Guid BankStatementItemId { get; set; }
    public Guid TransactionId { get; set; }
    public int ConfidenceScore { get; set; }
    public BankMatchType MatchType { get; set; }
    public bool CreatedByImport { get; set; }
    public DateTime ReconciledAt { get; set; } = DateTime.UtcNow;
    public BankStatementItem? BankStatementItem { get; set; }
    public Transaction? Transaction { get; set; }
}
```

- [ ] **Step 3: Adicionar `Source` na entidade Transaction**

No arquivo `Domain/Entities/Transaction.cs`, adicionar após `public string? Notes { get; set; }`:

```csharp
public TransactionSource Source { get; set; } = TransactionSource.Manual;
```

E adicionar o using no topo:
```csharp
using GestorAI.API.Domain.Enums;
```

- [ ] **Step 4: Atualizar AppDbContext**

Adicionar DbSets após `public DbSet<InstallmentPlan> InstallmentPlans => Set<InstallmentPlan>();`:

```csharp
public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
public DbSet<BankStatement> BankStatements => Set<BankStatement>();
public DbSet<BankStatementItem> BankStatementItems => Set<BankStatementItem>();
public DbSet<BankReconciliation> BankReconciliations => Set<BankReconciliation>();
```

No `OnModelCreating`, adicionar antes do `}` final:

```csharp
// Bank Reconciliation
modelBuilder.Entity<BankAccount>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
modelBuilder.Entity<BankStatement>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);

modelBuilder.Entity<BankStatement>()
    .HasOne(s => s.BankAccount)
    .WithMany(a => a.Statements)
    .HasForeignKey(s => s.BankAccountId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<BankStatementItem>()
    .HasOne(i => i.BankStatement)
    .WithMany(s => s.Items)
    .HasForeignKey(i => i.BankStatementId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<BankStatementItem>()
    .HasOne(i => i.Reconciliation)
    .WithOne(r => r.BankStatementItem)
    .HasForeignKey<BankReconciliation>(r => r.BankStatementItemId)
    .OnDelete(DeleteBehavior.Cascade);

modelBuilder.Entity<BankReconciliation>()
    .HasOne(r => r.Transaction)
    .WithMany()
    .HasForeignKey(r => r.TransactionId)
    .OnDelete(DeleteBehavior.Restrict);
```

- [ ] **Step 5: Gerar migration**

```bash
cd backend/src/GestorAI.API
dotnet ef migrations add AddBankReconciliation
```

Expected: arquivo `Infrastructure/Data/Migrations/YYYYMMDD_AddBankReconciliation.cs` criado sem erros.

- [ ] **Step 6: Verificar migration e aplicar**

```bash
dotnet ef database update
```

Expected: `Done.` sem erros.

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/Domain/ backend/src/GestorAI.API/Infrastructure/
git commit -m "feat: add BankReconciliation entities, enums and migration

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 2: Backend — Parser OFX e CSV

**Files:**
- Create: `backend/src/GestorAI.API/Services/Conciliacao/BankStatementParserService.cs`
- Create: `backend/tests/GestorAI.Tests/Services/BankStatementParserServiceTests.cs`

**Interfaces:**
- Produces: `BankStatementParserService.ParseOfxAsync(Stream) → List<ParsedTransaction>` e `ParseCsvAsync(Stream) → List<ParsedTransaction>`
- Produces: record `ParsedTransaction(DateOnly Date, decimal Amount, string Description, string? BankTransactionId)`

- [ ] **Step 1: Escrever testes falhando**

```csharp
// tests/GestorAI.Tests/Services/BankStatementParserServiceTests.cs
using GestorAI.API.Services.Conciliacao;
using System.Text;

namespace GestorAI.Tests.Services;

public class BankStatementParserServiceTests
{
    private readonly BankStatementParserService _svc = new();

    [Fact]
    public async Task ParseOfxAsync_ExtractsTrnAmtAndMemo()
    {
        var ofx = """
            OFXHEADER:100
            DATA:OFXSGML
            <OFX>
            <STMTTRN>
            <TRNTYPE>DEBIT
            <DTPOSTED>20250801000000[-03:BRT]
            <TRNAMT>-250.00
            <FITID>ABC123
            <MEMO>PIX ENVIADO JOAO
            </STMTTRN>
            <STMTTRN>
            <TRNTYPE>CREDIT
            <DTPOSTED>20250802120000
            <TRNAMT>1500.00
            <FITID>DEF456
            <MEMO>TED RECEBIDA
            </STMTTRN>
            </OFX>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));

        var result = await _svc.ParseOfxAsync(stream);

        Assert.Equal(2, result.Count);
        Assert.Equal(-250m, result[0].Amount);
        Assert.Equal("PIX ENVIADO JOAO", result[0].Description);
        Assert.Equal("ABC123", result[0].BankTransactionId);
        Assert.Equal(new DateOnly(2025, 8, 1), result[0].Date);
        Assert.Equal(1500m, result[1].Amount);
    }

    [Fact]
    public async Task ParseCsvAsync_ExtractsColumnsCorrectly()
    {
        var csv = """
            Data,Descrição,Valor
            2025-08-01,PIX RECEBIDO JOAO,250.00
            01/08/2025,PAGTO FORNECEDOR,-1500.00
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _svc.ParseCsvAsync(stream);

        Assert.Equal(2, result.Count);
        Assert.Equal(250m, result[0].Amount);
        Assert.Equal("PIX RECEBIDO JOAO", result[0].Description);
        Assert.Equal(new DateOnly(2025, 8, 1), result[0].Date);
        Assert.Equal(-1500m, result[1].Amount);
    }

    [Fact]
    public async Task ParseCsvAsync_SkipsHeaderAndEmptyLines()
    {
        var csv = "Data,Descrição,Valor\n2025-08-01,Teste,100.00\n\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _svc.ParseCsvAsync(stream);

        Assert.Single(result);
    }
}
```

- [ ] **Step 2: Rodar testes para verificar que falham**

```bash
cd backend
dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "BankStatementParserServiceTests"
```

Expected: FAIL — `BankStatementParserService` não encontrado.

- [ ] **Step 3: Implementar BankStatementParserService**

```csharp
// Services/Conciliacao/BankStatementParserService.cs
using System.Text.RegularExpressions;

namespace GestorAI.API.Services.Conciliacao;

public record ParsedTransaction(
    DateOnly Date,
    decimal Amount,
    string Description,
    string? BankTransactionId);

public class BankStatementParserService
{
    public async Task<List<ParsedTransaction>> ParseOfxAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        var results = new List<ParsedTransaction>();

        var trnBlocks = Regex.Matches(content,
            @"<STMTTRN>(.*?)</STMTTRN>",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        foreach (Match block in trnBlocks)
        {
            var text = block.Groups[1].Value;
            var date = ExtractOfxDate(GetTag(text, "DTPOSTED"));
            var amountStr = GetTag(text, "TRNAMT");
            var memo = GetTag(text, "MEMO") ?? GetTag(text, "NAME") ?? "";
            var fitid = GetTag(text, "FITID");

            if (date is null || !decimal.TryParse(amountStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var amount))
                continue;

            results.Add(new ParsedTransaction(date.Value, amount, memo.Trim(), fitid));
        }

        return results;
    }

    public async Task<List<ParsedTransaction>> ParseCsvAsync(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var results = new List<ParsedTransaction>();
        string? line;
        var isHeader = true;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (isHeader) { isHeader = false; continue; }

            var parts = line.Split(',');
            if (parts.Length < 3) continue;

            var dateStr = parts[0].Trim();
            var desc = parts[1].Trim();
            var amountStr = parts[2].Trim();

            if (!TryParseDate(dateStr, out var date)) continue;
            if (!decimal.TryParse(amountStr,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var amount)) continue;

            results.Add(new ParsedTransaction(date, amount, desc, null));
        }

        return results;
    }

    private static string? GetTag(string text, string tag)
    {
        var m = Regex.Match(text, $@"<{tag}>\s*([^\r\n<]+)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static DateOnly? ExtractOfxDate(string? raw)
    {
        if (raw is null) return null;
        var digits = Regex.Match(raw, @"(\d{8})");
        if (!digits.Success) return null;
        var s = digits.Groups[1].Value;
        if (!int.TryParse(s[..4], out var y) ||
            !int.TryParse(s[4..6], out var mo) ||
            !int.TryParse(s[6..8], out var d)) return null;
        return new DateOnly(y, mo, d);
    }

    private static bool TryParseDate(string s, out DateOnly result)
    {
        if (DateOnly.TryParseExact(s, "yyyy-MM-dd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out result)) return true;
        if (DateOnly.TryParseExact(s, "dd/MM/yyyy",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out result)) return true;
        return false;
    }
}
```

- [ ] **Step 4: Rodar testes**

```bash
dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "BankStatementParserServiceTests"
```

Expected: 3 testes PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Services/Conciliacao/ backend/tests/
git commit -m "feat: add OFX and CSV parser service with tests

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 3: Backend — DTOs, BankAccountService e BankReconciliationService

**Files:**
- Create: `backend/src/GestorAI.API/DTOs/Conciliacao/BankAccountDto.cs`
- Create: `backend/src/GestorAI.API/DTOs/Conciliacao/BankStatementDto.cs`
- Create: `backend/src/GestorAI.API/Services/Conciliacao/BankAccountService.cs`
- Create: `backend/src/GestorAI.API/Services/Conciliacao/BankReconciliationService.cs`
- Create: `backend/tests/GestorAI.Tests/Services/BankReconciliationServiceTests.cs`

**Interfaces:**
- Consumes: entidades do Task 1, ParsedTransaction do Task 2
- Produces: `BankAccountService`, `BankReconciliationService` consumidos pelo Task 4 (endpoints)

- [ ] **Step 1: Criar DTOs**

```csharp
// DTOs/Conciliacao/BankAccountDto.cs
namespace GestorAI.API.DTOs.Conciliacao;

public record CreateBankAccountRequest(
    string Name,
    string BankName,
    string? AccountNumber,
    string? Agency);

public record BankAccountResponse(
    Guid Id,
    string Name,
    string BankName,
    string? AccountNumber,
    string? Agency,
    bool IsActive);
```

```csharp
// DTOs/Conciliacao/BankStatementDto.cs
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Conciliacao;

public record BankStatementListItem(
    Guid Id,
    Guid BankAccountId,
    string BankAccountName,
    string FileName,
    string Format,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    DateTime ImportedAt,
    int ItemCount,
    int AutoConciliated,
    int PendingReview,
    int Unmatched,
    int ManuallyIgnored);

public record BankStatementItemResponse(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    string Description,
    string? BankTransactionId,
    string Status,
    BankReconciliationResponse? Reconciliation);

public record BankReconciliationResponse(
    Guid Id,
    Guid TransactionId,
    string TransactionDescription,
    decimal TransactionAmount,
    DateTime TransactionDueDate,
    int ConfidenceScore,
    string MatchType,
    bool CreatedByImport);

public record ManualMatchRequest(Guid ItemId, Guid TransactionId);
```

- [ ] **Step 2: Criar BankAccountService**

```csharp
// Services/Conciliacao/BankAccountService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Conciliacao;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Conciliacao;

public class BankAccountService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<BankAccountResponse>> ListAsync(CancellationToken ct) =>
        await db.BankAccounts
            .Where(a => a.IsActive)
            .OrderBy(a => a.Name)
            .Select(a => ToResponse(a))
            .ToListAsync(ct);

    public async Task<BankAccountResponse> CreateAsync(CreateBankAccountRequest req, CancellationToken ct)
    {
        var account = new BankAccount
        {
            CompanyId = tenantContext.CompanyId,
            Name = req.Name,
            BankName = req.BankName,
            AccountNumber = req.AccountNumber,
            Agency = req.Agency,
        };
        db.BankAccounts.Add(account);
        await db.SaveChangesAsync(ct);
        return ToResponse(account);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var account = await db.BankAccounts.FindAsync([id], ct)
            ?? throw new AppException("Conta bancária não encontrada.", 404);
        account.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private static BankAccountResponse ToResponse(BankAccount a) =>
        new(a.Id, a.Name, a.BankName, a.AccountNumber, a.Agency, a.IsActive);
}
```

- [ ] **Step 3: Escrever testes para o algoritmo de matching (falharão)**

```csharp
// tests/GestorAI.Tests/Services/BankReconciliationServiceTests.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Conciliacao;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Services;

public class BankReconciliationServiceTests
{
    private readonly Guid _companyId = Guid.NewGuid();

    private (AppDbContext db, BankReconciliationService svc, BankAccount account) Setup()
    {
        var tenant = new TenantContext { CompanyId = _companyId };
        var db = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,
            tenant);

        var account = new BankAccount
        {
            CompanyId = _companyId,
            Name = "Conta Teste",
            BankName = "Banco Teste"
        };
        db.BankAccounts.Add(account);
        db.SaveChanges();

        return (db, new BankReconciliationService(db, tenant, new BankStatementParserService()), account);
    }

    private Transaction AddTransaction(AppDbContext db, decimal amount, DateTime dueDate,
        TipoLancamento type = TipoLancamento.Receita)
    {
        var t = new Transaction
        {
            CompanyId = _companyId,
            Type = type,
            Description = "Lançamento teste",
            Amount = amount,
            DueDate = dueDate,
            Status = StatusLancamento.Pendente,
            Category = "Teste",
        };
        db.Transactions.Add(t);
        db.SaveChanges();
        return t;
    }

    [Fact]
    public async Task ImportAsync_ExactMatch_SetAutoConciliated()
    {
        var (db, svc, account) = Setup();
        var txDate = new DateTime(2025, 8, 1);
        AddTransaction(db, 250m, txDate, TipoLancamento.Receita);

        var parsed = new List<ParsedTransaction>
        {
            new(new DateOnly(2025, 8, 1), 250m, "PIX RECEBIDO", "FITID001")
        };

        var stmt = await svc.ImportAsync(account.Id, "extrato.ofx",
            Domain.Enums.BankStatementFormat.OFX, parsed, default);

        var item = db.BankStatementItems.Include(i => i.Reconciliation).First();
        Assert.Equal(BankStatementItemStatus.AutoConciliated, item.Status);
        Assert.NotNull(item.Reconciliation);
        Assert.Equal(100, item.Reconciliation!.ConfidenceScore);
    }

    [Fact]
    public async Task ImportAsync_DateWithin7Days_SetPendingReview()
    {
        var (db, svc, account) = Setup();
        var txDate = new DateTime(2025, 8, 5);
        AddTransaction(db, 250m, txDate, TipoLancamento.Receita);

        var parsed = new List<ParsedTransaction>
        {
            new(new DateOnly(2025, 8, 1), 250m, "PIX RECEBIDO", null)
        };

        await svc.ImportAsync(account.Id, "extrato.ofx",
            Domain.Enums.BankStatementFormat.OFX, parsed, default);

        var item = db.BankStatementItems.Include(i => i.Reconciliation).First();
        Assert.Equal(BankStatementItemStatus.PendingReview, item.Status);
        Assert.NotNull(item.Reconciliation);
        Assert.True(item.Reconciliation!.ConfidenceScore < 100);
    }

    [Fact]
    public async Task ImportAsync_NoMatch_CreatesNewTransaction()
    {
        var (db, svc, account) = Setup();

        var parsed = new List<ParsedTransaction>
        {
            new(new DateOnly(2025, 8, 1), 300m, "TAXA BANCARIA", null)
        };

        await svc.ImportAsync(account.Id, "extrato.ofx",
            Domain.Enums.BankStatementFormat.OFX, parsed, default);

        var item = db.BankStatementItems.First();
        Assert.Equal(BankStatementItemStatus.Unmatched, item.Status);
        var created = db.Transactions.FirstOrDefault(t => t.Source == TransactionSource.BankImport);
        Assert.NotNull(created);
        Assert.Equal(300m, created!.Amount);
    }

    [Fact]
    public async Task IgnoreAsync_SetsManuallyIgnored()
    {
        var (db, svc, account) = Setup();
        var parsed = new List<ParsedTransaction>
        {
            new(new DateOnly(2025, 8, 1), 10m, "IOF", null)
        };
        await svc.ImportAsync(account.Id, "extrato.ofx",
            Domain.Enums.BankStatementFormat.OFX, parsed, default);

        var item = db.BankStatementItems.First();
        // delete auto-created transaction so we can ignore
        var tx = db.Transactions.FirstOrDefault(t => t.Source == TransactionSource.BankImport);
        if (tx != null) db.Transactions.Remove(tx);
        db.SaveChanges();

        await svc.IgnoreAsync(item.Id, default);

        var updated = db.BankStatementItems.Find(item.Id)!;
        Assert.Equal(BankStatementItemStatus.ManuallyIgnored, updated.Status);
    }
}
```

- [ ] **Step 4: Rodar testes para verificar que falham**

```bash
cd backend
dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "BankReconciliationServiceTests"
```

Expected: FAIL — `BankReconciliationService` não encontrado.

- [ ] **Step 5: Implementar BankReconciliationService**

```csharp
// Services/Conciliacao/BankReconciliationService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Conciliacao;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Conciliacao;

public class BankReconciliationService(
    AppDbContext db,
    TenantContext tenantContext,
    BankStatementParserService parser)
{
    public async Task<BankStatementListItem> ImportAsync(
        Guid bankAccountId,
        string fileName,
        BankStatementFormat format,
        List<ParsedTransaction> transactions,
        CancellationToken ct)
    {
        var account = await db.BankAccounts.FindAsync([bankAccountId], ct)
            ?? throw new AppException("Conta bancária não encontrada.", 404);

        var statement = new BankStatement
        {
            CompanyId = tenantContext.CompanyId,
            BankAccountId = bankAccountId,
            FileName = fileName,
            Format = format,
            PeriodStart = transactions.Count > 0 ? transactions.Min(t => t.Date) : DateOnly.FromDateTime(DateTime.UtcNow),
            PeriodEnd = transactions.Count > 0 ? transactions.Max(t => t.Date) : DateOnly.FromDateTime(DateTime.UtcNow),
            ItemCount = transactions.Count,
        };
        db.BankStatements.Add(statement);
        await db.SaveChangesAsync(ct);

        // load all company transactions for matching
        var allTx = await db.Transactions
            .Where(t => t.Status != StatusLancamento.Cancelado)
            .ToListAsync(ct);

        var usedTxIds = new HashSet<Guid>();

        foreach (var parsed in transactions)
        {
            var item = new BankStatementItem
            {
                BankStatementId = statement.Id,
                Date = parsed.Date,
                Amount = parsed.Amount,
                Description = parsed.Description,
                BankTransactionId = parsed.BankTransactionId,
            };
            db.BankStatementItems.Add(item);

            var expectedType = parsed.Amount > 0 ? TipoLancamento.Receita : TipoLancamento.Despesa;
            var absAmount = Math.Abs(parsed.Amount);
            var parsedDateTime = parsed.Date.ToDateTime(TimeOnly.MinValue);

            // try exact match first
            var exactMatch = allTx.FirstOrDefault(t =>
                !usedTxIds.Contains(t.Id) &&
                t.Type == expectedType &&
                t.Amount == absAmount &&
                Math.Abs((t.DueDate.Date - parsedDateTime).TotalDays) <= 3);

            if (exactMatch != null)
            {
                usedTxIds.Add(exactMatch.Id);
                item.Status = BankStatementItemStatus.AutoConciliated;
                db.BankReconciliations.Add(new BankReconciliation
                {
                    BankStatementItemId = item.Id,
                    TransactionId = exactMatch.Id,
                    ConfidenceScore = 100,
                    MatchType = BankMatchType.Auto,
                    CreatedByImport = false,
                });
                continue;
            }

            // try fuzzy match (within 1% amount + 7 days)
            var fuzzyMatch = allTx.FirstOrDefault(t =>
                !usedTxIds.Contains(t.Id) &&
                t.Type == expectedType &&
                Math.Abs(t.Amount - absAmount) / Math.Max(absAmount, 0.01m) <= 0.01m &&
                Math.Abs((t.DueDate.Date - parsedDateTime).TotalDays) <= 7);

            if (fuzzyMatch != null)
            {
                usedTxIds.Add(fuzzyMatch.Id);
                var daysDiff = Math.Abs((fuzzyMatch.DueDate.Date - parsedDateTime).TotalDays);
                var score = daysDiff <= 3 ? 85 : 70;
                item.Status = BankStatementItemStatus.PendingReview;
                db.BankReconciliations.Add(new BankReconciliation
                {
                    BankStatementItemId = item.Id,
                    TransactionId = fuzzyMatch.Id,
                    ConfidenceScore = score,
                    MatchType = BankMatchType.Auto,
                    CreatedByImport = false,
                });
                continue;
            }

            // exact amount, date > 7 days
            var farMatch = allTx.FirstOrDefault(t =>
                !usedTxIds.Contains(t.Id) &&
                t.Type == expectedType &&
                t.Amount == absAmount);

            if (farMatch != null)
            {
                usedTxIds.Add(farMatch.Id);
                item.Status = BankStatementItemStatus.PendingReview;
                db.BankReconciliations.Add(new BankReconciliation
                {
                    BankStatementItemId = item.Id,
                    TransactionId = farMatch.Id,
                    ConfidenceScore = 50,
                    MatchType = BankMatchType.Auto,
                    CreatedByImport = false,
                });
                continue;
            }

            // no match — create transaction automatically
            item.Status = BankStatementItemStatus.Unmatched;
            var newTx = new Transaction
            {
                CompanyId = tenantContext.CompanyId,
                Type = expectedType,
                Description = parsed.Description,
                Amount = absAmount,
                DueDate = parsedDateTime,
                PaymentDate = parsedDateTime,
                Status = StatusLancamento.Pago,
                Category = "Importado",
                Notes = "Gerado automaticamente via import de extrato bancário",
                Source = TransactionSource.BankImport,
            };
            db.Transactions.Add(newTx);
            db.BankReconciliations.Add(new BankReconciliation
            {
                BankStatementItemId = item.Id,
                TransactionId = newTx.Id,
                ConfidenceScore = 0,
                MatchType = BankMatchType.Auto,
                CreatedByImport = true,
            });
        }

        await db.SaveChangesAsync(ct);
        return await GetStatementListItemAsync(statement.Id, ct);
    }

    public async Task<List<BankStatementListItem>> ListStatementsAsync(
        Guid? bankAccountId, CancellationToken ct)
    {
        var query = db.BankStatements
            .Include(s => s.BankAccount)
            .Include(s => s.Items)
                .ThenInclude(i => i.Reconciliation)
            .AsQueryable();

        if (bankAccountId.HasValue)
            query = query.Where(s => s.BankAccountId == bankAccountId.Value);

        var statements = await query.OrderByDescending(s => s.ImportedAt).ToListAsync(ct);
        return statements.Select(ToListItem).ToList();
    }

    public async Task<List<BankStatementItemResponse>> GetItemsAsync(
        Guid statementId, CancellationToken ct)
    {
        var items = await db.BankStatementItems
            .Where(i => i.BankStatementId == statementId)
            .Include(i => i.Reconciliation)
                .ThenInclude(r => r!.Transaction)
            .OrderBy(i => i.Date)
            .ToListAsync(ct);

        return items.Select(ToItemResponse).ToList();
    }

    public async Task DeleteStatementAsync(Guid statementId, CancellationToken ct)
    {
        var stmt = await db.BankStatements
            .Include(s => s.Items)
                .ThenInclude(i => i.Reconciliation)
            .FirstOrDefaultAsync(s => s.Id == statementId, ct)
            ?? throw new AppException("Extrato não encontrado.", 404);

        // remove auto-created transactions
        var autoTxIds = stmt.Items
            .Where(i => i.Reconciliation?.CreatedByImport == true)
            .Select(i => i.Reconciliation!.TransactionId)
            .ToList();

        var txToRemove = await db.Transactions
            .Where(t => autoTxIds.Contains(t.Id))
            .ToListAsync(ct);
        db.Transactions.RemoveRange(txToRemove);
        db.BankStatements.Remove(stmt);
        await db.SaveChangesAsync(ct);
    }

    public async Task ManualMatchAsync(ManualMatchRequest req, CancellationToken ct)
    {
        var item = await db.BankStatementItems
            .Include(i => i.Reconciliation)
            .FirstOrDefaultAsync(i => i.Id == req.ItemId, ct)
            ?? throw new AppException("Item não encontrado.", 404);

        var tx = await db.Transactions.FindAsync([req.TransactionId], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (item.Reconciliation != null)
        {
            // remove previous tentative reconciliation if CreatedByImport
            if (item.Reconciliation.CreatedByImport)
            {
                var oldTx = await db.Transactions.FindAsync([item.Reconciliation.TransactionId], ct);
                if (oldTx != null) db.Transactions.Remove(oldTx);
            }
            db.BankReconciliations.Remove(item.Reconciliation);
        }

        item.Status = BankStatementItemStatus.AutoConciliated;
        db.BankReconciliations.Add(new BankReconciliation
        {
            BankStatementItemId = item.Id,
            TransactionId = tx.Id,
            ConfidenceScore = 100,
            MatchType = BankMatchType.Manual,
            CreatedByImport = false,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task UndoMatchAsync(Guid reconciliationId, CancellationToken ct)
    {
        var recon = await db.BankReconciliations
            .Include(r => r.BankStatementItem)
            .FirstOrDefaultAsync(r => r.Id == reconciliationId, ct)
            ?? throw new AppException("Conciliação não encontrada.", 404);

        if (recon.CreatedByImport)
        {
            // delete the auto-generated transaction and return item to Unmatched
            var tx = await db.Transactions.FindAsync([recon.TransactionId], ct);
            if (tx != null) db.Transactions.Remove(tx);
            recon.BankStatementItem!.Status = BankStatementItemStatus.Unmatched;
        }
        else
        {
            recon.BankStatementItem!.Status = BankStatementItemStatus.PendingReview;
        }

        db.BankReconciliations.Remove(recon);
        await db.SaveChangesAsync(ct);
    }

    public async Task IgnoreAsync(Guid itemId, CancellationToken ct)
    {
        var item = await db.BankStatementItems
            .Include(i => i.Reconciliation)
            .FirstOrDefaultAsync(i => i.Id == itemId, ct)
            ?? throw new AppException("Item não encontrado.", 404);

        if (item.Reconciliation != null)
            db.BankReconciliations.Remove(item.Reconciliation);

        item.Status = BankStatementItemStatus.ManuallyIgnored;
        await db.SaveChangesAsync(ct);
    }

    private async Task<BankStatementListItem> GetStatementListItemAsync(
        Guid statementId, CancellationToken ct)
    {
        var stmt = await db.BankStatements
            .Include(s => s.BankAccount)
            .Include(s => s.Items)
            .FirstAsync(s => s.Id == statementId, ct);
        return ToListItem(stmt);
    }

    private static BankStatementListItem ToListItem(BankStatement s) => new(
        s.Id, s.BankAccountId, s.BankAccount?.Name ?? "",
        s.FileName, s.Format.ToString(),
        s.PeriodStart, s.PeriodEnd, s.ImportedAt, s.ItemCount,
        s.Items.Count(i => i.Status == BankStatementItemStatus.AutoConciliated),
        s.Items.Count(i => i.Status == BankStatementItemStatus.PendingReview),
        s.Items.Count(i => i.Status == BankStatementItemStatus.Unmatched),
        s.Items.Count(i => i.Status == BankStatementItemStatus.ManuallyIgnored));

    private static BankStatementItemResponse ToItemResponse(BankStatementItem i)
    {
        BankReconciliationResponse? recon = null;
        if (i.Reconciliation != null)
        {
            var r = i.Reconciliation;
            var t = r.Transaction;
            recon = new BankReconciliationResponse(
                r.Id, r.TransactionId,
                t?.Description ?? "",
                t?.Amount ?? 0,
                t?.DueDate ?? DateTime.MinValue,
                r.ConfidenceScore, r.MatchType.ToString(),
                r.CreatedByImport);
        }
        return new BankStatementItemResponse(
            i.Id, i.Date, i.Amount, i.Description,
            i.BankTransactionId, i.Status.ToString(), recon);
    }
}
```

- [ ] **Step 6: Rodar testes**

```bash
dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "BankReconciliationServiceTests"
```

Expected: 4 testes PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Conciliacao/ backend/src/GestorAI.API/Services/Conciliacao/ backend/tests/
git commit -m "feat: add BankAccountService and BankReconciliationService with matching algorithm

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 4: Backend — Endpoints e wiring em Program.cs

**Files:**
- Create: `backend/src/GestorAI.API/Endpoints/ConciliacaoEndpoints.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

**Interfaces:**
- Consumes: `BankAccountService`, `BankReconciliationService`, `BankStatementParserService`
- Produces: endpoints REST documentados no spec

- [ ] **Step 1: Criar ConciliacaoEndpoints.cs**

```csharp
// Endpoints/ConciliacaoEndpoints.cs
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Conciliacao;
using GestorAI.API.Services.Conciliacao;

namespace GestorAI.API.Endpoints;

public static class ConciliacaoEndpoints
{
    public static void MapConciliacao(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        // Bank Accounts
        group.MapGet("/bank-accounts", async (
            BankAccountService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPost("/bank-accounts", async (
            CreateBankAccountRequest req, BankAccountService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/bank-accounts/{result.Id}", result);
        });

        group.MapDelete("/bank-accounts/{id:guid}", async (
            Guid id, BankAccountService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        // Bank Statements
        group.MapPost("/bank-statements/import", async (
            HttpRequest httpReq,
            BankStatementParserService parser,
            BankReconciliationService svc,
            CancellationToken ct) =>
        {
            if (!httpReq.HasFormContentType)
                return Results.BadRequest("Envie um multipart/form-data.");

            var form = await httpReq.ReadFormAsync(ct);
            var file = form.Files.GetFile("file");
            if (file is null)
                return Results.BadRequest("Campo 'file' obrigatório.");

            if (!Guid.TryParse(form["bankAccountId"], out var bankAccountId))
                return Results.BadRequest("Campo 'bankAccountId' inválido.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            BankStatementFormat format;
            List<ParsedTransaction> parsed;

            using var stream = file.OpenReadStream();
            if (ext == ".ofx")
            {
                format = BankStatementFormat.OFX;
                parsed = await parser.ParseOfxAsync(stream);
            }
            else if (ext == ".csv")
            {
                format = BankStatementFormat.CSV;
                parsed = await parser.ParseCsvAsync(stream);
            }
            else
            {
                return Results.BadRequest("Formato não suportado. Use .ofx ou .csv");
            }

            if (parsed.Count == 0)
                return Results.BadRequest("Nenhuma transação encontrada no arquivo.");

            var result = await svc.ImportAsync(bankAccountId, file.FileName, format, parsed, ct);
            return Results.Created($"/api/bank-statements/{result.Id}", result);
        });

        group.MapGet("/bank-statements", async (
            Guid? bankAccountId,
            BankReconciliationService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListStatementsAsync(bankAccountId, ct)));

        group.MapGet("/bank-statements/{id:guid}/items", async (
            Guid id, BankReconciliationService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetItemsAsync(id, ct)));

        group.MapDelete("/bank-statements/{id:guid}", async (
            Guid id, BankReconciliationService svc, CancellationToken ct) =>
        {
            await svc.DeleteStatementAsync(id, ct);
            return Results.NoContent();
        });

        // Reconciliation actions
        group.MapPost("/bank-reconciliation/match", async (
            ManualMatchRequest req, BankReconciliationService svc, CancellationToken ct) =>
        {
            await svc.ManualMatchAsync(req, ct);
            return Results.Ok();
        });

        group.MapDelete("/bank-reconciliation/{id:guid}", async (
            Guid id, BankReconciliationService svc, CancellationToken ct) =>
        {
            await svc.UndoMatchAsync(id, ct);
            return Results.NoContent();
        });

        group.MapPost("/bank-reconciliation/{itemId:guid}/ignore", async (
            Guid itemId, BankReconciliationService svc, CancellationToken ct) =>
        {
            await svc.IgnoreAsync(itemId, ct);
            return Results.Ok();
        });
    }
}
```

- [ ] **Step 2: Registrar serviços e mapear endpoints em Program.cs**

Adicionar após `builder.Services.AddScoped<CobrancaService>();`:

```csharp
// Services — Conciliação Bancária
builder.Services.AddScoped<BankStatementParserService>();
builder.Services.AddScoped<BankAccountService>();
builder.Services.AddScoped<BankReconciliationService>();
```

Adicionar using no topo de Program.cs:
```csharp
using GestorAI.API.Services.Conciliacao;
```

Adicionar após `app.MapAssinaturas();`:
```csharp
app.MapConciliacao();
```

- [ ] **Step 3: Verificar compilação**

```bash
cd backend/src/GestorAI.API
dotnet build
```

Expected: `Build succeeded. 0 Warning(s). 0 Error(s).`

- [ ] **Step 4: Rodar todos os testes**

```bash
cd backend
dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj
```

Expected: todos PASS.

- [ ] **Step 5: Commit**

```bash
git add backend/src/GestorAI.API/Endpoints/ConciliacaoEndpoints.cs backend/src/GestorAI.API/Program.cs
git commit -m "feat: add conciliacao endpoints and wire up services

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 5: Frontend — Types, Hook e api.postForm

**Files:**
- Create: `frontend/src/types/conciliacao.ts`
- Create: `frontend/src/hooks/useConciliacao.ts`
- Modify: `frontend/src/services/api.ts`

**Interfaces:**
- Produces: `useConciliacao()` hook com todos os métodos usados pelas pages seguintes

- [ ] **Step 1: Criar tipos**

```typescript
// frontend/src/types/conciliacao.ts

export interface BankAccountResponse {
  id: string
  name: string
  bankName: string
  accountNumber: string | null
  agency: string | null
  isActive: boolean
}

export interface CreateBankAccountRequest {
  name: string
  bankName: string
  accountNumber?: string
  agency?: string
}

export interface BankReconciliationResponse {
  id: string
  transactionId: string
  transactionDescription: string
  transactionAmount: number
  transactionDueDate: string
  confidenceScore: number
  matchType: 'Auto' | 'Manual'
  createdByImport: boolean
}

export interface BankStatementItemResponse {
  id: string
  date: string
  amount: number
  description: string
  bankTransactionId: string | null
  status: 'AutoConciliated' | 'PendingReview' | 'ManuallyIgnored' | 'Unmatched'
  reconciliation: BankReconciliationResponse | null
}

export interface BankStatementListItem {
  id: string
  bankAccountId: string
  bankAccountName: string
  fileName: string
  format: 'OFX' | 'CSV'
  periodStart: string
  periodEnd: string
  importedAt: string
  itemCount: number
  autoConciliated: number
  pendingReview: number
  unmatched: number
  manuallyIgnored: number
}
```

- [ ] **Step 2: Adicionar `postForm` em api.ts**

No arquivo `frontend/src/services/api.ts`, adicionar dentro do objeto `export const api`:

```typescript
  postForm: <T>(path: string, body: FormData) =>
    request<T>(path, {
      method: 'POST',
      body,
      headers: {},  // remove Content-Type para o browser setar boundary do multipart
    }),
```

Atenção: a função `request` força `'Content-Type': 'application/json'` via spread, então é necessário sobrescrever com `headers: {}` no options, e ajustar `request` para não forçar Content-Type quando já há headers customizados. Alterar `request` para:

```typescript
async function request<T>(path: string, options: RequestInit = {}, retried = false): Promise<T> {
  const token = localStorage.getItem('ga_token')
  const hasCustomHeaders = options.headers && Object.keys(options.headers).length > 0

  const res = await fetch(`${API_BASE}${path}`, {
    ...options,
    headers: hasCustomHeaders ? {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options.headers,
    } : {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
  })
  // resto da função inalterado
```

- [ ] **Step 3: Criar hook useConciliacao**

```typescript
// frontend/src/hooks/useConciliacao.ts
import { useCallback } from 'react'
import { api } from '@/services/api'
import type {
  BankAccountResponse,
  BankStatementItemResponse,
  BankStatementListItem,
  CreateBankAccountRequest,
} from '@/types/conciliacao'

export function useConciliacao() {
  const listAccounts = useCallback(() =>
    api.get<BankAccountResponse[]>('/api/bank-accounts'), [])

  const createAccount = useCallback((req: CreateBankAccountRequest) =>
    api.post<BankAccountResponse>('/api/bank-accounts', req), [])

  const deleteAccount = useCallback((id: string) =>
    api.delete(`/api/bank-accounts/${id}`), [])

  const importStatement = useCallback((file: File, bankAccountId: string) => {
    const form = new FormData()
    form.append('file', file)
    form.append('bankAccountId', bankAccountId)
    return api.postForm<BankStatementListItem>('/api/bank-statements/import', form)
  }, [])

  const listStatements = useCallback((bankAccountId?: string) => {
    const qs = bankAccountId ? `?bankAccountId=${bankAccountId}` : ''
    return api.get<BankStatementListItem[]>(`/api/bank-statements${qs}`)
  }, [])

  const getStatementItems = useCallback((id: string) =>
    api.get<BankStatementItemResponse[]>(`/api/bank-statements/${id}/items`), [])

  const deleteStatement = useCallback((id: string) =>
    api.delete(`/api/bank-statements/${id}`), [])

  const manualMatch = useCallback((itemId: string, transactionId: string) =>
    api.post('/api/bank-reconciliation/match', { itemId, transactionId }), [])

  const undoMatch = useCallback((reconciliationId: string) =>
    api.delete(`/api/bank-reconciliation/${reconciliationId}`), [])

  const ignoreItem = useCallback((itemId: string) =>
    api.post(`/api/bank-reconciliation/${itemId}/ignore`, {}), [])

  return {
    listAccounts, createAccount, deleteAccount,
    importStatement, listStatements, getStatementItems, deleteStatement,
    manualMatch, undoMatch, ignoreItem,
  }
}
```

- [ ] **Step 4: Verificar tipos compilam**

```bash
cd frontend
npx tsc --noEmit
```

Expected: 0 erros.

- [ ] **Step 5: Commit**

```bash
git add frontend/src/types/conciliacao.ts frontend/src/hooks/useConciliacao.ts frontend/src/services/api.ts
git commit -m "feat: add conciliacao types, hook and api.postForm

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 6: Frontend — ContasBancarias page + rotas + nav

**Files:**
- Create: `frontend/src/pages/financeiro/ContasBancarias.tsx`
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/TopNav.tsx`

**Interfaces:**
- Consumes: `useConciliacao()` do Task 5

- [ ] **Step 1: Criar ContasBancarias.tsx**

```tsx
// frontend/src/pages/financeiro/ContasBancarias.tsx
import { useEffect, useState } from 'react'
import { useConciliacao } from '@/hooks/useConciliacao'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import { Trash2 } from 'lucide-react'
import type { BankAccountResponse } from '@/types/conciliacao'

export default function ContasBancarias() {
  const { listAccounts, createAccount, deleteAccount } = useConciliacao()
  const [accounts, setAccounts] = useState<BankAccountResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [name, setName] = useState('')
  const [bankName, setBankName] = useState('')
  const [accountNumber, setAccountNumber] = useState('')
  const [agency, setAgency] = useState('')
  const [saving, setSaving] = useState(false)

  async function load() {
    setLoading(true)
    try { setAccounts(await listAccounts()) }
    catch { toast.error('Erro ao carregar contas') }
    finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [])

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    if (!name.trim() || !bankName.trim()) return
    setSaving(true)
    try {
      await createAccount({ name: name.trim(), bankName: bankName.trim(), accountNumber: accountNumber.trim() || undefined, agency: agency.trim() || undefined })
      setName(''); setBankName(''); setAccountNumber(''); setAgency('')
      await load()
      toast.success('Conta criada')
    } catch { toast.error('Erro ao criar conta') }
    finally { setSaving(false) }
  }

  async function handleDelete(id: string) {
    try { await deleteAccount(id); await load(); toast.success('Conta removida') }
    catch { toast.error('Erro ao remover conta') }
  }

  return (
    <div className="max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold">Contas Bancárias</h1>

      <div className="rounded-xl border bg-card p-6 space-y-4">
        <h2 className="font-semibold text-sm">Nova Conta</h2>
        <form onSubmit={handleCreate} className="grid grid-cols-2 gap-3">
          <div className="space-y-1 col-span-2 sm:col-span-1">
            <Label>Nome da conta *</Label>
            <Input value={name} onChange={e => setName(e.target.value)} placeholder="Ex: Conta Corrente BB" required />
          </div>
          <div className="space-y-1 col-span-2 sm:col-span-1">
            <Label>Banco *</Label>
            <Input value={bankName} onChange={e => setBankName(e.target.value)} placeholder="Ex: Banco do Brasil" required />
          </div>
          <div className="space-y-1">
            <Label>Agência</Label>
            <Input value={agency} onChange={e => setAgency(e.target.value)} placeholder="0001" />
          </div>
          <div className="space-y-1">
            <Label>Conta</Label>
            <Input value={accountNumber} onChange={e => setAccountNumber(e.target.value)} placeholder="12345-6" />
          </div>
          <div className="col-span-2">
            <Button type="submit" disabled={saving}>{saving ? '...' : 'Adicionar Conta'}</Button>
          </div>
        </form>
      </div>

      <div className="rounded-xl border bg-card divide-y">
        {loading ? (
          <p className="p-4 text-sm text-muted-foreground">Carregando...</p>
        ) : accounts.length === 0 ? (
          <p className="p-4 text-sm text-muted-foreground">Nenhuma conta cadastrada.</p>
        ) : accounts.map(a => (
          <div key={a.id} className="flex items-center justify-between px-4 py-3">
            <div>
              <p className="font-medium text-sm">{a.name}</p>
              <p className="text-xs text-muted-foreground">{a.bankName}{a.agency ? ` · Ag. ${a.agency}` : ''}{a.accountNumber ? ` · Cc. ${a.accountNumber}` : ''}</p>
            </div>
            <Button size="icon" variant="ghost" className="h-8 w-8 text-muted-foreground hover:text-destructive"
              onClick={() => handleDelete(a.id)}>
              <Trash2 size={14} />
            </Button>
          </div>
        ))}
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Atualizar router/index.tsx**

Adicionar import:
```tsx
import ContasBancarias from '@/pages/financeiro/ContasBancarias'
import Conciliacao from '@/pages/financeiro/Conciliacao'
import ConciliacaoRevisao from '@/pages/financeiro/ConciliacaoRevisao'
```

Adicionar rotas após `{ path: '/financeiro/categorias', element: <Categorias /> }`:
```tsx
{ path: '/financeiro/contas-bancarias', element: <ContasBancarias /> },
{ path: '/financeiro/conciliacao', element: <Conciliacao /> },
{ path: '/financeiro/conciliacao/:id', element: <ConciliacaoRevisao /> },
```

- [ ] **Step 3: Atualizar TopNav.tsx**

No array de itens do grupo `Financeiro`, adicionar após `{ icon: Tag, label: 'Categorias', path: '/financeiro/categorias' }`:

```tsx
{ icon: Landmark, label: 'Contas Bancárias', path: '/financeiro/contas-bancarias' },
{ icon: GitMerge,  label: 'Conciliação',      path: '/financeiro/conciliacao' },
```

E adicionar `Landmark, GitMerge` ao import do lucide-react no topo do arquivo.

- [ ] **Step 4: Verificar tipos**

```bash
cd frontend && npx tsc --noEmit
```

Expected: 0 erros (os imports de Conciliacao/ConciliacaoRevisao ainda não existem — criaremos no próximo task; pode usar arquivos placeholder temporários se necessário para compilar).

- [ ] **Step 5: Commit**

```bash
git add frontend/src/pages/financeiro/ContasBancarias.tsx frontend/src/router/ frontend/src/components/layout/TopNav.tsx
git commit -m "feat: add ContasBancarias page, routes and nav items

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 7: Frontend — Conciliacao page (lista + modal import)

**Files:**
- Create: `frontend/src/pages/financeiro/Conciliacao.tsx`

**Interfaces:**
- Consumes: `useConciliacao()` do Task 5
- Produces: página que navega para `/financeiro/conciliacao/:id`

- [ ] **Step 1: Criar Conciliacao.tsx**

```tsx
// frontend/src/pages/financeiro/Conciliacao.tsx
import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useConciliacao } from '@/hooks/useConciliacao'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { toast } from '@/hooks/useToast'
import { Upload, Trash2 } from 'lucide-react'
import type { BankAccountResponse, BankStatementListItem } from '@/types/conciliacao'

const fmtDate = (s: string) => new Date(s + 'T12:00:00').toLocaleDateString('pt-BR')

export default function Conciliacao() {
  const navigate = useNavigate()
  const { listAccounts, listStatements, deleteStatement, importStatement } = useConciliacao()
  const [accounts, setAccounts] = useState<BankAccountResponse[]>([])
  const [statements, setStatements] = useState<BankStatementListItem[]>([])
  const [selectedAccount, setSelectedAccount] = useState('')
  const [loading, setLoading] = useState(true)
  const [showModal, setShowModal] = useState(false)
  const [importAccount, setImportAccount] = useState('')
  const [importing, setImporting] = useState(false)
  const fileRef = useRef<HTMLInputElement>(null)

  async function load() {
    setLoading(true)
    try {
      const [accs, stmts] = await Promise.all([
        listAccounts(),
        listStatements(selectedAccount || undefined),
      ])
      setAccounts(accs)
      setStatements(stmts)
    } catch { toast.error('Erro ao carregar dados') }
    finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [selectedAccount])

  async function handleImport() {
    const file = fileRef.current?.files?.[0]
    if (!file || !importAccount) return
    setImporting(true)
    try {
      const stmt = await importStatement(file, importAccount)
      setShowModal(false)
      toast.success(`${stmt.itemCount} transações importadas`)
      navigate(`/financeiro/conciliacao/${stmt.id}`)
    } catch (e) {
      toast.error(e instanceof Error ? e.message : 'Erro ao importar')
    } finally { setImporting(false) }
  }

  async function handleDelete(id: string) {
    try { await deleteStatement(id); await load(); toast.success('Extrato removido') }
    catch { toast.error('Erro ao remover extrato') }
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">Conciliação Bancária</h1>
        <Button onClick={() => setShowModal(true)}>
          <Upload size={15} className="mr-2" /> Importar Extrato
        </Button>
      </div>

      <div className="flex gap-3 items-center">
        <select
          value={selectedAccount}
          onChange={e => setSelectedAccount(e.target.value)}
          className="flex h-9 rounded-md border border-input bg-transparent px-3 py-1 text-sm"
        >
          <option value="">Todas as contas</option>
          {accounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
        </select>
      </div>

      <div className="rounded-xl border bg-card divide-y">
        {loading ? (
          <p className="p-4 text-sm text-muted-foreground">Carregando...</p>
        ) : statements.length === 0 ? (
          <p className="p-4 text-sm text-muted-foreground">Nenhum extrato importado.</p>
        ) : statements.map(s => (
          <div key={s.id} className="flex items-center justify-between px-4 py-3">
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <span className="font-medium text-sm truncate">{s.fileName}</span>
                <Badge variant="outline" className="text-xs">{s.format}</Badge>
              </div>
              <p className="text-xs text-muted-foreground mt-0.5">
                {s.bankAccountName} · {fmtDate(s.periodStart)} – {fmtDate(s.periodEnd)}
              </p>
              <div className="flex gap-2 mt-1 flex-wrap">
                {s.autoConciliated > 0 && <Badge variant="secondary">{s.autoConciliated} conciliados</Badge>}
                {s.pendingReview > 0 && <Badge variant="outline" className="border-yellow-400 text-yellow-600">{s.pendingReview} pendentes</Badge>}
                {s.unmatched > 0 && <Badge variant="outline">{s.unmatched} novos lançamentos</Badge>}
              </div>
            </div>
            <div className="flex gap-2 shrink-0">
              <Button size="sm" variant="outline" onClick={() => navigate(`/financeiro/conciliacao/${s.id}`)}>
                Revisar
              </Button>
              <Button size="icon" variant="ghost" className="h-8 w-8 text-muted-foreground hover:text-destructive"
                onClick={() => handleDelete(s.id)}>
                <Trash2 size={14} />
              </Button>
            </div>
          </div>
        ))}
      </div>

      {showModal && (
        <div className="fixed inset-0 bg-black/40 z-50 flex items-center justify-center p-4">
          <div className="bg-card rounded-xl border shadow-xl p-6 w-full max-w-sm space-y-4">
            <h2 className="font-semibold">Importar Extrato</h2>
            <div className="space-y-2">
              <label className="text-sm font-medium">Conta bancária</label>
              <select
                value={importAccount}
                onChange={e => setImportAccount(e.target.value)}
                className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-sm"
              >
                <option value="">Selecionar...</option>
                {accounts.map(a => <option key={a.id} value={a.id}>{a.name}</option>)}
              </select>
            </div>
            <div className="space-y-2">
              <label className="text-sm font-medium">Arquivo (.ofx ou .csv)</label>
              <input ref={fileRef} type="file" accept=".ofx,.csv"
                className="text-sm file:mr-3 file:rounded-md file:border file:px-3 file:py-1 file:text-sm file:font-medium" />
            </div>
            <div className="flex gap-2 justify-end">
              <Button variant="ghost" onClick={() => setShowModal(false)}>Cancelar</Button>
              <Button onClick={handleImport} disabled={importing || !importAccount}>
                {importing ? '...' : 'Importar'}
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Verificar build**

```bash
cd frontend && npx tsc --noEmit
```

Expected: 0 erros (ConciliacaoRevisao ainda não existe mas a rota está importada — criar um placeholder se necessário: `export default function ConciliacaoRevisao() { return <div /> }`).

- [ ] **Step 3: Commit**

```bash
git add frontend/src/pages/financeiro/Conciliacao.tsx
git commit -m "feat: add Conciliacao main page with import modal

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Task 8: Frontend — ConciliacaoRevisao page (3 abas)

**Files:**
- Create: `frontend/src/pages/financeiro/ConciliacaoRevisao.tsx`

**Interfaces:**
- Consumes: `useConciliacao()`, `useFinanceiro()` (para buscar lançamentos na busca manual)
- Consumes: `BankStatementItemResponse`, `BankReconciliationResponse` do Task 5

- [ ] **Step 1: Criar ConciliacaoRevisao.tsx**

```tsx
// frontend/src/pages/financeiro/ConciliacaoRevisao.tsx
import { useEffect, useState } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { useConciliacao } from '@/hooks/useConciliacao'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { toast } from '@/hooks/useToast'
import { CheckCircle2, XCircle, EyeOff, Undo2, ArrowLeft } from 'lucide-react'
import type { BankStatementItemResponse } from '@/types/conciliacao'

type Tab = 'pendentes' | 'conciliados' | 'novos'

const fmt = (v: number) => Math.abs(v).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })
const fmtDate = (s: string) => new Date(s + 'T12:00:00').toLocaleDateString('pt-BR')

function ItemCard({ item, onConfirm, onReject, onIgnore, onUndo, tab }: {
  item: BankStatementItemResponse
  tab: Tab
  onConfirm?: () => void
  onReject?: () => void
  onIgnore?: () => void
  onUndo?: () => void
}) {
  const r = item.reconciliation
  return (
    <div className="rounded-lg border bg-card p-4 space-y-2">
      <div className="flex items-start justify-between gap-2">
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium truncate">{item.description}</p>
          <p className="text-xs text-muted-foreground">
            {fmtDate(item.date)} · {item.amount > 0 ? '+' : ''}{fmt(item.amount)}
          </p>
        </div>
        {tab === 'pendentes' && r && (
          <Badge variant="outline" className="text-xs shrink-0">
            {r.confidenceScore}% confiança
          </Badge>
        )}
      </div>

      {r && (
        <div className="rounded-md bg-muted/50 px-3 py-2 text-xs space-y-0.5">
          <p className="font-medium text-muted-foreground">Lançamento sugerido</p>
          <p>{r.transactionDescription}</p>
          <p className="text-muted-foreground">
            {fmtDate(r.transactionDueDate)} · {fmt(r.transactionAmount)}
            {r.createdByImport && ' · gerado automaticamente'}
          </p>
        </div>
      )}

      <div className="flex gap-2">
        {tab === 'pendentes' && (
          <>
            <Button size="sm" variant="outline" className="gap-1" onClick={onConfirm}>
              <CheckCircle2 size={13} /> Confirmar
            </Button>
            <Button size="sm" variant="ghost" className="gap-1 text-muted-foreground" onClick={onReject}>
              <XCircle size={13} /> Rejeitar
            </Button>
            <Button size="sm" variant="ghost" className="gap-1 text-muted-foreground" onClick={onIgnore}>
              <EyeOff size={13} /> Ignorar
            </Button>
          </>
        )}
        {tab === 'conciliados' && (
          <Button size="sm" variant="ghost" className="gap-1 text-muted-foreground" onClick={onUndo}>
            <Undo2 size={13} /> Desfazer
          </Button>
        )}
        {tab === 'novos' && r?.createdByImport && (
          <Button size="sm" variant="ghost" className="gap-1 text-muted-foreground" onClick={onReject}>
            <Trash2 size={13} /> Excluir lançamento
          </Button>
        )}
      </div>
    </div>
  )
}

// add Trash2 to import above

export default function ConciliacaoRevisao() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { getStatementItems, manualMatch, undoMatch, ignoreItem, deleteStatement } = useConciliacao()
  const [items, setItems] = useState<BankStatementItemResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [tab, setTab] = useState<Tab>('pendentes')

  async function load() {
    if (!id) return
    setLoading(true)
    try { setItems(await getStatementItems(id)) }
    catch { toast.error('Erro ao carregar itens') }
    finally { setLoading(false) }
  }

  useEffect(() => { void load() }, [id])

  async function handleConfirm(item: BankStatementItemResponse) {
    if (!item.reconciliation) return
    try {
      await manualMatch(item.id, item.reconciliation.transactionId)
      await load()
    } catch { toast.error('Erro ao confirmar') }
  }

  async function handleReject(item: BankStatementItemResponse) {
    if (!item.reconciliation) return
    try {
      await undoMatch(item.reconciliation.id)
      await load()
    } catch { toast.error('Erro ao rejeitar') }
  }

  async function handleIgnore(item: BankStatementItemResponse) {
    try { await ignoreItem(item.id); await load() }
    catch { toast.error('Erro ao ignorar') }
  }

  async function handleUndo(item: BankStatementItemResponse) {
    if (!item.reconciliation) return
    try { await undoMatch(item.reconciliation.id); await load() }
    catch { toast.error('Erro ao desfazer') }
  }

  const pendentes = items.filter(i => i.status === 'PendingReview')
  const conciliados = items.filter(i => i.status === 'AutoConciliated')
  const novos = items.filter(i => i.status === 'Unmatched' && i.reconciliation?.createdByImport)

  const tabItems = tab === 'pendentes' ? pendentes : tab === 'conciliados' ? conciliados : novos

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <Button variant="ghost" size="icon" onClick={() => navigate('/financeiro/conciliacao')}>
          <ArrowLeft size={16} />
        </Button>
        <h1 className="text-xl font-bold">Revisão de Extrato</h1>
      </div>

      <div className="flex gap-1 border-b">
        {([
          { key: 'pendentes' as Tab, label: 'Pendentes', count: pendentes.length },
          { key: 'conciliados' as Tab, label: 'Conciliados', count: conciliados.length },
          { key: 'novos' as Tab, label: 'Novos Lançamentos', count: novos.length },
        ] as const).map(t => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
              tab === t.key
                ? 'border-primary text-primary'
                : 'border-transparent text-muted-foreground hover:text-foreground'
            }`}
          >
            {t.label}
            {t.count > 0 && (
              <span className="ml-1.5 rounded-full bg-muted px-1.5 py-0.5 text-xs">{t.count}</span>
            )}
          </button>
        ))}
      </div>

      {loading ? (
        <p className="text-sm text-muted-foreground">Carregando...</p>
      ) : tabItems.length === 0 ? (
        <p className="text-sm text-muted-foreground py-6 text-center">
          {tab === 'pendentes' ? 'Nenhum item pendente de revisão.' :
           tab === 'conciliados' ? 'Nenhum item conciliado ainda.' :
           'Nenhum lançamento gerado automaticamente.'}
        </p>
      ) : (
        <div className="space-y-3">
          {tabItems.map(item => (
            <ItemCard
              key={item.id}
              item={item}
              tab={tab}
              onConfirm={() => handleConfirm(item)}
              onReject={() => handleReject(item)}
              onIgnore={() => handleIgnore(item)}
              onUndo={() => handleUndo(item)}
            />
          ))}
        </div>
      )}
    </div>
  )
}
```

Adicionar `Trash2` ao import de lucide-react no topo do arquivo:
```tsx
import { CheckCircle2, XCircle, EyeOff, Undo2, ArrowLeft, Trash2 } from 'lucide-react'
```

- [ ] **Step 2: Verificar build completo**

```bash
cd frontend && npx tsc --noEmit
```

Expected: 0 erros.

- [ ] **Step 3: Rodar lint**

```bash
cd frontend && npm run lint
```

Expected: 0 erros.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/pages/financeiro/ConciliacaoRevisao.tsx
git commit -m "feat: add ConciliacaoRevisao page with 3-tab review flow

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>"
```

---

## Verificação Final

- [ ] Backend compila: `cd backend/src/GestorAI.API && dotnet build`
- [ ] Todos testes passam: `cd backend && dotnet test tests/GestorAI.Tests/GestorAI.Tests.csproj`
- [ ] Frontend compila: `cd frontend && npx tsc --noEmit`
- [ ] Migration aplicada: `cd backend/src/GestorAI.API && dotnet ef database update`
- [ ] Dev server inicia sem erros: `cd frontend && npm run dev`
