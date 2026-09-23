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
