# Exportação Contábil — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Plano de Contas hierárquico + mapeamento de categorias + exportação de lançamentos para Domínio (TXT) e Fortes (CSV).

**Architecture:** Núcleo + adaptadores — `AccountingExportService` monta `AccountingEntry` normalizadas a partir das `Transaction`s pagas; `DominioExporter` e `FortesExporter` convertem para o formato de cada sistema. `ChartOfAccount` é hierárquico (auto-referência) com template NBC TG pré-preenchido. `AccountMapping` faz o de-para `Transaction.Category` → `ChartOfAccount`.

**Tech Stack:** ASP.NET Core 10 Minimal API (C#), EF Core, PostgreSQL, React + TypeScript + Vite + Tailwind + shadcn/ui.

## Global Constraints

- Multi-tenancy via `CompanyId` com `HasQueryFilter` em `AppDbContext` — todas as novas entidades tenant-scoped
- Nunca usar `FindAsync` em entidades com global query filter — usar `FirstOrDefaultAsync` com predicado explícito
- JSON camelCase + `JsonStringEnumConverter` globalmente no backend
- DB schema `gestor`, snake_case via `EFCore.NamingConventions`
- Todos os endpoints com `.RequireAuthorization()`; endpoints destrutivos (DELETE) com `.RequireAuthorization("AdminOnly")`
- Frontend: shadcn/ui components, `toast` via `@/hooks/useToast`, `api.*` via `@/services/api`
- `Transaction.PaymentDate` é a data do lançamento contábil (não `DueDate`)
- Export inclui apenas `Status = Pago` por padrão; `includePending = true` inclui `Pendente` também
- Lógica de débito/crédito: Receita → `DR caixa / CR conta mapeada`; Despesa → `DR conta mapeada / CR caixa`

---

### Task 1: Entidades, Enums e Migration

**Files:**
- Create: `backend/src/GestorAI.API/Domain/Enums/AccountType.cs`
- Create: `backend/src/GestorAI.API/Domain/Enums/AccountingSystem.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/ChartOfAccount.cs`
- Create: `backend/src/GestorAI.API/Domain/Entities/AccountMapping.cs`
- Modify: `backend/src/GestorAI.API/Domain/Entities/CompanySettings.cs`
- Modify: `backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs`
- Create: migration `AddAccountingExport`

**Interfaces:**
- Produces: `ChartOfAccount`, `AccountMapping` entities; `AccountType`, `AccountingSystem` enums; DbSets + query filters

- [ ] **Step 1: Criar enums**

`backend/src/GestorAI.API/Domain/Enums/AccountType.cs`:
```csharp
namespace GestorAI.API.Domain.Enums;
public enum AccountType { Ativo, Passivo, PatrimonioLiquido, Receita, Despesa }
```

`backend/src/GestorAI.API/Domain/Enums/AccountingSystem.cs`:
```csharp
namespace GestorAI.API.Domain.Enums;
public enum AccountingSystem { Dominio, Fortes }
```

- [ ] **Step 2: Criar entidade ChartOfAccount**

`backend/src/GestorAI.API/Domain/Entities/ChartOfAccount.cs`:
```csharp
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class ChartOfAccount : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public AccountType Type { get; set; }
    public Guid? ParentId { get; set; }
    public bool IsActive { get; set; } = true;
    public ChartOfAccount? Parent { get; set; }
    public List<ChartOfAccount> Children { get; set; } = [];
}
```

- [ ] **Step 3: Criar entidade AccountMapping**

`backend/src/GestorAI.API/Domain/Entities/AccountMapping.cs`:
```csharp
namespace GestorAI.API.Domain.Entities;

public class AccountMapping : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string CategoryName { get; set; }
    public Guid AccountId { get; set; }
    public ChartOfAccount? Account { get; set; }
}
```

- [ ] **Step 4: Adicionar campos em CompanySettings**

Em `backend/src/GestorAI.API/Domain/Entities/CompanySettings.cs`, adicionar ao final da classe (antes do último `}`):
```csharp
    // Accounting export
    public Guid? DefaultCashAccountId { get; set; }
    public AccountingSystem? PreferredAccountingSystem { get; set; }
```

Adicionar using no topo do arquivo:
```csharp
using GestorAI.API.Domain.Enums;
```

- [ ] **Step 5: Atualizar AppDbContext**

Adicionar DbSets após `BankReconciliation`:
```csharp
public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();
public DbSet<AccountMapping> AccountMappings => Set<AccountMapping>();
```

Adicionar na seção `// Bank Reconciliation` de `OnModelCreating`, após as configurações existentes:
```csharp
// Accounting Export
modelBuilder.Entity<ChartOfAccount>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);
modelBuilder.Entity<AccountMapping>().HasQueryFilter(e => e.CompanyId == tenantContext.CompanyId);

modelBuilder.Entity<ChartOfAccount>()
    .HasIndex(c => new { c.CompanyId, c.Code })
    .IsUnique();

modelBuilder.Entity<ChartOfAccount>()
    .HasOne(c => c.Parent)
    .WithMany(c => c.Children)
    .HasForeignKey(c => c.ParentId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<AccountMapping>()
    .HasIndex(m => new { m.CompanyId, m.CategoryName })
    .IsUnique();

modelBuilder.Entity<AccountMapping>()
    .HasOne(m => m.Account)
    .WithMany()
    .HasForeignKey(m => m.AccountId)
    .OnDelete(DeleteBehavior.Restrict);
```

- [ ] **Step 6: Gerar migration**

```bash
cd backend/src/GestorAI.API
dotnet ef migrations add AddAccountingExport
```

Verificar que o arquivo gerado contém: tabelas `chart_of_accounts`, `account_mappings`, colunas `default_cash_account_id` e `preferred_accounting_system` em `company_settings`.

- [ ] **Step 7: Verificar build**

```bash
cd backend/src/GestorAI.API
dotnet build
```
Expected: 0 errors.

- [ ] **Step 8: Commit**

```bash
git add backend/src/GestorAI.API/Domain/Enums/AccountType.cs \
        backend/src/GestorAI.API/Domain/Enums/AccountingSystem.cs \
        backend/src/GestorAI.API/Domain/Entities/ChartOfAccount.cs \
        backend/src/GestorAI.API/Domain/Entities/AccountMapping.cs \
        backend/src/GestorAI.API/Domain/Entities/CompanySettings.cs \
        backend/src/GestorAI.API/Infrastructure/Data/AppDbContext.cs \
        backend/src/GestorAI.API/Infrastructure/Data/Migrations/
git commit -m "feat: add ChartOfAccount, AccountMapping entities and migration"
```

---

### Task 2: ChartOfAccountService + DTOs + testes

**Files:**
- Create: `backend/src/GestorAI.API/DTOs/Contabilidade/ChartOfAccountDto.cs`
- Create: `backend/src/GestorAI.API/Services/Contabilidade/ChartOfAccountService.cs`
- Create: `backend/tests/GestorAI.Tests/Services/ChartOfAccountServiceTests.cs`

**Interfaces:**
- Consumes: `ChartOfAccount` entity, `AccountType` enum, `AppDbContext`, `TenantContext`
- Produces: `ChartOfAccountService` with `ListAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `LoadTemplateAsync`; DTOs `ChartOfAccountResponse`, `CreateChartOfAccountRequest`, `UpdateChartOfAccountRequest`

- [ ] **Step 1: Criar DTOs**

`backend/src/GestorAI.API/DTOs/Contabilidade/ChartOfAccountDto.cs`:
```csharp
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Contabilidade;

public record CreateChartOfAccountRequest(
    string Code,
    string Name,
    AccountType Type,
    Guid? ParentId);

public record UpdateChartOfAccountRequest(
    string Code,
    string Name);

public record ChartOfAccountResponse(
    Guid Id,
    string Code,
    string Name,
    AccountType Type,
    Guid? ParentId,
    bool IsActive,
    List<ChartOfAccountResponse> Children);
```

- [ ] **Step 2: Escrever testes**

`backend/tests/GestorAI.Tests/Services/ChartOfAccountServiceTests.cs`:
```csharp
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Services.Contabilidade;
using GestorAI.Tests.Helpers;

namespace GestorAI.Tests.Services;

public class ChartOfAccountServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsAccount()
    {
        var (db, tenant) = TestDbHelper.Create();
        var svc = new ChartOfAccountService(db, tenant);

        var req = new CreateChartOfAccountRequest("4.1.1", "Receita de Vendas", AccountType.Receita, null);
        var result = await svc.CreateAsync(req, default);

        Assert.Equal("4.1.1", result.Code);
        Assert.Equal("Receita de Vendas", result.Name);
        Assert.Equal(AccountType.Receita, result.Type);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesAccount()
    {
        var (db, tenant) = TestDbHelper.Create();
        var svc = new ChartOfAccountService(db, tenant);

        var req = new CreateChartOfAccountRequest("5.1.1", "Despesas Admin", AccountType.Despesa, null);
        var created = await svc.CreateAsync(req, default);

        await svc.DeleteAsync(created.Id, default);

        var all = await svc.ListAsync(default);
        Assert.DoesNotContain(all, a => a.Id == created.Id && a.IsActive);
    }

    [Fact]
    public async Task LoadTemplateAsync_CreatesDefaultAccounts()
    {
        var (db, tenant) = TestDbHelper.Create();
        var svc = new ChartOfAccountService(db, tenant);

        await svc.LoadTemplateAsync(default);

        var all = await svc.ListAsync(default);
        // flat list includes all levels
        var flat = Flatten(all);
        Assert.True(flat.Count >= 20);
        Assert.Contains(flat, a => a.Code == "1" && a.Name == "Ativo");
        Assert.Contains(flat, a => a.Code == "4" && a.Name == "Receitas");
        Assert.Contains(flat, a => a.Code == "5" && a.Name == "Despesas");
    }

    [Fact]
    public async Task LoadTemplateAsync_DoesNothingIfAccountsExist()
    {
        var (db, tenant) = TestDbHelper.Create();
        var svc = new ChartOfAccountService(db, tenant);

        var req = new CreateChartOfAccountRequest("1", "Ativo", AccountType.Ativo, null);
        await svc.CreateAsync(req, default);

        await svc.LoadTemplateAsync(default); // should be no-op

        var all = await svc.ListAsync(default);
        var flat = Flatten(all);
        Assert.Single(flat); // only the one we created
    }

    private static List<ChartOfAccountResponse> Flatten(List<ChartOfAccountResponse> list)
    {
        var result = new List<ChartOfAccountResponse>();
        foreach (var item in list)
        {
            result.Add(item);
            result.AddRange(Flatten(item.Children));
        }
        return result;
    }
}
```

- [ ] **Step 3: Rodar testes — devem falhar**

```bash
dotnet test backend/tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "ChartOfAccountServiceTests"
```
Expected: FAIL com `ChartOfAccountService not found`.

- [ ] **Step 4: Implementar ChartOfAccountService**

`backend/src/GestorAI.API/Services/Contabilidade/ChartOfAccountService.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contabilidade;

public class ChartOfAccountService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ChartOfAccountResponse>> ListAsync(CancellationToken ct)
    {
        var all = await db.ChartOfAccounts
            .OrderBy(a => a.Code)
            .ToListAsync(ct);
        return BuildTree(all, null);
    }

    public async Task<ChartOfAccountResponse> CreateAsync(CreateChartOfAccountRequest req, CancellationToken ct)
    {
        var account = new ChartOfAccount
        {
            CompanyId = tenantContext.CompanyId,
            Code = req.Code,
            Name = req.Name,
            Type = req.Type,
            ParentId = req.ParentId,
        };
        db.ChartOfAccounts.Add(account);
        await db.SaveChangesAsync(ct);
        return ToResponse(account, []);
    }

    public async Task<ChartOfAccountResponse> UpdateAsync(Guid id, UpdateChartOfAccountRequest req, CancellationToken ct)
    {
        var account = await db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Conta não encontrada.", 404);
        account.Code = req.Code;
        account.Name = req.Name;
        await db.SaveChangesAsync(ct);
        return ToResponse(account, []);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var account = await db.ChartOfAccounts.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Conta não encontrada.", 404);
        account.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    public async Task LoadTemplateAsync(CancellationToken ct)
    {
        var hasAny = await db.ChartOfAccounts.AnyAsync(ct);
        if (hasAny) return;

        var entries = GetTemplate();
        var idMap = new Dictionary<string, Guid>();

        foreach (var (code, name, type, parentCode) in entries)
        {
            Guid? parentId = parentCode != null && idMap.TryGetValue(parentCode, out var pid) ? pid : null;
            var account = new ChartOfAccount
            {
                CompanyId = tenantContext.CompanyId,
                Code = code,
                Name = name,
                Type = type,
                ParentId = parentId,
            };
            db.ChartOfAccounts.Add(account);
            await db.SaveChangesAsync(ct);
            idMap[code] = account.Id;
        }
    }

    private static List<ChartOfAccountResponse> BuildTree(List<ChartOfAccount> all, Guid? parentId) =>
        all.Where(a => a.ParentId == parentId)
           .Select(a => ToResponse(a, BuildTree(all, a.Id)))
           .ToList();

    private static ChartOfAccountResponse ToResponse(ChartOfAccount a, List<ChartOfAccountResponse> children) =>
        new(a.Id, a.Code, a.Name, a.Type, a.ParentId, a.IsActive, children);

    private static List<(string Code, string Name, AccountType Type, string? ParentCode)> GetTemplate() =>
    [
        ("1",       "Ativo",                            AccountType.Ativo,           null),
        ("1.1",     "Ativo Circulante",                 AccountType.Ativo,           "1"),
        ("1.1.1",   "Caixa e Equivalentes de Caixa",   AccountType.Ativo,           "1.1"),
        ("1.1.1.01","Caixa",                            AccountType.Ativo,           "1.1.1"),
        ("1.1.1.02","Banco Conta Corrente",             AccountType.Ativo,           "1.1.1"),
        ("1.1.2",   "Contas a Receber",                 AccountType.Ativo,           "1.1"),
        ("1.1.2.01","Clientes",                         AccountType.Ativo,           "1.1.2"),
        ("1.1.3",   "Estoques",                         AccountType.Ativo,           "1.1"),
        ("1.2",     "Ativo Não Circulante",             AccountType.Ativo,           "1"),
        ("1.2.1",   "Imobilizado",                      AccountType.Ativo,           "1.2"),
        ("2",       "Passivo",                          AccountType.Passivo,         null),
        ("2.1",     "Passivo Circulante",               AccountType.Passivo,         "2"),
        ("2.1.1",   "Fornecedores",                     AccountType.Passivo,         "2.1"),
        ("2.1.2",   "Obrigações Fiscais",               AccountType.Passivo,         "2.1"),
        ("2.1.3",   "Obrigações Trabalhistas",          AccountType.Passivo,         "2.1"),
        ("2.2",     "Passivo Não Circulante",           AccountType.Passivo,         "2"),
        ("3",       "Patrimônio Líquido",               AccountType.PatrimonioLiquido, null),
        ("3.1",     "Capital Social",                   AccountType.PatrimonioLiquido, "3"),
        ("3.2",     "Lucros/Prejuízos Acumulados",      AccountType.PatrimonioLiquido, "3"),
        ("4",       "Receitas",                         AccountType.Receita,         null),
        ("4.1",     "Receitas Operacionais",            AccountType.Receita,         "4"),
        ("4.1.1",   "Receita de Vendas",                AccountType.Receita,         "4.1"),
        ("4.1.2",   "Receita de Serviços",              AccountType.Receita,         "4.1"),
        ("4.2",     "Outras Receitas",                  AccountType.Receita,         "4"),
        ("5",       "Despesas",                         AccountType.Despesa,         null),
        ("5.1",     "Despesas Operacionais",            AccountType.Despesa,         "5"),
        ("5.1.1",   "Despesas Administrativas",         AccountType.Despesa,         "5.1"),
        ("5.1.1.01","Aluguel",                          AccountType.Despesa,         "5.1.1"),
        ("5.1.1.02","Água e Energia Elétrica",          AccountType.Despesa,         "5.1.1"),
        ("5.1.1.03","Material de Escritório",           AccountType.Despesa,         "5.1.1"),
        ("5.1.1.04","Telefone e Internet",              AccountType.Despesa,         "5.1.1"),
        ("5.1.2",   "Despesas com Pessoal",             AccountType.Despesa,         "5.1"),
        ("5.1.2.01","Salários e Ordenados",             AccountType.Despesa,         "5.1.2"),
        ("5.1.2.02","Encargos Sociais",                 AccountType.Despesa,         "5.1.2"),
        ("5.1.2.03","Pró-Labore",                       AccountType.Despesa,         "5.1.2"),
        ("5.1.3",   "Despesas Tributárias",             AccountType.Despesa,         "5.1"),
        ("5.1.3.01","Impostos e Taxas",                 AccountType.Despesa,         "5.1.3"),
        ("5.1.3.02","Simples Nacional",                 AccountType.Despesa,         "5.1.3"),
        ("5.1.4",   "Despesas Financeiras",             AccountType.Despesa,         "5.1"),
        ("5.1.4.01","Juros e Encargos Bancários",       AccountType.Despesa,         "5.1.4"),
        ("5.1.4.02","Tarifas Bancárias",                AccountType.Despesa,         "5.1.4"),
        ("5.2",     "Custo das Mercadorias/Serviços",   AccountType.Despesa,         "5"),
        ("5.2.1",   "CMV/CSV",                          AccountType.Despesa,         "5.2"),
    ];
}
```

- [ ] **Step 5: Rodar testes — devem passar**

```bash
dotnet test backend/tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "ChartOfAccountServiceTests"
```
Expected: 4/4 PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Contabilidade/ \
        backend/src/GestorAI.API/Services/Contabilidade/ChartOfAccountService.cs \
        backend/tests/GestorAI.Tests/Services/ChartOfAccountServiceTests.cs
git commit -m "feat: add ChartOfAccountService with NBC TG template and tests"
```

---

### Task 3: AccountMappingService + AccountingSettingsService + testes

**Files:**
- Create: `backend/src/GestorAI.API/DTOs/Contabilidade/AccountMappingDto.cs`
- Create: `backend/src/GestorAI.API/DTOs/Contabilidade/AccountingSettingsDto.cs`
- Create: `backend/src/GestorAI.API/Services/Contabilidade/AccountMappingService.cs`
- Create: `backend/src/GestorAI.API/Services/Contabilidade/AccountingSettingsService.cs`
- Create: `backend/tests/GestorAI.Tests/Services/AccountMappingServiceTests.cs`

**Interfaces:**
- Consumes: `AccountMapping`, `CompanySettings`, `ChartOfAccount` entities
- Produces: `AccountMappingService.ListAsync`, `BulkUpsertAsync`; `AccountingSettingsService.GetAsync`, `UpdateAsync`

- [ ] **Step 1: Criar DTOs**

`backend/src/GestorAI.API/DTOs/Contabilidade/AccountMappingDto.cs`:
```csharp
namespace GestorAI.API.DTOs.Contabilidade;

public record AccountMappingItem(string CategoryName, Guid AccountId);
public record BulkUpsertAccountMappingsRequest(List<AccountMappingItem> Mappings);

public record AccountMappingResponse(
    Guid Id,
    string CategoryName,
    Guid AccountId,
    string AccountCode,
    string AccountName);
```

`backend/src/GestorAI.API/DTOs/Contabilidade/AccountingSettingsDto.cs`:
```csharp
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Contabilidade;

public record UpdateAccountingSettingsRequest(
    Guid? DefaultCashAccountId,
    AccountingSystem? PreferredAccountingSystem);

public record AccountingSettingsResponse(
    Guid? DefaultCashAccountId,
    string? DefaultCashAccountCode,
    string? DefaultCashAccountName,
    AccountingSystem? PreferredAccountingSystem);
```

- [ ] **Step 2: Escrever testes**

`backend/tests/GestorAI.Tests/Services/AccountMappingServiceTests.cs`:
```csharp
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Services.Contabilidade;
using GestorAI.Tests.Helpers;

namespace GestorAI.Tests.Services;

public class AccountMappingServiceTests
{
    [Fact]
    public async Task BulkUpsertAsync_CreatesMappings()
    {
        var (db, tenant) = TestDbHelper.Create();
        var coaSvc = new ChartOfAccountService(db, tenant);
        var account = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("4.1.1", "Receita Vendas", AccountType.Receita, null), default);

        var svc = new AccountMappingService(db, tenant);
        var req = new BulkUpsertAccountMappingsRequest([new("Vendas", account.Id)]);
        await svc.BulkUpsertAsync(req, default);

        var list = await svc.ListAsync(default);
        Assert.Single(list);
        Assert.Equal("Vendas", list[0].CategoryName);
        Assert.Equal(account.Id, list[0].AccountId);
    }

    [Fact]
    public async Task BulkUpsertAsync_UpdatesExistingMapping()
    {
        var (db, tenant) = TestDbHelper.Create();
        var coaSvc = new ChartOfAccountService(db, tenant);
        var acc1 = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("4.1.1", "Receita A", AccountType.Receita, null), default);
        var acc2 = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("4.1.2", "Receita B", AccountType.Receita, null), default);

        var svc = new AccountMappingService(db, tenant);
        await svc.BulkUpsertAsync(new([new("Vendas", acc1.Id)]), default);
        await svc.BulkUpsertAsync(new([new("Vendas", acc2.Id)]), default);

        var list = await svc.ListAsync(default);
        Assert.Single(list);
        Assert.Equal(acc2.Id, list[0].AccountId);
    }
}
```

- [ ] **Step 3: Rodar testes — devem falhar**

```bash
dotnet test backend/tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "AccountMappingServiceTests"
```
Expected: FAIL.

- [ ] **Step 4: Implementar AccountMappingService**

`backend/src/GestorAI.API/Services/Contabilidade/AccountMappingService.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contabilidade;

public class AccountMappingService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<AccountMappingResponse>> ListAsync(CancellationToken ct) =>
        await db.AccountMappings
            .Include(m => m.Account)
            .OrderBy(m => m.CategoryName)
            .Select(m => new AccountMappingResponse(
                m.Id, m.CategoryName, m.AccountId,
                m.Account!.Code, m.Account.Name))
            .ToListAsync(ct);

    public async Task BulkUpsertAsync(BulkUpsertAccountMappingsRequest req, CancellationToken ct)
    {
        var existing = await db.AccountMappings.ToListAsync(ct);
        var existingByCategory = existing.ToDictionary(m => m.CategoryName);

        foreach (var item in req.Mappings)
        {
            if (existingByCategory.TryGetValue(item.CategoryName, out var mapping))
                mapping.AccountId = item.AccountId;
            else
                db.AccountMappings.Add(new AccountMapping
                {
                    CompanyId = tenantContext.CompanyId,
                    CategoryName = item.CategoryName,
                    AccountId = item.AccountId,
                });
        }

        await db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Implementar AccountingSettingsService**

`backend/src/GestorAI.API/Services/Contabilidade/AccountingSettingsService.cs`:
```csharp
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contabilidade;

public class AccountingSettingsService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<AccountingSettingsResponse> GetAsync(CancellationToken ct)
    {
        var settings = await db.CompanySettings.FirstOrDefaultAsync(ct)
            ?? throw new AppException("Configurações da empresa não encontradas.", 404);

        string? code = null, name = null;
        if (settings.DefaultCashAccountId.HasValue)
        {
            var account = await db.ChartOfAccounts
                .FirstOrDefaultAsync(a => a.Id == settings.DefaultCashAccountId.Value, ct);
            code = account?.Code;
            name = account?.Name;
        }

        return new AccountingSettingsResponse(
            settings.DefaultCashAccountId, code, name,
            settings.PreferredAccountingSystem);
    }

    public async Task<AccountingSettingsResponse> UpdateAsync(UpdateAccountingSettingsRequest req, CancellationToken ct)
    {
        var settings = await db.CompanySettings.FirstOrDefaultAsync(ct)
            ?? throw new AppException("Configurações da empresa não encontradas.", 404);

        settings.DefaultCashAccountId = req.DefaultCashAccountId;
        settings.PreferredAccountingSystem = req.PreferredAccountingSystem;
        await db.SaveChangesAsync(ct);
        return await GetAsync(ct);
    }
}
```

- [ ] **Step 6: Rodar testes — devem passar**

```bash
dotnet test backend/tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "AccountMappingServiceTests"
```
Expected: 2/2 PASS.

- [ ] **Step 7: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Contabilidade/ \
        backend/src/GestorAI.API/Services/Contabilidade/AccountMappingService.cs \
        backend/src/GestorAI.API/Services/Contabilidade/AccountingSettingsService.cs \
        backend/tests/GestorAI.Tests/Services/AccountMappingServiceTests.cs
git commit -m "feat: add AccountMappingService and AccountingSettingsService with tests"
```

