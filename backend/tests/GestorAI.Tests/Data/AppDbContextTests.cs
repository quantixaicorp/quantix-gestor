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
            new TenantContext { EmpresaId = empresaId });

    [Fact]
    public async Task Clientes_QueryFilter_ExcludesOtherTenantData()
    {
        var empresaId = Guid.NewGuid();
        var outroId = Guid.NewGuid();
        using var ctx = CreateContext(empresaId);

        ctx.Clientes.Add(new Cliente { EmpresaId = empresaId, Nome = "Ana", Whatsapp = "11999990001" });
        ctx.Clientes.Add(new Cliente { EmpresaId = outroId, Nome = "Bob", Whatsapp = "11999990002" });
        await ctx.SaveChangesAsync();

        var result = await ctx.Clientes.ToListAsync();

        Assert.Single(result);
        Assert.Equal("Ana", result[0].Nome);
    }

    [Fact]
    public async Task Produtos_QueryFilter_ReturnsOnlyCurrentTenant()
    {
        var empresaId = Guid.NewGuid();
        var outroId = Guid.NewGuid();
        using var ctx = CreateContext(empresaId);

        ctx.Categorias.Add(new Categoria { EmpresaId = empresaId, Nome = "Cat A" });
        ctx.Categorias.Add(new Categoria { EmpresaId = outroId, Nome = "Cat B" });
        await ctx.SaveChangesAsync();

        var result = await ctx.Categorias.ToListAsync();

        Assert.Single(result);
        Assert.Equal("Cat A", result[0].Nome);
    }
}
