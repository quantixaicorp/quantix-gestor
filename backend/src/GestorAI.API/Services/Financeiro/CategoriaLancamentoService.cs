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
        var query = db.CategoriasLancamento.AsQueryable();

        if (!string.IsNullOrEmpty(tipo) && Enum.TryParse<TipoLancamento>(tipo, out var t))
            query = query.Where(c => c.Tipo == t);

        return await query
            .OrderBy(c => c.Nome)
            .Select(c => new CategoriaLancamentoResponse(c.Id, c.Nome, c.Tipo.ToString()))
            .ToListAsync(ct);
    }

    public async Task<CategoriaLancamentoResponse> CreateAsync(
        CreateCategoriaLancamentoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoLancamento>(req.Tipo, out var tipo))
            throw new AppException($"Tipo inválido: {req.Tipo}.", 400);

        var existe = await db.CategoriasLancamento
            .AnyAsync(c => c.Nome == req.Nome && c.Tipo == tipo, ct);
        if (existe)
            throw new AppException("Já existe uma categoria com este nome para o tipo informado.", 400);

        var cat = new CategoriaLancamento
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            Tipo = tipo
        };
        db.CategoriasLancamento.Add(cat);
        await db.SaveChangesAsync(ct);
        return new CategoriaLancamentoResponse(cat.Id, cat.Nome, cat.Tipo.ToString());
    }

    public async Task<CategoriaLancamentoResponse> UpdateAsync(
        Guid id, UpdateCategoriaLancamentoRequest req, CancellationToken ct)
    {
        var cat = await db.CategoriasLancamento.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Categoria não encontrada.", 404);

        var nomeAntigo = cat.Nome;

        var duplicado = await db.CategoriasLancamento
            .AnyAsync(c => c.Id != id && c.Nome == req.Nome && c.Tipo == cat.Tipo, ct);
        if (duplicado)
            throw new AppException("Já existe uma categoria com este nome para o tipo informado.", 400);

        cat.Nome = req.Nome;

        var lancamentosParaAtualizar = await db.Lancamentos
            .Where(l => l.Categoria == nomeAntigo)
            .ToListAsync(ct);
        foreach (var l in lancamentosParaAtualizar)
            l.Categoria = req.Nome;

        await db.SaveChangesAsync(ct);

        return new CategoriaLancamentoResponse(cat.Id, cat.Nome, cat.Tipo.ToString());
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var cat = await db.CategoriasLancamento.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Categoria não encontrada.", 404);

        var emUso = await db.Lancamentos
            .AnyAsync(l => l.Categoria == cat.Nome, ct);
        if (emUso)
            throw new AppException(
                "Esta categoria está em uso em lançamentos e não pode ser excluída.", 400);

        db.CategoriasLancamento.Remove(cat);
        await db.SaveChangesAsync(ct);
    }
}