---

### Task 4: AccountingExportService + DominioExporter + FortesExporter + testes

**Files:**
- Create: `backend/src/GestorAI.API/Services/Contabilidade/AccountingExportService.cs`
- Create: `backend/src/GestorAI.API/DTOs/Contabilidade/AccountingExportDto.cs`
- Create: `backend/tests/GestorAI.Tests/Services/AccountingExportServiceTests.cs`

**Interfaces:**
- Consumes: `Transaction`, `AccountMapping`, `CompanySettings`, `ChartOfAccount`; `TipoLancamento`, `StatusLancamento` enums
- Produces: `AccountingExportService.ExportAsync(request)` returns `(byte[] content, string fileName, string contentType)`

- [ ] **Step 1: Criar DTO de request**

`backend/src/GestorAI.API/DTOs/Contabilidade/AccountingExportDto.cs`:
```csharp
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Contabilidade;

public record AccountingExportRequest(
    AccountingSystem System,
    List<string> Months,  // formato "yyyy-MM", ex: ["2025-01", "2025-02"]
    bool IncludePending);

public record AccountingEntry(
    DateOnly Date,
    string Description,
    string DebitCode,
    string CreditCode,
    decimal Amount);
```

- [ ] **Step 2: Escrever testes**

`backend/tests/GestorAI.Tests/Services/AccountingExportServiceTests.cs`:
```csharp
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Services.Contabilidade;
using GestorAI.Tests.Helpers;

namespace GestorAI.Tests.Services;

public class AccountingExportServiceTests
{
    [Fact]
    public async Task ExportAsync_DominioFormat_ContainsCorrectLines()
    {
        var (db, tenant) = TestDbHelper.Create();

        // Setup chart of accounts
        var coaSvc = new ChartOfAccountService(db, tenant);
        var cashAcc = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("1.1.1.02", "Banco CC", AccountType.Ativo, null), default);
        var recAcc = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("4.1.1", "Receita Vendas", AccountType.Receita, null), default);

        // Setup settings
        var settingsSvc = new AccountingSettingsService(db, tenant);
        await settingsSvc.UpdateAsync(
            new UpdateAccountingSettingsRequest(cashAcc.Id, AccountingSystem.Dominio), default);

        // Setup mapping
        var mappingSvc = new AccountMappingService(db, tenant);
        await mappingSvc.BulkUpsertAsync(
            new BulkUpsertAccountMappingsRequest([new("Vendas", recAcc.Id)]), default);

        // Add paid transaction
        db.Transactions.Add(new Transaction
        {
            CompanyId = tenant.CompanyId,
            Type = TipoLancamento.Receita,
            Description = "Venda balcão",
            Amount = 500m,
            DueDate = new DateTime(2025, 1, 15),
            PaymentDate = new DateTime(2025, 1, 15),
            Status = StatusLancamento.Pago,
            Category = "Vendas",
            Source = TransactionSource.Manual,
        });
        await db.SaveChangesAsync();

        var svc = new AccountingExportService(db, tenant);
        var req = new AccountingExportRequest(AccountingSystem.Dominio, ["2025-01"], false);
        var (content, fileName, contentType) = await svc.ExportAsync(req, default);

        var text = System.Text.Encoding.UTF8.GetString(content);
        Assert.Contains("15/01/2025", text);
        Assert.Contains("1.1.1.02", text);
        Assert.Contains("4.1.1", text);
        Assert.Contains("500,00", text);
        Assert.Equal("text/plain", contentType);
    }

    [Fact]
    public async Task ExportAsync_FortesFormat_HasCsvHeader()
    {
        var (db, tenant) = TestDbHelper.Create();
        var coaSvc = new ChartOfAccountService(db, tenant);
        var cashAcc = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("1.1.1.02", "Banco CC", AccountType.Ativo, null), default);
        var despAcc = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("5.1.1.01", "Aluguel", AccountType.Despesa, null), default);

        var settingsSvc = new AccountingSettingsService(db, tenant);
        await settingsSvc.UpdateAsync(
            new UpdateAccountingSettingsRequest(cashAcc.Id, AccountingSystem.Fortes), default);

        var mappingSvc = new AccountMappingService(db, tenant);
        await mappingSvc.BulkUpsertAsync(
            new BulkUpsertAccountMappingsRequest([new("Aluguel", despAcc.Id)]), default);

        db.Transactions.Add(new Transaction
        {
            CompanyId = tenant.CompanyId,
            Type = TipoLancamento.Despesa,
            Description = "Aluguel janeiro",
            Amount = 2000m,
            DueDate = new DateTime(2025, 1, 5),
            PaymentDate = new DateTime(2025, 1, 5),
            Status = StatusLancamento.Pago,
            Category = "Aluguel",
            Source = TransactionSource.Manual,
        });
        await db.SaveChangesAsync();

        var svc = new AccountingExportService(db, tenant);
        var req = new AccountingExportRequest(AccountingSystem.Fortes, ["2025-01"], false);
        var (content, fileName, contentType) = await svc.ExportAsync(req, default);

        var text = System.Text.Encoding.UTF8.GetString(content);
        var lines = text.Split('\n');
        Assert.Equal("Data;Historico;Debito;Credito;Valor", lines[0].Trim());
        Assert.Contains("5.1.1.01", text);
        Assert.Contains("1.1.1.02", text);
        Assert.Equal("text/csv", contentType);
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenNoCashAccount()
    {
        var (db, tenant) = TestDbHelper.Create();
        var svc = new AccountingExportService(db, tenant);
        var req = new AccountingExportRequest(AccountingSystem.Dominio, ["2025-01"], false);

        await Assert.ThrowsAsync<AppException>(() => svc.ExportAsync(req, default));
    }

    [Fact]
    public async Task ExportAsync_ThrowsWhenCategoryNotMapped()
    {
        var (db, tenant) = TestDbHelper.Create();
        var coaSvc = new ChartOfAccountService(db, tenant);
        var cashAcc = await coaSvc.CreateAsync(
            new CreateChartOfAccountRequest("1.1.1.02", "Banco CC", AccountType.Ativo, null), default);
        var settingsSvc = new AccountingSettingsService(db, tenant);
        await settingsSvc.UpdateAsync(new UpdateAccountingSettingsRequest(cashAcc.Id, null), default);

        db.Transactions.Add(new Transaction
        {
            CompanyId = tenant.CompanyId,
            Type = TipoLancamento.Receita,
            Description = "Venda",
            Amount = 100m,
            DueDate = new DateTime(2025, 1, 1),
            PaymentDate = new DateTime(2025, 1, 1),
            Status = StatusLancamento.Pago,
            Category = "Vendas",
            Source = TransactionSource.Manual,
        });
        await db.SaveChangesAsync();

        var svc = new AccountingExportService(db, tenant);
        var req = new AccountingExportRequest(AccountingSystem.Dominio, ["2025-01"], false);

        var ex = await Assert.ThrowsAsync<AppException>(() => svc.ExportAsync(req, default));
        Assert.Contains("Vendas", ex.Message);
    }
}
```

