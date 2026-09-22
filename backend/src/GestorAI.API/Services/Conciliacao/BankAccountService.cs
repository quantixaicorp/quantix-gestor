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
        var account = await db.BankAccounts.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Conta bancária não encontrada.", 404);
        account.IsActive = false;
        await db.SaveChangesAsync(ct);
    }

    private static BankAccountResponse ToResponse(BankAccount a) =>
        new(a.Id, a.Name, a.BankName, a.AccountNumber, a.Agency, a.IsActive);
}
