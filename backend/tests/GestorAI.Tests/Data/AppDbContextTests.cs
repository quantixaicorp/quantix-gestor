using GestorAI.API.Domain.Entities;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.Tests.Data;

public class AppDbContextTests
{
    private static AppDbContext CreateContext(Guid empresaId) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options,
            new TenantContext { CompanyId = empresaId });

    [Fact]
    public async Task Clientes_QueryFilter_ExcludesOtherTenantData()
    {
        var empresaId = Guid.NewGuid();
        var outroId = Guid.NewGuid();
        using var ctx = CreateContext(empresaId);

        ctx.Customers.Add(new Customer { CompanyId = empresaId, Name = "Ana", WhatsApp = "11999990001" });
        ctx.Customers.Add(new Customer { CompanyId = outroId, Name = "Bob", WhatsApp = "11999990002" });
        await ctx.SaveChangesAsync();

        var result = await ctx.Customers.ToListAsync();

        Assert.Single(result);
        Assert.Equal("Ana", result[0].Name);
    }

    [Fact]
    public async Task Produtos_QueryFilter_ReturnsOnlyCurrentTenant()
    {
        var empresaId = Guid.NewGuid();
        var outroId = Guid.NewGuid();
        using var ctx = CreateContext(empresaId);

        ctx.Categories.Add(new Category { CompanyId = empresaId, Name = "Cat A" });
        ctx.Categories.Add(new Category { CompanyId = outroId, Name = "Cat B" });
        await ctx.SaveChangesAsync();

        var result = await ctx.Categories.ToListAsync();

        Assert.Single(result);
        Assert.Equal("Cat A", result[0].Name);
    }
}