- [ ] **Step 3: Rodar testes — devem falhar**

```bash
dotnet test backend/tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "AccountingExportServiceTests"
```
Expected: FAIL.

- [ ] **Step 4: Implementar AccountingExportService com exporters**

`backend/src/GestorAI.API/Services/Contabilidade/AccountingExportService.cs`:
```csharp
using System.Text;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contabilidade;

public class AccountingExportService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<(byte[] Content, string FileName, string ContentType)> ExportAsync(
        AccountingExportRequest req, CancellationToken ct)
    {
        // Validate cash account configured
        var settings = await db.CompanySettings.FirstOrDefaultAsync(ct)
            ?? throw new AppException("Configurações da empresa não encontradas.", 404);

        if (settings.DefaultCashAccountId == null)
            throw new AppException("Configure a conta padrão de caixa/banco antes de exportar.", 400);

        var cashAccount = await db.ChartOfAccounts
            .FirstOrDefaultAsync(a => a.Id == settings.DefaultCashAccountId.Value, ct)
            ?? throw new AppException("Conta padrão de caixa não encontrada no Plano de Contas.", 404);

        // Parse months into date ranges
        var periods = req.Months
            .Select(m => DateOnly.ParseExact(m, "yyyy-MM", null))
            .Select(d => (From: d, To: d.AddMonths(1).AddDays(-1)))
            .ToList();

        var fromDate = periods.Min(p => p.From).ToDateTime(TimeOnly.MinValue);
        var toDate = periods.Max(p => p.To).ToDateTime(TimeOnly.MaxValue);

        // Fetch transactions
        var statusFilter = req.IncludePending
            ? new[] { StatusLancamento.Pago, StatusLancamento.Pendente }
            : new[] { StatusLancamento.Pago };

        var transactions = await db.Transactions
            .Where(t => statusFilter.Contains(t.Status)
                     && t.PaymentDate >= fromDate
                     && t.PaymentDate <= toDate)
            .OrderBy(t => t.PaymentDate)
            .ToListAsync(ct);

        // Validate all categories are mapped
        var mappings = await db.AccountMappings
            .Include(m => m.Account)
            .ToListAsync(ct);
        var mappingDict = mappings.ToDictionary(m => m.CategoryName);

        var unmapped = transactions
            .Select(t => t.Category)
            .Distinct()
            .Where(c => !mappingDict.ContainsKey(c))
            .ToList();

        if (unmapped.Count > 0)
            throw new AppException(
                $"As seguintes categorias não têm mapeamento contábil: {string.Join(", ", unmapped)}", 400);

        // Build accounting entries
        var entries = transactions.Select(t =>
        {
            var account = mappingDict[t.Category].Account!;
            var date = DateOnly.FromDateTime(t.PaymentDate!.Value);
            var desc = t.Description.Length > 40 ? t.Description[..40] : t.Description;

            return t.Type == TipoLancamento.Receita
                ? new AccountingEntry(date, desc, cashAccount.Code, account.Code, t.Amount)
                : new AccountingEntry(date, desc, account.Code, cashAccount.Code, t.Amount);
        }).ToList();

        var exporter = req.System == AccountingSystem.Dominio
            ? (IAccountingExporter)new DominioExporter()
            : new FortesExporter();

        var from = periods.Min(p => p.From);
        var to = periods.Max(p => p.To);
        var content = exporter.Export(entries);
        return (content, exporter.FileName(from, to), exporter.ContentType);
    }
}

public interface IAccountingExporter
{
    string ContentType { get; }
    string FileName(DateOnly from, DateOnly to);
    byte[] Export(IEnumerable<AccountingEntry> entries);
}

public class DominioExporter : IAccountingExporter
{
    public string ContentType => "text/plain";
    public string FileName(DateOnly from, DateOnly to) =>
        $"dominio_{from:yyyyMM}_{to:yyyyMM}.txt";

    public byte[] Export(IEnumerable<AccountingEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            var valor = e.Amount.ToString("N2").Replace(".", "").Replace(",", ",");
            sb.AppendLine($"I|{e.Date:dd/MM/yyyy}|{e.Description}|{e.DebitCode}|{e.CreditCode}|{valor}|");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}

public class FortesExporter : IAccountingExporter
{
    public string ContentType => "text/csv";
    public string FileName(DateOnly from, DateOnly to) =>
        $"fortes_{from:yyyyMM}_{to:yyyyMM}.csv";

    public byte[] Export(IEnumerable<AccountingEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Data;Historico;Debito;Credito;Valor");
        foreach (var e in entries)
        {
            var valor = e.Amount.ToString("N2").Replace(".", "").Replace(",", ",");
            sb.AppendLine($"{e.Date:dd/MM/yyyy};{e.Description};{e.DebitCode};{e.CreditCode};{valor}");
        }
        return Encoding.UTF8.GetBytes(sb.ToString());
    }
}
```

