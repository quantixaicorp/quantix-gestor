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
        await db.ContratoTemplates
            .Include(t => t.Itens)
            .OrderBy(t => t.Nome)
            .Select(t => new ContratoTemplateListItem(
                t.Id, t.Nome, t.TipoCobranca.ToString(),
                t.Periodicidade.ToString(), t.ValorPadrao, t.Itens.Count))
            .ToListAsync(ct);

    public async Task<ContratoTemplateResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var t = await db.ContratoTemplates
            .Include(t => t.Itens)
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new AppException("Template não encontrado.", 404);
        return ToResponse(t);
    }

    public async Task<ContratoTemplateResponse> CreateAsync(
        CreateContratoTemplateRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoCobranca>(req.TipoCobranca, out var tipo))
            throw new AppException($"TipoCobranca inválido: {req.TipoCobranca}.", 400);
        if (!Enum.TryParse<Periodicidade>(req.Periodicidade, out var periodicidade))
            throw new AppException($"Periodicidade inválida: {req.Periodicidade}.", 400);

        var template = new ContratoTemplate
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            Objeto = req.Objeto,
            TipoCobranca = tipo,
            Periodicidade = periodicidade,
            DiaVencimento = req.DiaVencimento,
            ValorPadrao = req.ValorPadrao,
        };
        foreach (var item in req.Itens)
            template.Itens.Add(new ContratoTemplateItem
            {
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
            });

        db.ContratoTemplates.Add(template);
        await db.SaveChangesAsync(ct);
        return await GetAsync(template.Id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var t = await db.ContratoTemplates
            .FirstOrDefaultAsync(t => t.Id == id, ct)
            ?? throw new AppException("Template não encontrado.", 404);
        db.ContratoTemplates.Remove(t);
        await db.SaveChangesAsync(ct);
    }

    private static ContratoTemplateResponse ToResponse(ContratoTemplate t) => new(
        t.Id, t.Nome, t.Objeto,
        t.TipoCobranca.ToString(), t.Periodicidade.ToString(),
        t.DiaVencimento, t.ValorPadrao, t.CriadoEm,
        t.Itens.Select(i => new ContratoTemplateItemResponse(
            i.Id, i.Descricao, i.Quantidade, i.ValorUnitario)).ToList(),
        t.Itens.Sum(i => i.Quantidade * i.ValorUnitario));
}
