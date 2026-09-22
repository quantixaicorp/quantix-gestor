using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Estoque;

public class CategoriaService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<CategoriaResponse>> ListAsync(CancellationToken ct) =>
        await db.Categories
            .OrderBy(c => c.Name)
            .Select(c => new CategoriaResponse(c.Id, c.Name))
            .ToListAsync(ct);

    public async Task<CategoriaResponse> CreateAsync(CreateCategoriaRequest req, CancellationToken ct)
    {
        var categoria = new Category { Name = req.Name, CompanyId = tenantContext.CompanyId };
        db.Categories.Add(categoria);
        await db.SaveChangesAsync(ct);
        return new CategoriaResponse(categoria.Id, categoria.Name);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var categoria = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("Category não encontrada");
        db.Categories.Remove(categoria);
        await db.SaveChangesAsync(ct);
    }
}
