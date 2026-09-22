using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Compras;

public class CompraService(
    AppDbContext db,
    TenantContext tenantContext,
    ParcelamentoService parcelamentoService)
{
    public async Task<List<CompraResponse>> ListAsync(
        string? status, Guid? fornecedorId, DateTime? de, DateTime? ate, CancellationToken ct)
    {
        var query = db.Purchases
            .Include(c => c.Supplier)
            .Include(c => c.Items)
            .Include(c => c.InstallmentPlan)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusCompra>(status, out var s))
            query = query.Where(c => c.Status == s);
        if (fornecedorId.HasValue)
            query = query.Where(c => c.SupplierId == fornecedorId.Value);
        if (de.HasValue)
            query = query.Where(c => c.Date >= de.Value);
        if (ate.HasValue)
            query = query.Where(c => c.Date <= ate.Value);

        var list = await query.OrderByDescending(c => c.Number).ToListAsync(ct);
        return list.Select(ToResponse).ToList();
    }

    public async Task<CompraResumoResponse> GetResumoAsync(CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var comprasMes = await db.Purchases
            .Where(c => c.Status == StatusCompra.Confirmada && c.Date >= inicioMes)
            .ToListAsync(ct);

        var totalContasPagar = await db.InstallmentPlans
            .Include(p => p.Installments)
            .Where(p => p.PurchaseId != null && p.Status != StatusParcelamento.Cancelado)
            .SelectMany(p => p.Installments)
            .Where(l => l.Status == StatusLancamento.Pendente)
            .SumAsync(l => (decimal?)l.Amount, ct) ?? 0m;

        return new CompraResumoResponse(
            comprasMes.Sum(c => c.TotalAmount),
            comprasMes.Count,
            totalContasPagar);
    }

    public async Task<CompraResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Purchases
            .Include(x => x.Supplier)
            .Include(x => x.Items)
            .Include(x => x.InstallmentPlan).ThenInclude(p => p!.Installments)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Purchase não encontrada.", 404);
        return ToResponse(c);
    }

    public async Task<CompraResponse> CreateAsync(CreateCompraRequest req, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var numero = await NextNumeroCompraAsync(ct);
        var compra = new Purchase
        {
            CompanyId = tenantContext.CompanyId,
            Number = numero,
            Date = req.Date,
            SupplierId = req.SupplierId,
            PurchaseOrderId = req.PurchaseOrderId,
            PurchaseType = req.PurchaseType,
            NoteNumber = req.NoteNumber,
            PaymentTerms = req.PaymentTerms,
            PaymentMethod = req.PaymentMethod,
            Status = StatusCompra.Rascunho,
            Notes = req.Notes,
        };

        foreach (var itemReq in req.Items)
        {
            var item = BuildItem(itemReq);
            item.CompanyId = tenantContext.CompanyId;
            item.PurchaseId = compra.Id;
            compra.Items.Add(item);
        }

        compra.TotalAmount = compra.Items.Sum(i => i.TotalAmount);
        db.Purchases.Add(compra);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetAsync(compra.Id, ct);
    }

    public async Task<CompraResponse> UpdateAsync(Guid id, UpdateCompraRequest req, CancellationToken ct)
    {
        var compra = await db.Purchases
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Purchase não encontrada.", 404);

        if (compra.Status != StatusCompra.Rascunho)
            throw new AppException("Apenas rascunhos podem ser editados.", 400);

        compra.SupplierId = req.SupplierId;
        compra.Date = req.Date;
        compra.NoteNumber = req.NoteNumber;
        compra.PurchaseType = req.PurchaseType;
        compra.PurchaseOrderId = req.PurchaseOrderId;
        compra.Notes = req.Notes;
        compra.PaymentTerms = req.PaymentTerms;
        compra.PaymentMethod = req.PaymentMethod;

        db.PurchaseItems.RemoveRange(compra.Items);
        compra.Items.Clear();

        foreach (var itemReq in req.Items)
        {
            var item = BuildItem(itemReq);
            item.CompanyId = tenantContext.CompanyId;
            item.PurchaseId = compra.Id;
            compra.Items.Add(item);
        }

        compra.TotalAmount = compra.Items.Sum(i => i.TotalAmount);
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<CompraResponse> ConfirmarAsync(Guid id, CancellationToken ct)
    {
        var compra = await db.Purchases
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Purchase não encontrada.", 404);

        if (compra.Status != StatusCompra.Rascunho)
            throw new AppException("Apenas rascunhos podem ser confirmados.", 400);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Atualizar estoque para itens com destino EstoqueParaVenda
        foreach (var item in compra.Items.Where(i => i.Destination == DestinoCompra.EstoqueParaVenda && i.ProductId.HasValue))
        {
            var produto = await db.Products.FindAsync([item.ProductId!.Value], ct)
                ?? throw new AppException($"Product {item.ProductId} não encontrado.", 404);

            var novoEstoque = produto.CurrentStock + item.Quantity;
            if (novoEstoque > 0)
                produto.AverageCost = (produto.CurrentStock * produto.AverageCost + item.Quantity * item.UnitPrice) / novoEstoque;

            produto.CurrentStock = novoEstoque;
            produto.UpdatedAt = DateTime.UtcNow;

            db.StockMovements.Add(new StockMovement
            {
                CompanyId = tenantContext.CompanyId,
                ProductId = item.ProductId!.Value,
                Type = TipoMovimentacao.Entrada,
                Quantity = item.Quantity,
                Source = OrigemMovimentacao.Compra,
                Notes = $"Purchase #{compra.Number}",
            });
        }

        // Gerar parcelamento
        var categoriaDefault = compra.Items.Select(i => i.FinancialCategory)
            .FirstOrDefault(c => !string.IsNullOrEmpty(c)) ?? "Compras";

        var vencimentos = VencimentoCalculator.Calcular(
            compra.PaymentTerms,
            compra.Date,
            compra.TotalAmount,
            null,
            null);

        var descricao = $"Purchase #{compra.Number}";
        var parcelamento = await parcelamentoService.CriarAsync(
            compra.Id, descricao, compra.TotalAmount, vencimentos, categoriaDefault, ct);

        compra.Status = StatusCompra.Confirmada;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<CompraResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var compra = await db.Purchases
            .Include(c => c.Items)
            .Include(c => c.InstallmentPlan)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Purchase não encontrada.", 404);

        if (compra.Status == StatusCompra.Cancelada)
            throw new AppException("Purchase já está cancelada.", 400);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        if (compra.Status == StatusCompra.Confirmada)
        {
            // Reverter estoque
            foreach (var item in compra.Items.Where(i => i.Destination == DestinoCompra.EstoqueParaVenda && i.ProductId.HasValue))
            {
                var produto = await db.Products.FindAsync([item.ProductId!.Value], ct);
                if (produto is null) continue;

                produto.CurrentStock -= item.Quantity;
                produto.UpdatedAt = DateTime.UtcNow;

                db.StockMovements.Add(new StockMovement
                {
                    CompanyId = tenantContext.CompanyId,
                    ProductId = item.ProductId!.Value,
                    Type = TipoMovimentacao.Saida,
                    Quantity = item.Quantity,
                    Source = OrigemMovimentacao.Manual,
                    Notes = $"Cancelamento Purchase #{compra.Number}",
                });
            }

            // Cancelar parcelas
            if (compra.InstallmentPlan is not null)
                await parcelamentoService.CancelarParcelasAsync(compra.InstallmentPlan.Id, ct);
        }

        compra.Status = StatusCompra.Cancelada;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var compra = await db.Purchases.FindAsync([id], ct)
            ?? throw new AppException("Purchase não encontrada.", 404);

        if (compra.Status != StatusCompra.Rascunho)
            throw new AppException("Apenas rascunhos podem ser excluídos.", 400);

        db.Purchases.Remove(compra);
        await db.SaveChangesAsync(ct);
    }

    private async Task<int> NextNumeroCompraAsync(CancellationToken ct)
    {
        var max = await db.Purchases
            .Where(c => c.CompanyId == tenantContext.CompanyId)
            .MaxAsync(c => (int?)c.Number, ct) ?? 0;
        return max + 1;
    }

    private static PurchaseItem BuildItem(ItemCompraRequest req)
    {
        if (!Enum.TryParse<DestinoCompra>(req.Destination, out var destino))
            throw new AppException($"DestinoCompra inválido: {req.Destination}", 400);

        var total = req.Quantity * req.UnitPrice - req.Discount + req.AllocatedFreight + req.Taxes;

        return new PurchaseItem
        {
            Id = Guid.NewGuid(),
            ProductId = req.ProductId,
            Description = req.Description,
            Destination = destino,
            Quantity = req.Quantity,
            UnitPrice = req.UnitPrice,
            Discount = req.Discount,
            AllocatedFreight = req.AllocatedFreight,
            Taxes = req.Taxes,
            TotalAmount = total,
            FinancialCategory = req.FinancialCategory,
            CostCenter = req.CostCenter,
        };
    }

    private static CompraResponse ToResponse(Purchase c)
    {
        var itens = c.Items.Select(i => new ItemCompraResponse(
            i.Id, i.ProductId, i.Description, i.Destination.ToString(),
            i.Quantity, i.UnitPrice, i.Discount, i.AllocatedFreight,
            i.Taxes, i.TotalAmount, i.FinancialCategory, i.CostCenter)).ToList();

        ParcelamentoResumoResponse? parcelamentoResumo = c.InstallmentPlan is null ? null :
            new(c.InstallmentPlan.Id, c.InstallmentPlan.Description, c.InstallmentPlan.TotalAmount,
                c.InstallmentPlan.InstallmentCount, c.InstallmentPlan.Status.ToString());

        return new CompraResponse(
            c.Id, c.Number, c.Date, c.SupplierId,
            c.Supplier?.Name ?? "",
            c.PurchaseOrderId, c.PurchaseType, c.NoteNumber,
            c.PaymentTerms, c.PaymentMethod,
            c.Status.ToString(), c.TotalAmount, c.Notes,
            c.CreatedAt, itens, parcelamentoResumo);
    }
}
