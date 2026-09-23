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