- [ ] **Step 5: Rodar testes — devem passar**

```bash
dotnet test backend/tests/GestorAI.Tests/GestorAI.Tests.csproj --filter "AccountingExportServiceTests"
```
Expected: 4/4 PASS.

- [ ] **Step 6: Commit**

```bash
git add backend/src/GestorAI.API/DTOs/Contabilidade/AccountingExportDto.cs \
        backend/src/GestorAI.API/Services/Contabilidade/AccountingExportService.cs \
        backend/tests/GestorAI.Tests/Services/AccountingExportServiceTests.cs
git commit -m "feat: add AccountingExportService with Dominio and Fortes exporters and tests"
```

---

### Task 5: Endpoints + wiring em Program.cs

**Files:**
- Create: `backend/src/GestorAI.API/Endpoints/ContabilidadeEndpoints.cs`
- Modify: `backend/src/GestorAI.API/Program.cs`

**Interfaces:**
- Consumes: `ChartOfAccountService`, `AccountMappingService`, `AccountingSettingsService`, `AccountingExportService`
- Produces: 10 endpoints REST sob `/api`

- [ ] **Step 1: Criar ContabilidadeEndpoints.cs**

`backend/src/GestorAI.API/Endpoints/ContabilidadeEndpoints.cs`:
```csharp
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Services.Contabilidade;

namespace GestorAI.API.Endpoints;

public static class ContabilidadeEndpoints
{
    public static void MapContabilidade(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        // Chart of Accounts
        group.MapGet("/chart-of-accounts", async (
            ChartOfAccountService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPost("/chart-of-accounts", async (
            CreateChartOfAccountRequest req, ChartOfAccountService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/chart-of-accounts/{result.Id}", result);
        });

        group.MapPut("/chart-of-accounts/{id:guid}", async (
            Guid id, UpdateChartOfAccountRequest req, ChartOfAccountService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapDelete("/chart-of-accounts/{id:guid}", async (
            Guid id, ChartOfAccountService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/chart-of-accounts/load-template", async (
            ChartOfAccountService svc, CancellationToken ct) =>
        {
            await svc.LoadTemplateAsync(ct);
            return Results.Ok();
        });

        // Account Mappings
        group.MapGet("/account-mappings", async (
            AccountMappingService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPut("/account-mappings", async (
            BulkUpsertAccountMappingsRequest req, AccountMappingService svc, CancellationToken ct) =>
        {
            await svc.BulkUpsertAsync(req, ct);
            return Results.Ok();
        });

        // Accounting Settings
        group.MapGet("/accounting-settings", async (
            AccountingSettingsService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(ct)));

        group.MapPut("/accounting-settings", async (
            UpdateAccountingSettingsRequest req, AccountingSettingsService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(req, ct)));

        // Export
        group.MapPost("/accounting-export/download", async (
            AccountingExportRequest req, AccountingExportService svc, CancellationToken ct) =>
        {
            var (content, fileName, contentType) = await svc.ExportAsync(req, ct);
            return Results.File(content, contentType, fileName);
        });
    }
}
```

