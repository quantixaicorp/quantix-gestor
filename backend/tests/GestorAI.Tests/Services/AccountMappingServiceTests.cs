using GestorAI.API.Domain.Entities;
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
        db.TransactionCategories.Add(new TransactionCategory
        {
            CompanyId = tenant.CompanyId,
            Name = "Vendas",
            Type = TipoLancamento.Receita,
        });
        await db.SaveChangesAsync();

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
        db.TransactionCategories.Add(new TransactionCategory
        {
            CompanyId = tenant.CompanyId,
            Name = "Vendas",
            Type = TipoLancamento.Receita,
        });
        await db.SaveChangesAsync();

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
