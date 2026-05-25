using System.Security.Claims;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.AspNetCore.Http;

namespace GestorAI.Tests.MultiTenancy;

public class TenantMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithValidClaim_SetsTenantContext()
    {
        var empresaId = Guid.NewGuid();
        var tenantContext = new TenantContext();
        var middleware = new TenantMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("empresa_id", empresaId.ToString())]));

        await middleware.InvokeAsync(context, tenantContext);

        Assert.Equal(empresaId, tenantContext.EmpresaId);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingClaim_LeavesEmpresaIdEmpty()
    {
        var tenantContext = new TenantContext();
        var middleware = new TenantMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity());

        await middleware.InvokeAsync(context, tenantContext);

        Assert.Equal(Guid.Empty, tenantContext.EmpresaId);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidGuid_LeavesEmpresaIdEmpty()
    {
        var tenantContext = new TenantContext();
        var middleware = new TenantMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("empresa_id", "nao-e-guid")]));

        await middleware.InvokeAsync(context, tenantContext);

        Assert.Equal(Guid.Empty, tenantContext.EmpresaId);
    }
}