- [ ] **Step 2: Wiring em Program.cs**

Adicionar após os outros `AddScoped` (linha com `BankReconciliationService`):
```csharp
builder.Services.AddScoped<ChartOfAccountService>();
builder.Services.AddScoped<AccountMappingService>();
builder.Services.AddScoped<AccountingSettingsService>();
builder.Services.AddScoped<AccountingExportService>();
```

Adicionar após `app.MapConciliacao()`:
```csharp
app.MapContabilidade();
```

- [ ] **Step 3: Build**

```bash
dotnet build backend/src/GestorAI.API
```
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add backend/src/GestorAI.API/Endpoints/ContabilidadeEndpoints.cs \
        backend/src/GestorAI.API/Program.cs
git commit -m "feat: add contabilidade endpoints and wire up services"
```

---

### Task 6: Frontend — tipos e hook

**Files:**
- Create: `frontend/src/types/contabilidade.ts`
- Create: `frontend/src/hooks/useContabilidade.ts`

**Interfaces:**
- Consumes: `api.get`, `api.post`, `api.put`, `api.delete` from `@/services/api`
- Produces: 10 funções exportadas via `useContabilidade()`

- [ ] **Step 1: Criar tipos**

`frontend/src/types/contabilidade.ts`:
```typescript
export type AccountType = 'Ativo' | 'Passivo' | 'PatrimonioLiquido' | 'Receita' | 'Despesa'
export type AccountingSystem = 'Dominio' | 'Fortes'

