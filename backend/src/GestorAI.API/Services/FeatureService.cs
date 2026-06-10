using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GestorAI.API.Services;

public class FeatureService(AppDbContext db, TenantContext tenantContext)
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public async Task<bool> HasFeatureAsync(string feature, CancellationToken ct = default)
    {
        var planoAtivo = await db.EmpresasPlano
            .Include(ep => ep.Plano)
            .Where(ep => ep.EmpresaId == tenantContext.EmpresaId && ep.Ativo)
            .OrderByDescending(ep => ep.InicioEm)
            .FirstOrDefaultAsync(ct);

        if (planoAtivo?.Plano is null)
            return true; // sem plano configurado = sem restrição (modo trial/dev)

        var features = JsonSerializer.Deserialize<List<string>>(
            planoAtivo.Plano.Features, _json) ?? [];

        return features.Contains(feature);
    }

    public async Task RequireFeatureAsync(string feature, CancellationToken ct = default)
    {
        if (!await HasFeatureAsync(feature, ct))
            throw new AppException("Esta funcionalidade não está disponível no seu plano atual.", 402);
    }
}
