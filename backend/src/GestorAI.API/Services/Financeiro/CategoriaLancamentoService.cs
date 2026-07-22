using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Financeiro;

public class CategoriaLancamentoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<CategoriaLancamentoResponse>> ListAsync(string? tipo, CancellationToken ct)
    {
        var query = db.TransactionCategories.AsQueryable();

        if (!string.IsNullOrEmpty(tipo) && Enum.TryParse<TipoLancamento>(tipo, out var t))
            query = query.Where(c => c.Type == t);

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new CategoriaLancamentoResponse(c.Id, c.Name, c.Type.ToString()))
            .ToListAsync(ct);
    }

    public async Task<CategoriaLancamentoResponse> CreateAsync(
        CreateCategoriaLancamentoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoLancamento>(req.Type, out var tipo))
            throw new AppException($"Type inválido: {req.Type}.", 400);

        var existe = await db.TransactionCategories
            .AnyAsync(c => c.Name == req.Name && c.Type == tipo, ct);
        if (existe)
            throw new AppException("Já existe uma categoria com este nome para o tipo informado.", 400);

        var cat = new TransactionCategory
        {
            CompanyId = tenantContext.CompanyId,
            Name = req.Name,
            Type = tipo
        };
        db.TransactionCategories.Add(cat);
        await db.SaveChangesAsync(ct);
        return new CategoriaLancamentoResponse(cat.Id, cat.Name, cat.Type.ToString());
    }

    public async Task<CategoriaLancamentoResponse> UpdateAsync(
        Guid id, UpdateCategoriaLancamentoRequest req, CancellationToken ct)
    {
        var cat = await db.TransactionCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Category não encontrada.", 404);

        var nomeAntigo = cat.Name;

        var duplicado = await db.TransactionCategories
            .AnyAsync(c => c.Id != id && c.Name == req.Name && c.Type == cat.Type, ct);
        if (duplicado)
            throw new AppException("Já existe uma categoria com este nome para o tipo informado.", 400);

        cat.Name = req.Name;

        var lancamentosParaAtualizar = await db.Transactions
            .Where(l => l.Category == nomeAntigo)
            .ToListAsync(ct);
        foreach (var l in lancamentosParaAtualizar)
            l.Category = req.Name;

        await db.SaveChangesAsync(ct);

        return new CategoriaLancamentoResponse(cat.Id, cat.Name, cat.Type.ToString());
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var cat = await db.TransactionCategories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Category não encontrada.", 404);

        var emUso = await db.Transactions
            .AnyAsync(l => l.Category == cat.Name, ct);
        if (emUso)
            throw new AppException(
                "Esta categoria está em uso em lançamentos e não pode ser excluída.", 400);

        db.TransactionCategories.Remove(cat);
        await db.SaveChangesAsync(ct);
    }
}