export interface ChartOfAccountResponse {
  id: string
  code: string
  name: string
  type: AccountType
  parentId: string | null
  isActive: boolean
  children: ChartOfAccountResponse[]
}

export interface CreateChartOfAccountRequest {
  code: string
  name: string
  type: AccountType
  parentId: string | null
}

export interface UpdateChartOfAccountRequest {
  code: string
  name: string
}

export interface AccountMappingResponse {
  id: string
  categoryName: string
  accountId: string
  accountCode: string
  accountName: string
}

export interface AccountMappingItem {
  categoryName: string
  accountId: string
}

export interface AccountingSettingsResponse {
  defaultCashAccountId: string | null
  defaultCashAccountCode: string | null
  defaultCashAccountName: string | null
  preferredAccountingSystem: AccountingSystem | null
}

export interface UpdateAccountingSettingsRequest {
  defaultCashAccountId: string | null
  preferredAccountingSystem: AccountingSystem | null
}

export interface AccountingExportRequest {
  system: AccountingSystem
  months: string[]       // ["2025-01", "2025-02"]
  includePending: boolean
}
```

- [ ] **Step 2: Criar hook**

`frontend/src/hooks/useContabilidade.ts`:
```typescript
import { useCallback } from 'react'
import { api } from '@/services/api'
import type {
  ChartOfAccountResponse,
  CreateChartOfAccountRequest,
  UpdateChartOfAccountRequest,
  AccountMappingResponse,
  AccountMappingItem,
  AccountingSettingsResponse,
  UpdateAccountingSettingsRequest,
  AccountingExportRequest,
} from '@/types/contabilidade'

export function useContabilidade() {
  const listAccounts = useCallback(() =>
    api.get<ChartOfAccountResponse[]>('/api/chart-of-accounts'), [])

  const createAccount = useCallback((req: CreateChartOfAccountRequest) =>
    api.post<ChartOfAccountResponse>('/api/chart-of-accounts', req), [])

  const updateAccount = useCallback((id: string, req: UpdateChartOfAccountRequest) =>
    api.put<ChartOfAccountResponse>(`/api/chart-of-accounts/${id}`, req), [])

  const deleteAccount = useCallback((id: string) =>
    api.delete(`/api/chart-of-accounts/${id}`), [])

  const loadTemplate = useCallback(() =>
    api.post('/api/chart-of-accounts/load-template', {}), [])

  const listMappings = useCallback(() =>
    api.get<AccountMappingResponse[]>('/api/account-mappings'), [])

  const saveMappings = useCallback((mappings: AccountMappingItem[]) =>
    api.put('/api/account-mappings', { mappings }), [])

  const getSettings = useCallback(() =>
    api.get<AccountingSettingsResponse>('/api/accounting-settings'), [])

  const updateSettings = useCallback((req: UpdateAccountingSettingsRequest) =>
    api.put<AccountingSettingsResponse>('/api/accounting-settings', req), [])

  const downloadExport = useCallback(async (req: AccountingExportRequest) => {
    const form = new FormData()
    // Use regular post and handle blob download
    const token = localStorage.getItem('ga_token')
    const apiBase = import.meta.env.VITE_API_URL ?? 'http://localhost:5002'
    const res = await fetch(`${apiBase}/api/accounting-export/download`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
      },
      body: JSON.stringify(req),
    })
    if (!res.ok) {
      const text = await res.text().catch(() => '')
      throw new Error(text || `Erro ${res.status}`)
    }
    const blob = await res.blob()
    const disposition = res.headers.get('Content-Disposition') ?? ''
    const match = disposition.match(/filename="?([^"]+)"?/)
    const fileName = match?.[1] ?? 'export.txt'
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = fileName
    a.click()
    URL.revokeObjectURL(url)
  }, [])

  return {
    listAccounts, createAccount, updateAccount, deleteAccount, loadTemplate,
    listMappings, saveMappings,
    getSettings, updateSettings,
    downloadExport,
  }
}
```

- [ ] **Step 3: Build frontend**

```bash
cd frontend && npm run build
```
Expected: 0 errors de TypeScript.

- [ ] **Step 4: Commit**

```bash
git add frontend/src/types/contabilidade.ts frontend/src/hooks/useContabilidade.ts
git commit -m "feat: add contabilidade types and hook"
```

---

### Task 7: Páginas Plano de Contas + Mapeamento + navegação

**Files:**
- Create: `frontend/src/pages/configuracoes/PlanoDeContas.tsx`
- Create: `frontend/src/pages/configuracoes/MapeamentoContabil.tsx`
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/TopNav.tsx`

**Interfaces:**
- Consumes: `useContabilidade()`, shadcn `Button`, `Input`, `Label`, `Select`, `Badge`, `toast`
- Produces: páginas em `/configuracoes/contabilidade/plano-de-contas` e `/configuracoes/contabilidade/mapeamento`

- [ ] **Step 1: Criar PlanoDeContas.tsx**

