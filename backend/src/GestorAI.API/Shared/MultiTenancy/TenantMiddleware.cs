namespace GestorAI.API.Shared.MultiTenancy;

public class TenantMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext)
    {
        var claim = context.User.FindFirst("empresa_id")?.Value;
        if (Guid.TryParse(claim, out var id))
            tenantContext.EmpresaId = id;
        await next(context);
    }
}
