using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contratos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contratos;

public class ContratoTemplateService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ContratoTemplateListItem>> ListAsync(CancellationToken ct) =>
        await db.ContractTemplates
            .Include(t => t.Items)
            .OrderBy(t => t.Name)
            .Select(t => new ContratoTemplateListItem(
                t.Id, t.Name, t.ChargeType.ToString(),
                t.Frequency.ToString(), t.DefaultAmount, t.Items.Count))
            .ToListAsync(ct);

    public async Task<ContratoTemplateResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var t = await db.ContractTemplates
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new AppException("Template não encontrado.", 404);
        return ToResponse(t);
    }

    public async Task<ContratoTemplateResponse> CreateAsync(
        CreateContratoTemplateRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoCobranca>(req.ChargeType, out var tipo))
            throw new AppException($"TipoCobranca inválido: {req.ChargeType}.", 400);
        if (!Enum.TryParse<Periodicidade>(req.Frequency, out var periodicidade))
            throw new AppException($"Periodicidade inválida: {req.Frequency}.", 400);

        var template = new ContractTemplate
        {
            CompanyId = tenantContext.CompanyId,
            Name = req.Name,
            Subject = req.Subject,
            ChargeType = tipo,
            Frequency = periodicidade,
            DueDay = req.DueDay,
            DefaultAmount = req.DefaultAmount,
        };
        foreach (var item in req.Items)
            template.Items.Add(new ContractTemplateItem
            {
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
            });

        db.ContractTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return await GetAsync(template.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var t = await db.ContractTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new AppException("Template não encontrado.", 404);
        db.ContractTemplates.Remove(t);
        await db.SaveChangesAsync(ct);
    }

    private static ContratoTemplateResponse ToResponse(ContractTemplate t) => new(
        t.Id, t.Name, t.Subject,
        t.ChargeType.ToString(), t.Frequency.ToString(),
        t.DueDay, t.DefaultAmount, t.CreatedAt,
        t.Items.Select(i => new ContratoTemplateItemResponse(
            i.Id, i.Description, i.Quantity, i.UnitPrice)).ToList(),
        t.Items.Sum(i => i.Quantity * i.UnitPrice));
}
