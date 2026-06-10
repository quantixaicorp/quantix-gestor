using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services;

public class TenantResolutionService(AppDbContext db)
{
    public async Task<Guid?> ResolveByDomainAsync(string host, CancellationToken ct)
    {
        var dominio = host.Split(':')[0].ToLowerInvariant();
        var slug = dominio.Replace(".gestorai.com.br", "");

        var config = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c =>
                c.DominioCustomizado == dominio || c.Slug == slug, ct);

        return config?.EmpresaId;
    }
}
