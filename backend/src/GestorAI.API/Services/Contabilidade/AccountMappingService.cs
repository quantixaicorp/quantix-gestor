using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contabilidade;

public class AccountMappingService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<AccountMappingResponse>> ListAsync(CancellationToken ct)
    {
        // Registered categories + any ad-hoc categories used directly on transactions (e.g. "Importado" from bank import)
        var registered = await db.TransactionCategories
            .Select(c => c.Name)
            .ToListAsync(ct);

        var fromTransactions = await db.Transactions
            .Where(t => !string.IsNullOrEmpty(t.Category))
            .Select(t => t.Category!)
            .Distinct()
            .ToListAsync(ct);

        var allCategories = registered.Union(fromTransactions).OrderBy(c => c).ToList();

        var mappings = await db.AccountMappings
            .Include(m => m.Account)
            .ToListAsync(ct);

        var mappingDict = mappings.ToDictionary(m => m.CategoryName);

        return allCategories.Select(name =>
        {
            mappingDict.TryGetValue(name, out var mapping);
            return new AccountMappingResponse(
                mapping?.Id ?? Guid.Empty,
                name,
                mapping?.AccountId ?? Guid.Empty,
                mapping?.Account?.Code ?? "",
                mapping?.Account?.Name ?? "");
        }).ToList();
    }

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