`frontend/src/pages/configuracoes/PlanoDeContas.tsx`:
```tsx
import { useEffect, useState } from 'react'
import { ChevronDown, ChevronRight, Plus, Pencil, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import { useContabilidade } from '@/hooks/useContabilidade'
import type { ChartOfAccountResponse, AccountType } from '@/types/contabilidade'

const ACCOUNT_TYPE_LABELS: Record<AccountType, string> = {
  Ativo: 'Ativo', Passivo: 'Passivo', PatrimonioLiquido: 'Patrimônio Líquido',
  Receita: 'Receita', Despesa: 'Despesa',
}

function AccountRow({
  account,
  depth,
  onEdit,
  onDelete,
  onAddChild,
}: {
  account: ChartOfAccountResponse
  depth: number
  onEdit: (a: ChartOfAccountResponse) => void
  onDelete: (id: string) => void
  onAddChild: (parentId: string, type: AccountType) => void
}) {
  const [open, setOpen] = useState(depth < 2)
  const hasChildren = account.children.length > 0

  return (
    <>
      <tr className={account.isActive ? '' : 'opacity-40'}>
        <td className="py-1 px-2" style={{ paddingLeft: `${(depth + 1) * 16}px` }}>
          <div className="flex items-center gap-1">
            {hasChildren
              ? <button onClick={() => setOpen(o => !o)}>
                  {open ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
                </button>
              : <span className="w-4" />}
            <span className="font-mono text-sm text-muted-foreground">{account.code}</span>
            <span className="text-sm ml-2">{account.name}</span>
          </div>
        </td>
        <td className="py-1 px-2 text-xs text-muted-foreground">
          {ACCOUNT_TYPE_LABELS[account.type]}
        </td>
        <td className="py-1 px-2">
          <div className="flex gap-1">
            <Button size="icon" variant="ghost" className="h-6 w-6"
              onClick={() => onEdit(account)}>
              <Pencil size={12} />
            </Button>
            <Button size="icon" variant="ghost" className="h-6 w-6"
              onClick={() => onAddChild(account.id, account.type)}>
              <Plus size={12} />
            </Button>
            <Button size="icon" variant="ghost" className="h-6 w-6 text-destructive"
              onClick={() => onDelete(account.id)}>
              <Trash2 size={12} />
            </Button>
          </div>
        </td>
      </tr>
      {open && account.children.map(child => (
        <AccountRow key={child.id} account={child} depth={depth + 1}
          onEdit={onEdit} onDelete={onDelete} onAddChild={onAddChild} />
      ))}
    </>
  )
}

export default function PlanoDeContas() {
  const { listAccounts, createAccount, updateAccount, deleteAccount, loadTemplate } = useContabilidade()
  const [accounts, setAccounts] = useState<ChartOfAccountResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [editing, setEditing] = useState<ChartOfAccountResponse | null>(null)
  const [adding, setAdding] = useState<{ parentId: string | null; type: AccountType } | null>(null)
  const [form, setForm] = useState({ code: '', name: '' })

  const load = () => {
    setLoading(true)
    listAccounts()
      .then(setAccounts)
      .catch(() => toast({ title: 'Erro ao carregar plano de contas', variant: 'destructive' }))
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  const handleLoadTemplate = async () => {
    try {
      await loadTemplate()
      load()
      toast({ title: 'Modelo padrão carregado com sucesso' })
    } catch (e: any) {
      toast({ title: e.message, variant: 'destructive' })
    }
  }

  const handleSave = async () => {
    try {
      if (editing) {
        await updateAccount(editing.id, { code: form.code, name: form.name })
        toast({ title: 'Conta atualizada' })
      } else if (adding) {
        await createAccount({
          code: form.code, name: form.name,
          type: adding.type, parentId: adding.parentId,
        })
        toast({ title: 'Conta criada' })
      }
      setEditing(null)
      setAdding(null)
      setForm({ code: '', name: '' })
      load()
    } catch (e: any) {
      toast({ title: e.message, variant: 'destructive' })
    }
  }

  const handleDelete = async (id: string) => {
    try {
      await deleteAccount(id)
      toast({ title: 'Conta desativada' })
      load()
    } catch (e: any) {
      toast({ title: e.message, variant: 'destructive' })
    }
  }

  const isEmpty = accounts.length === 0

  return (
    <div className="p-6 max-w-3xl mx-auto">
      <div className="flex items-center justify-between mb-4">
        <h1 className="text-2xl font-bold">Plano de Contas</h1>
        <div className="flex gap-2">
          {isEmpty && (
            <Button variant="outline" onClick={handleLoadTemplate}>
              Carregar Modelo Padrão
            </Button>
          )}
          <Button onClick={() => { setAdding({ parentId: null, type: 'Ativo' }); setForm({ code: '', name: '' }) }}>
            <Plus size={16} className="mr-1" /> Nova Conta
          </Button>
        </div>
      </div>

      {(editing || adding) && (
        <div className="border rounded-lg p-4 mb-4 bg-muted/30 flex gap-4 items-end">
          <div className="flex-1">
            <Label>Código</Label>
            <Input value={form.code} onChange={e => setForm(f => ({ ...f, code: e.target.value }))}
              placeholder="ex: 4.1.1" className="mt-1" />
          </div>
          <div className="flex-1">
            <Label>Nome</Label>
            <Input value={form.name} onChange={e => setForm(f => ({ ...f, name: e.target.value }))}
              placeholder="ex: Receita de Vendas" className="mt-1" />
          </div>
          <div className="flex gap-2">
            <Button onClick={handleSave}>Salvar</Button>
            <Button variant="outline" onClick={() => { setEditing(null); setAdding(null) }}>
              Cancelar
            </Button>
          </div>
        </div>
      )}

      {loading ? (
        <p className="text-muted-foreground">Carregando...</p>
      ) : isEmpty ? (
        <p className="text-muted-foreground text-center py-12">
          Nenhuma conta cadastrada. Clique em "Carregar Modelo Padrão" para começar.
        </p>
      ) : (
        <table className="w-full">
          <thead>
            <tr className="text-xs text-muted-foreground border-b">
              <th className="py-2 px-2 text-left">Código / Nome</th>
              <th className="py-2 px-2 text-left">Tipo</th>
              <th className="py-2 px-2" />
            </tr>
          </thead>
          <tbody>
            {accounts.map(a => (
              <AccountRow key={a.id} account={a} depth={0}
                onEdit={a => { setEditing(a); setForm({ code: a.code, name: a.name }) }}
                onDelete={handleDelete}
                onAddChild={(parentId, type) => { setAdding({ parentId, type }); setForm({ code: '', name: '' }) }}
              />
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
```

- [ ] **Step 2: Criar MapeamentoContabil.tsx**

`frontend/src/pages/configuracoes/MapeamentoContabil.tsx`:
```tsx
import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Badge } from '@/components/ui/badge'
import { toast } from '@/hooks/useToast'
import { useContabilidade } from '@/hooks/useContabilidade'
import type { ChartOfAccountResponse, AccountMappingResponse, AccountingSystem } from '@/types/contabilidade'

function flattenAccounts(list: ChartOfAccountResponse[]): ChartOfAccountResponse[] {
  return list.flatMap(a => [a, ...flattenAccounts(a.children)]).filter(a => a.isActive)
}

export default function MapeamentoContabil() {
  const { listAccounts, listMappings, saveMappings, getSettings, updateSettings } = useContabilidade()
  const [accounts, setAccounts] = useState<ChartOfAccountResponse[]>([])
  const [mappings, setMappings] = useState<AccountMappingResponse[]>([])
  const [draft, setDraft] = useState<Record<string, string>>({})
  const [cashAccountId, setCashAccountId] = useState<string>('')
  const [preferredSystem, setPreferredSystem] = useState<AccountingSystem | ''>('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    Promise.all([listAccounts(), listMappings(), getSettings()])
      .then(([accs, maps, settings]) => {
        setAccounts(accs)
        setMappings(maps)
        const d: Record<string, string> = {}
        maps.forEach(m => { d[m.categoryName] = m.accountId })
        setDraft(d)
        setCashAccountId(settings.defaultCashAccountId ?? '')
        setPreferredSystem(settings.preferredAccountingSystem ?? '')
      })
      .catch(() => toast({ title: 'Erro ao carregar dados', variant: 'destructive' }))
      .finally(() => setLoading(false))
  }, [])

  const flatAccounts = flattenAccounts(accounts)
  const categories = Array.from(new Set(mappings.map(m => m.categoryName)))

  const handleSave = async () => {
    try {
      const mappingItems = Object.entries(draft)
        .filter(([, accountId]) => accountId)
        .map(([categoryName, accountId]) => ({ categoryName, accountId }))
      await saveMappings(mappingItems)
      await updateSettings({
        defaultCashAccountId: cashAccountId || null,
        preferredAccountingSystem: (preferredSystem as AccountingSystem) || null,
      })
      toast({ title: 'Mapeamento salvo com sucesso' })
    } catch (e: any) {
      toast({ title: e.message, variant: 'destructive' })
    }
  }

  if (loading) return <div className="p-6">Carregando...</div>

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Mapeamento Contábil</h1>
        <Button onClick={handleSave}>Salvar Mapeamento</Button>
      </div>

      <div className="border rounded-lg p-4 mb-6 space-y-4">
        <h2 className="font-semibold">Configurações Gerais</h2>
        <div>
          <Label>Conta Padrão de Caixa/Banco</Label>
          <select
            value={cashAccountId}
            onChange={e => setCashAccountId(e.target.value)}
            className="mt-1 w-full border rounded px-3 py-2 text-sm bg-background"
          >
            <option value="">Selecione...</option>
            {flatAccounts.map(a => (
              <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
            ))}
          </select>
        </div>
        <div>
          <Label>Sistema Contábil Preferido</Label>
          <select
            value={preferredSystem}
            onChange={e => setPreferredSystem(e.target.value as AccountingSystem | '')}
            className="mt-1 w-full border rounded px-3 py-2 text-sm bg-background"
          >
            <option value="">Nenhum</option>
            <option value="Dominio">Domínio (Thomson Reuters)</option>
            <option value="Fortes">Fortes (Fortes Tecnologia)</option>
          </select>
        </div>
      </div>

      <div className="border rounded-lg overflow-hidden">
        <table className="w-full">
          <thead className="bg-muted/50">
            <tr className="text-xs text-muted-foreground">
              <th className="py-2 px-4 text-left">Categoria</th>
              <th className="py-2 px-4 text-left">Conta Contábil</th>
            </tr>
          </thead>
          <tbody>
            {categories.length === 0 ? (
              <tr>
                <td colSpan={2} className="py-8 text-center text-muted-foreground text-sm">
                  Nenhuma categoria cadastrada
                </td>
              </tr>
            ) : categories.map(cat => (
              <tr key={cat} className="border-t">
                <td className="py-2 px-4 text-sm">
                  <div className="flex items-center gap-2">
                    {cat}
                    {!draft[cat] && (
                      <Badge variant="outline" className="text-yellow-600 border-yellow-400 text-xs">
                        Sem mapeamento
                      </Badge>
                    )}
                  </div>
                </td>
                <td className="py-2 px-4">
                  <select
                    value={draft[cat] ?? ''}
                    onChange={e => setDraft(d => ({ ...d, [cat]: e.target.value }))}
                    className="w-full border rounded px-2 py-1 text-sm bg-background"
                  >
                    <option value="">Selecione...</option>
                    {flatAccounts.map(a => (
                      <option key={a.id} value={a.id}>{a.code} — {a.name}</option>
                    ))}
                  </select>
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

- [ ] **Step 3: Adicionar rotas em router/index.tsx**

Adicionar imports:
```tsx
import PlanoDeContas from '@/pages/configuracoes/PlanoDeContas'
import MapeamentoContabil from '@/pages/configuracoes/MapeamentoContabil'
```

Adicionar rotas junto com as outras rotas de configurações:
```tsx
{ path: '/configuracoes/contabilidade/plano-de-contas', element: <PlanoDeContas /> },
{ path: '/configuracoes/contabilidade/mapeamento', element: <MapeamentoContabil /> },
```

- [ ] **Step 4: Adicionar items no TopNav**

Em `frontend/src/components/layout/TopNav.tsx`, no grupo de Configurações (`moduleSlug: 'configuracoes'`), adicionar após o item de Integrações:
```tsx
{ icon: BookOpen, label: 'Plano de Contas', path: '/configuracoes/contabilidade/plano-de-contas' },
{ icon: ArrowLeftRight, label: 'Mapeamento Contábil', path: '/configuracoes/contabilidade/mapeamento' },
```

Adicionar imports dos ícones no topo do arquivo (junto com os outros ícones lucide):
```tsx
import { BookOpen, ArrowLeftRight } from 'lucide-react'
```

- [ ] **Step 5: Build frontend**

```bash
cd frontend && npm run build
```
Expected: 0 erros TypeScript.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/pages/configuracoes/PlanoDeContas.tsx \
        frontend/src/pages/configuracoes/MapeamentoContabil.tsx \
        frontend/src/router/index.tsx \
        frontend/src/components/layout/TopNav.tsx
git commit -m "feat: add PlanoDeContas and MapeamentoContabil pages, routes and nav"
```

