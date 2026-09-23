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
