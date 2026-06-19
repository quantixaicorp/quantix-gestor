using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Estoque;

public class CategoriaService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<CategoriaResponse>> ListAsync(CancellationToken ct) =>
        await db.Categorias
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaResponse(c.Id, c.Nome))
            .ToListAsync(ct);

    public async Task<CategoriaResponse> CreateAsync(CreateCategoriaRequest req, CancellationToken ct)
    {
        var categoria = new Categoria { Nome = req.Nome, EmpresaId = tenantContext.EmpresaId };
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync(ct);
        return new CategoriaResponse(categoria.Id, categoria.Nome);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var categoria = await db.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException("Categoria não encontrada");
        db.Categorias.Remove(categoria);
        await db.SaveChangesAsync(ct);
    }
}
