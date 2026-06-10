using GestorAI.API.Domain.Entities;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GestorAI.API.Services;

public record PlanoSaaSResponse(
    Guid Id, string Nome, string Descricao, decimal Preco, List<string> Features);

public record EmpresaPlanoAtualResponse(
    Guid PlanoId, string PlanoNome, string PlanoDescricao,
    decimal Preco, List<string> Features, DateTime InicioEm);

public class PlanoService(AppDbContext db, TenantContext tenantContext)
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<PlanoSaaSResponse>> ListPlanosAsync(CancellationToken ct)
    {
        var planos = await db.PlanosSaaS
            .Where(p => p.Ativo)
            .OrderBy(p => p.Preco)
            .ToListAsync(ct);

        return planos.Select(p => new PlanoSaaSResponse(
            p.Id, p.Nome, p.Descricao, p.Preco,
            JsonSerializer.Deserialize<List<string>>(p.Features, _json) ?? []
        )).ToList();
    }

    public async Task<EmpresaPlanoAtualResponse?> GetPlanoAtualAsync(CancellationToken ct)
    {
        var ep = await db.EmpresasPlano
            .Include(e => e.Plano)
            .Where(e => e.EmpresaId == tenantContext.EmpresaId && e.Ativo)
            .OrderByDescending(e => e.InicioEm)
            .FirstOrDefaultAsync(ct);

        if (ep?.Plano is null) return null;

        return new EmpresaPlanoAtualResponse(
            ep.PlanoSaaSId, ep.Plano.Nome, ep.Plano.Descricao, ep.Plano.Preco,
            JsonSerializer.Deserialize<List<string>>(ep.Plano.Features, _json) ?? [],
            ep.InicioEm);
    }

    public async Task AtivarPlanoAsync(Guid planoId, CancellationToken ct)
    {
        _ = await db.PlanosSaaS.FirstOrDefaultAsync(p => p.Id == planoId && p.Ativo, ct)
            ?? throw new AppException("Plano não encontrado.", 404);

        var atual = await db.EmpresasPlano
            .Where(ep => ep.EmpresaId == tenantContext.EmpresaId && ep.Ativo)
            .ToListAsync(ct);
        foreach (var a in atual)
        {
            a.Ativo = false;
            a.FimEm = DateTime.UtcNow;
        }

        db.EmpresasPlano.Add(new EmpresaPlano
        {
            EmpresaId = tenantContext.EmpresaId,
            PlanoSaaSId = planoId,
        });

        await db.SaveChangesAsync(ct);
    }
}