---

### Task 8: Página Exportar para Contador

**Files:**
- Create: `frontend/src/pages/financeiro/ExportarContador.tsx`
- Modify: `frontend/src/router/index.tsx`
- Modify: `frontend/src/components/layout/TopNav.tsx`

**Interfaces:**
- Consumes: `useContabilidade()`, `AccountingSystem`, shadcn components, `toast`
- Produces: página em `/financeiro/exportar-contador`

- [ ] **Step 1: Criar ExportarContador.tsx**

`frontend/src/pages/financeiro/ExportarContador.tsx`:
```tsx
import { useState } from 'react'
import { FileDown, AlertCircle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { toast } from '@/hooks/useToast'
import { useContabilidade } from '@/hooks/useContabilidade'
import type { AccountingSystem } from '@/types/contabilidade'

const MONTHS = [
  'Jan', 'Fev', 'Mar', 'Abr', 'Mai', 'Jun',
  'Jul', 'Ago', 'Set', 'Out', 'Nov', 'Dez',
]

export default function ExportarContador() {
  const { downloadExport } = useContabilidade()
  const currentYear = new Date().getFullYear()
  const [year, setYear] = useState(currentYear)
  const [selectedMonths, setSelectedMonths] = useState<number[]>([])
  const [system, setSystem] = useState<AccountingSystem>('Dominio')
  const [includePending, setIncludePending] = useState(false)
  const [loading, setLoading] = useState(false)
  const [validationError, setValidationError] = useState<string | null>(null)

  const toggleMonth = (m: number) =>
    setSelectedMonths(prev =>
      prev.includes(m) ? prev.filter(x => x !== m) : [...prev, m].sort((a, b) => a - b)
    )

  const handleExport = async () => {
    if (selectedMonths.length === 0) {
      toast({ title: 'Selecione pelo menos um mês', variant: 'destructive' })
      return
    }
    setValidationError(null)
    setLoading(true)
    try {
      const months = selectedMonths.map(m =>
        `${year}-${String(m).padStart(2, '0')}`
      )
      await downloadExport({ system, months, includePending })
      toast({ title: 'Arquivo gerado com sucesso' })
    } catch (e: any) {
      setValidationError(e.message)
    } finally {
      setLoading(false)
    }
  }

  const years = [currentYear - 1, currentYear, currentYear + 1]

  return (
    <div className="p-6 max-w-2xl mx-auto">
      <div className="flex items-center gap-3 mb-6">
        <FileDown size={24} />
        <h1 className="text-2xl font-bold">Exportar para Contador</h1>
      </div>

      <div className="space-y-6">
        <div>
          <Label className="text-base font-semibold">Sistema Contábil</Label>
          <div className="flex gap-4 mt-2">
            {(['Dominio', 'Fortes'] as AccountingSystem[]).map(s => (
              <label key={s} className="flex items-center gap-2 cursor-pointer">
                <input
                  type="radio"
                  name="system"
                  checked={system === s}
                  onChange={() => setSystem(s)}
                  className="accent-primary"
                />
                <span className="text-sm">
                  {s === 'Dominio' ? 'Domínio (Thomson Reuters)' : 'Fortes (Fortes Tecnologia)'}
                </span>
              </label>
            ))}
          </div>
        </div>

        <div>
          <div className="flex items-center justify-between mb-2">
            <Label className="text-base font-semibold">Período</Label>
            <select
              value={year}
              onChange={e => setYear(Number(e.target.value))}
              className="border rounded px-2 py-1 text-sm bg-background"
            >
              {years.map(y => <option key={y} value={y}>{y}</option>)}
            </select>
          </div>
          <div className="grid grid-cols-6 gap-2">
            {MONTHS.map((label, i) => {
              const m = i + 1
              const selected = selectedMonths.includes(m)
              return (
                <button
                  key={m}
                  onClick={() => toggleMonth(m)}
                  className={`py-2 rounded text-sm font-medium border transition-colors ${
                    selected
                      ? 'bg-primary text-primary-foreground border-primary'
                      : 'bg-background border-border hover:bg-muted'
                  }`}
                >
                  {label}
                </button>
              )
            })}
          </div>
          {selectedMonths.length > 0 && (
            <p className="text-xs text-muted-foreground mt-1">
              {selectedMonths.length} {selectedMonths.length === 1 ? 'mês selecionado' : 'meses selecionados'}
            </p>
          )}
        </div>

        <div className="flex items-center gap-2">
          <input
            type="checkbox"
            id="includePending"
            checked={includePending}
            onChange={e => setIncludePending(e.target.checked)}
            className="accent-primary"
          />
          <Label htmlFor="includePending" className="cursor-pointer font-normal">
            Incluir lançamentos pendentes
          </Label>
        </div>

        {validationError && (
          <div className="flex items-start gap-2 p-3 rounded border border-yellow-400 bg-yellow-50 text-yellow-800">
            <AlertCircle size={16} className="mt-0.5 shrink-0" />
            <p className="text-sm">{validationError}</p>
          </div>
        )}

        <Button onClick={handleExport} disabled={loading} className="w-full">
          <FileDown size={16} className="mr-2" />
          {loading ? 'Gerando arquivo...' : 'Exportar'}
        </Button>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Adicionar rota em router/index.tsx**

Adicionar import:
```tsx
import ExportarContador from '@/pages/financeiro/ExportarContador'
```

Adicionar rota junto com as outras rotas do módulo financeiro:
```tsx
{ path: '/financeiro/exportar-contador', element: <ExportarContador /> },
```

- [ ] **Step 3: Adicionar item no TopNav**

No grupo de Financeiro (`moduleSlug: 'financeiro'`), adicionar item ao final da lista:
```tsx
{ icon: FileDown, label: 'Exportar Contador', path: '/financeiro/exportar-contador' },
```

Adicionar import do ícone (se ainda não importado):
```tsx
import { FileDown } from 'lucide-react'
```

- [ ] **Step 4: Build final**

```bash
cd frontend && npm run build
```
Expected: 0 erros TypeScript.

- [ ] **Step 5: Rodar todos os testes backend**

```bash
dotnet test backend/tests/GestorAI.Tests/GestorAI.Tests.csproj
```
Expected: todos os testes pré-existentes passando + novos testes das Tasks 2, 3 e 4.

- [ ] **Step 6: Commit**

```bash
git add frontend/src/pages/financeiro/ExportarContador.tsx \
        frontend/src/router/index.tsx \
        frontend/src/components/layout/TopNav.tsx
git commit -m "feat: add ExportarContador page, route and nav item"
```
