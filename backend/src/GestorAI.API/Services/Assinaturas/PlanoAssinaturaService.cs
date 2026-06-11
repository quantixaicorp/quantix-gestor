using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Assinaturas;

public class PlanoAssinaturaService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<PlanoAssinaturaListItem>> ListAsync(CancellationToken ct) =>
        await db.PlanosAssinatura
            .Select(p => new PlanoAssinaturaListItem(
                p.Id, p.Nome, p.Nicho, p.Preco, p.Periodicidade.ToString(),
                p.Ativo, p.MaisVendido,
                p.Assinantes.Count(a => a.Status == AssinaturaStatus.Ativa)))
            .ToListAsync(ct);

    public async Task<PlanoAssinaturaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.PlanosAssinatura
            .Include(x => x.Itens)
            .Include(x => x.Assinantes)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Plano não encontrado.", 404);
        return ToResponse(p);
    }

    public async Task<PlanoAssinaturaResponse> CreateAsync(CreatePlanoAssinaturaRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<Periodicidade>(req.Periodicidade, out var per))
            throw new AppException($"Periodicidade inválida: {req.Periodicidade}.", 400);

        var plano = new PlanoAssinatura
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            Descricao = req.Descricao,
            Nicho = req.Nicho,
            Preco = req.Preco,
            Periodicidade = per,
            MaisVendido = req.MaisVendido,
        };

        foreach (var item in req.Itens)
            plano.Itens.Add(MapItem(item));

        db.PlanosAssinatura.Add(plano);
        await db.SaveChangesAsync(ct);
        return await GetAsync(plano.Id, ct);
    }

    public async Task<PlanoAssinaturaResponse> UpdateAsync(Guid id, UpdatePlanoAssinaturaRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<Periodicidade>(req.Periodicidade, out var per))
            throw new AppException($"Periodicidade inválida: {req.Periodicidade}.", 400);

        var plano = await db.PlanosAssinatura.Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Plano não encontrado.", 404);

        plano.Nome = req.Nome;
        plano.Descricao = req.Descricao;
        plano.Nicho = req.Nicho;
        plano.Preco = req.Preco;
        plano.Periodicidade = per;
        plano.MaisVendido = req.MaisVendido;
        plano.Ativo = req.Ativo;

        plano.Itens.Clear();
        foreach (var item in req.Itens)
            plano.Itens.Add(MapItem(item));

        await db.SaveChangesAsync(ct);
        return await GetAsync(plano.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var plano = await db.PlanosAssinatura.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Plano não encontrado.", 404);

        var temAssinantes = await db.AssinaturasCliente
            .AnyAsync(a => a.PlanoAssinaturaId == id && a.Status == AssinaturaStatus.Ativa, ct);
        if (temAssinantes)
            throw new AppException("Plano possui assinantes ativos e não pode ser removido.", 400);

        db.PlanosAssinatura.Remove(plano);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<NichoTemplateResponse>> ListTemplatesAsync(string? nicho, CancellationToken ct)
    {
        var query = db.NichoTemplates.Include(t => t.Itens).AsQueryable();
        if (!string.IsNullOrWhiteSpace(nicho))
            query = query.Where(t => t.Nicho == nicho);
        var templates = await query.ToListAsync(ct);
        return templates.Select(t => new NichoTemplateResponse(
            t.Id, t.Nicho, t.NomePlano, t.Descricao, t.PrecoSugerido, t.MaisVendido,
            t.Periodicidade.ToString(),
            t.Itens.Select(i => new NichoTemplateItemResponse(
                i.Id, i.Descricao, i.QuantidadePorCiclo, i.Tipo.ToString(), i.PercentualDesconto
            )).ToList()
        )).ToList();
    }

    public async Task<List<PlanoAssinaturaListItem>> ListPublicAsync(Guid empresaId, CancellationToken ct) =>
        await db.PlanosAssinatura
            .IgnoreQueryFilters()
            .Where(p => p.EmpresaId == empresaId && p.Ativo)
            .Select(p => new PlanoAssinaturaListItem(
                p.Id, p.Nome, p.Nicho, p.Preco, p.Periodicidade.ToString(),
                p.Ativo, p.MaisVendido,
                p.Assinantes.Count(a => a.Status == AssinaturaStatus.Ativa)))
            .ToListAsync(ct);

    public async Task<PlanoAssinaturaResponse> GetPublicAsync(Guid empresaId, Guid planoId, CancellationToken ct)
    {
        var p = await db.PlanosAssinatura
            .IgnoreQueryFilters()
            .Include(x => x.Itens)
            .Include(x => x.Assinantes)
            .FirstOrDefaultAsync(x => x.Id == planoId && x.EmpresaId == empresaId && x.Ativo, ct)
            ?? throw new AppException("Plano não encontrado.", 404);
        return ToResponse(p);
    }

    private static PlanoAssinaturaItem MapItem(PlanoItemRequest item)
    {
        if (!Enum.TryParse<TipoItemPlano>(item.Tipo, out var tipo))
            throw new AppException($"TipoItemPlano inválido: {item.Tipo}.", 400);
        return new PlanoAssinaturaItem
        {
            Descricao = item.Descricao,
            ServicoId = item.ServicoId,
            QuantidadePorCiclo = item.QuantidadePorCiclo,
            Tipo = tipo,
            PercentualDesconto = item.PercentualDesconto,
        };
    }

    private static PlanoAssinaturaResponse ToResponse(PlanoAssinatura p) =>
        new(p.Id, p.Nome, p.Descricao, p.Nicho, p.Preco, p.Periodicidade.ToString(),
            p.Ativo, p.MaisVendido,
            p.Assinantes.Count(a => a.Status == AssinaturaStatus.Ativa),
            p.Itens.Select(i => new PlanoItemResponse(
                i.Id, i.Descricao, i.ServicoId, i.QuantidadePorCiclo,
                i.Tipo.ToString(), i.PercentualDesconto)).ToList(),
            p.CriadoEm);
}
