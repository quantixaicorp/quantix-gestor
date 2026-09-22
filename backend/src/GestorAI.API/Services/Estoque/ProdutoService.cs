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
        var query = db.Products.Include(p => p.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(busca))
            query = query.Where(p => p.Name.Contains(busca) ||
                                     (p.Barcode != null && p.Barcode == busca));
        if (categoriaId.HasValue)
            query = query.Where(p => p.CategoryId == categoriaId.Value);
        if (apenasEstoqueBaixo == true)
            query = query.Where(p => p.CurrentStock <= p.MinimumStock);

        return await query
            .OrderBy(p => p.Name)
            .Select(p => ToResponse(p))
            .ToListAsync(ct);
    }

    public async Task<ProdutoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Products.Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Product não encontrado", 404);
        return ToResponse(p);
    }

    public async Task<ProdutoResponse> CreateAsync(CreateProdutoRequest req, CancellationToken ct)
    {
        var produto = new Product
        {
            CompanyId = tenantContext.CompanyId,
            CategoryId = req.CategoryId,
            Name = req.Name,
            Description = req.Description,
            SalePrice = req.SalePrice,
            AverageCost = req.AverageCost,
            CurrentStock = req.CurrentStock,
            MinimumStock = req.MinimumStock,
            Barcode = req.Barcode,
            Type = req.Type,
            DurationMinutes = req.DurationMinutes,
        };
        db.Products.Add(produto);

        if (req.CurrentStock > 0)
            db.StockMovements.Add(new StockMovement
            {
                CompanyId = tenantContext.CompanyId,
                ProductId = produto.Id,
                Type = TipoMovimentacao.Entrada,
                Quantity = req.CurrentStock,
                Source = OrigemMovimentacao.Manual,
                Notes = "Estoque inicial",
            });

        await db.SaveChangesAsync(ct);
        return await GetAsync(produto.Id, ct);
    }

    public async Task<ProdutoResponse> UpdateAsync(Guid id, UpdateProdutoRequest req, CancellationToken ct)
    {
        var produto = await db.Products.FindAsync([id], ct)
            ?? throw new AppException("Product não encontrado", 404);

        produto.CategoryId = req.CategoryId;
        produto.Name = req.Name;
        produto.Description = req.Description;
        produto.SalePrice = req.SalePrice;
        produto.MinimumStock = req.MinimumStock;
        produto.Barcode = req.Barcode;
        produto.IsActive = req.IsActive;
        produto.DurationMinutes = req.DurationMinutes;
        produto.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var produto = await db.Products.FindAsync([id], ct)
            ?? throw new AppException("Product não encontrado", 404);
        db.Products.Remove(produto);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException) { throw new AppException("Não é possível excluir: item possui movimentações vinculadas.", 409); }
    }

    public async Task<ProdutoResponse> EntradaEstoqueAsync(EntradaEstoqueRequest req, CancellationToken ct)
    {
        var produto = await db.Products.FindAsync([req.ProductId], ct)
            ?? throw new AppException("Product não encontrado", 404);

        var novoEstoque = produto.CurrentStock + req.Quantity;

        if (req.CustoUnitario.HasValue && novoEstoque > 0)
            produto.AverageCost =
                (produto.CurrentStock * produto.AverageCost + req.Quantity * req.CustoUnitario.Value)
                / novoEstoque;

        produto.CurrentStock = novoEstoque;
        produto.UpdatedAt = DateTime.UtcNow;

        db.StockMovements.Add(new StockMovement
        {
            CompanyId = tenantContext.CompanyId,
            ProductId = produto.Id,
            Type = TipoMovimentacao.Entrada,
            Quantity = req.Quantity,
            Source = OrigemMovimentacao.Manual,
            Notes = req.Notes,
        });

        await db.SaveChangesAsync(ct);
        return await GetAsync(produto.Id, ct);
    }

    public async Task<List<MovimentacaoResponse>> ListMovimentacoesAsync(
        Guid? produtoId, CancellationToken ct) =>
        await db.StockMovements
            .Include(m => m.Product)
            .Where(m => !produtoId.HasValue || m.ProductId == produtoId.Value)
            .OrderByDescending(m => m.MovementDate)
            .Select(m => new MovimentacaoResponse(
                m.Id, m.ProductId, m.Product!.Name,
                m.Type.ToString(), m.Quantity,
                m.Source.ToString(), m.MovementDate, m.Notes))
            .ToListAsync(ct);

    private static ProdutoResponse ToResponse(Product p) => new(
        p.Id, p.CategoryId, p.Category?.Name ?? "",
        p.Name, p.Description, p.SalePrice, p.AverageCost,
        p.CurrentStock, p.MinimumStock, p.Barcode,
        p.IsActive, p.Type == TipoProduto.Produto && p.CurrentStock <= p.MinimumStock, p.DurationMinutes, p.Type);
}
