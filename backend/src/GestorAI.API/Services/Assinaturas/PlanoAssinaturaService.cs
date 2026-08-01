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
        await db.SubscriptionPlans
            .Select(p => new PlanoAssinaturaListItem(
                p.Id, p.Name, p.Niche, p.Price, p.Frequency.ToString(),
                p.IsActive, p.BestSeller,
                p.Subscribers.Count(a => a.Status == AssinaturaStatus.Ativa)))
            .ToListAsync(ct);

    public async Task<PlanoAssinaturaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.SubscriptionPlans
            .Include(x => x.Items)
            .Include(x => x.Subscribers)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Plan não encontrado.", 404);
        return ToResponse(p);
    }

    public async Task<PlanoAssinaturaResponse> CreateAsync(CreatePlanoAssinaturaRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<Periodicidade>(req.Frequency, out var per))
            throw new AppException($"Periodicidade inválida: {req.Frequency}.", 400);

        var plano = new SubscriptionPlan
        {
            CompanyId = tenantContext.CompanyId,
            Name = req.Name,
            Description = req.Description,
            Niche = req.Niche,
            Price = req.Price,
            Frequency = per,
            BestSeller = req.BestSeller,
        };

        foreach (var item in req.Items)
            plano.Items.Add(MapItem(item));

        db.SubscriptionPlans.Add(plano);
        await db.SaveChangesAsync(ct);
        return await GetAsync(plano.Id, ct);
    }

    public async Task<PlanoAssinaturaResponse> UpdateAsync(Guid id, UpdatePlanoAssinaturaRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<Periodicidade>(req.Frequency, out var per))
            throw new AppException($"Periodicidade inválida: {req.Frequency}.", 400);

        var plano = await db.SubscriptionPlans.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Plan não encontrado.", 404);

        plano.Name = req.Name;
        plano.Description = req.Description;
        plano.Niche = req.Niche;
        plano.Price = req.Price;
        plano.Frequency = per;
        plano.BestSeller = req.BestSeller;
        plano.IsActive = req.IsActive;

        plano.Items.Clear();
        foreach (var item in req.Items)
            plano.Items.Add(MapItem(item));

        await db.SaveChangesAsync(ct);
        return await GetAsync(plano.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var plano = await db.SubscriptionPlans.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Plan não encontrado.", 404);

        var temAssinantes = await db.CustomerSubscriptions
            .AnyAsync(a => a.SubscriptionPlanId == id && a.Status == AssinaturaStatus.Ativa, ct);
        if (temAssinantes)
            throw new AppException("Plan possui assinantes ativos e não pode ser removido.", 400);

        db.SubscriptionPlans.Remove(plano);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<NichoTemplateResponse>> ListTemplatesAsync(string? nicho, CancellationToken ct)
    {
        var query = db.NicheTemplates.Include(t => t.Items).AsQueryable();
        if (!string.IsNullOrWhiteSpace(nicho))
            query = query.Where(t => t.Niche == nicho);
        var templates = await query.ToListAsync(ct);
        return templates.Select(t => new NichoTemplateResponse(
            t.Id, t.Niche, t.PlanName, t.Description, t.SuggestedPrice, t.BestSeller,
            t.Frequency.ToString(),
            t.Items.Select(i => new NichoTemplateItemResponse(
                i.Id, i.Description, i.QuantityPerCycle, i.Type.ToString(), i.DiscountPercentage
            )).ToList()
        )).ToList();
    }

    public async Task<List<PlanoAssinaturaListItem>> ListPublicAsync(Guid empresaId, CancellationToken ct) =>
        await db.SubscriptionPlans
            .IgnoreQueryFilters()
            .Where(p => p.CompanyId == empresaId && p.IsActive)
            .Select(p => new PlanoAssinaturaListItem(
                p.Id, p.Name, p.Niche, p.Price, p.Frequency.ToString(),
                p.IsActive, p.BestSeller,
                p.Subscribers.Count(a => a.Status == AssinaturaStatus.Ativa)))
            .ToListAsync(ct);

    public async Task<PlanoAssinaturaResponse> GetPublicAsync(Guid empresaId, Guid planoId, CancellationToken ct)
    {
        var p = await db.SubscriptionPlans
            .IgnoreQueryFilters()
            .Include(x => x.Items)
            .Include(x => x.Subscribers)
            .FirstOrDefaultAsync(x => x.Id == planoId && x.CompanyId == empresaId && x.IsActive, ct)
            ?? throw new AppException("Plan não encontrado.", 404);
        return ToResponse(p);
    }

    private static SubscriptionPlanItem MapItem(PlanoItemRequest item)
    {
        if (!Enum.TryParse<TipoItemPlano>(item.Type, out var tipo))
            throw new AppException($"TipoItemPlano inválido: {item.Type}.", 400);
        return new SubscriptionPlanItem
        {
            Description = item.Description,
            ServiceId = item.ServiceId,
            QuantityPerCycle = item.QuantityPerCycle,
            Type = tipo,
            DiscountPercentage = item.DiscountPercentage,
        };
    }

    private static PlanoAssinaturaResponse ToResponse(SubscriptionPlan p) =>
        new(p.Id, p.Name, p.Description, p.Niche, p.Price, p.Frequency.ToString(),
            p.IsActive, p.BestSeller,
            p.Subscribers.Count(a => a.Status == AssinaturaStatus.Ativa),
            p.Items.Select(i => new PlanoItemResponse(
                i.Id, i.Description, i.ServiceId, i.QuantityPerCycle,
                i.Type.ToString(), i.DiscountPercentage)).ToList(),
            p.CreatedAt);
}
