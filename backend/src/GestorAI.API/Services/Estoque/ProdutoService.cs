using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Estoque;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Estoque;

public class ProdutoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ProdutoResponse>> ListAsync(
        string? busca, Guid? categoriaId, bool? apenasEstoqueBaixo, CancellationToken ct)
    {
        var query = db.Produtos.Include(p => p.Categoria).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(p => p.Nome.Contains(busca) ||
                                     (p.CodigoBarras != null && p.CodigoBarras == busca));
        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoriaId == categoriaId.Value);
        if (apenasEstoqueBaixo == true)
            query = query.Where(p => p.EstoqueAtual <= p.EstoqueMinimo);

        return await query
            .OrderBy(p => p.Nome)
            .Select(p => ToResponse(p))
            .ToListAsync(ct);
    }

    public async Task<ProdutoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Produtos.Include(x => x.Categoria)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Produto não encontrado", 404);
        return ToResponse(p);
    }

    public async Task<ProdutoResponse> CreateAsync(CreateProdutoRequest req, CancellationToken ct)
    {
        var produto = new Produto
        {
            EmpresaId = tenantContext.EmpresaId,
            CategoriaId = req.CategoriaId,
            Nome = req.Nome,
            Descricao = req.Descricao,
            PrecoVenda = req.PrecoVenda,
            CustoMedio = req.CustoMedio,
            EstoqueAtual = req.EstoqueAtual,
            EstoqueMinimo = req.EstoqueMinimo,
            CodigoBarras = req.CodigoBarras,
            Tipo = req.Tipo,
        };
        db.Produtos.Add(produto);

        if (req.EstoqueAtual > 0)
            db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                EmpresaId = tenantContext.EmpresaId,
                ProdutoId = produto.Id,
                Tipo = TipoMovimentacao.Entrada,
                Quantidade = req.EstoqueAtual,
                Origem = OrigemMovimentacao.Manual,
                Observacao = "Estoque inicial",
            });

        await db.SaveChangesAsync(ct);
        return await GetAsync(produto.Id, ct);
    }

    public async Task<ProdutoResponse> UpdateAsync(Guid id, UpdateProdutoRequest req, CancellationToken ct)
    {
        var produto = await db.Produtos.FindAsync([id], ct)
            ?? throw new AppException("Produto não encontrado", 404);

        produto.CategoriaId = req.CategoriaId;
        produto.Nome = req.Nome;
        produto.Descricao = req.Descricao;
        produto.PrecoVenda = req.PrecoVenda;
        produto.EstoqueMinimo = req.EstoqueMinimo;
        produto.CodigoBarras = req.CodigoBarras;
        produto.Ativo = req.Ativo;
        produto.DuracaoMinutos = req.DuracaoMinutos;
        produto.AtualizadoEm = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var produto = await db.Produtos.FindAsync([id], ct)
            ?? throw new AppException("Produto não encontrado", 404);
        db.Produtos.Remove(produto);
        await db.SaveChangesAsync(ct);
    }

    public async Task<ProdutoResponse> EntradaEstoqueAsync(EntradaEstoqueRequest req, CancellationToken ct)
    {
        var produto = await db.Produtos.FindAsync([req.ProdutoId], ct)
            ?? throw new AppException("Produto não encontrado", 404);

        var novoEstoque = produto.EstoqueAtual + req.Quantidade;

        if (req.CustoUnitario.HasValue && novoEstoque > 0)
            produto.CustoMedio =
                (produto.EstoqueAtual * produto.CustoMedio + req.Quantidade * req.CustoUnitario.Value)
                / novoEstoque;

        produto.EstoqueAtual = novoEstoque;
        produto.AtualizadoEm = DateTime.UtcNow;

        db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
        {
            EmpresaId = tenantContext.EmpresaId,
            ProdutoId = produto.Id,
            Tipo = TipoMovimentacao.Entrada,
            Quantidade = req.Quantidade,
            Origem = OrigemMovimentacao.Manual,
            Observacao = req.Observacao,
        });

        await db.SaveChangesAsync(ct);
        return await GetAsync(produto.Id, ct);
    }

    public async Task<List<MovimentacaoResponse>> ListMovimentacoesAsync(
        Guid? produtoId, CancellationToken ct) =>
        await db.MovimentacoesEstoque
            .Include(m => m.Produto)
            .Where(m => !produtoId.HasValue || m.ProdutoId == produtoId.Value)
            .OrderByDescending(m => m.DataHora)
            .Select(m => new MovimentacaoResponse(
                m.Id, m.ProdutoId, m.Produto!.Nome,
                m.Tipo.ToString(), m.Quantidade,
                m.Origem.ToString(), m.DataHora, m.Observacao))
            .ToListAsync(ct);

    private static ProdutoResponse ToResponse(Produto p) => new(
        p.Id, p.CategoriaId, p.Categoria?.Nome ?? "",
        p.Nome, p.Descricao, p.PrecoVenda, p.CustoMedio,
        p.EstoqueAtual, p.EstoqueMinimo, p.CodigoBarras,
        p.Ativo, p.EstoqueAtual <= p.EstoqueMinimo, p.DuracaoMinutos, p.Tipo);
}
