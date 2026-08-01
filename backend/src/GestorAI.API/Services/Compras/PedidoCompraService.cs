using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Compras;

public class PedidoCompraService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<PedidoCompraResponse>> ListAsync(
        string? status, Guid? fornecedorId, CancellationToken ct)
    {
        var query = db.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.Items)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusPedidoCompra>(status, out var s))
            query = query.Where(p => p.Status == s);
        if (fornecedorId.HasValue)
            query = query.Where(p => p.SupplierId == fornecedorId.Value);

        var list = await query.OrderByDescending(p => p.Number).ToListAsync(ct);
        return list.Select(ToResponse).ToList();
    }

    public async Task<PedidoCompraResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.PurchaseOrders
            .Include(x => x.Supplier)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Pedido de compra não encontrado.", 404);
        return ToResponse(p);
    }

    public async Task<PedidoCompraResponse> CreateAsync(CreatePedidoCompraRequest req, CancellationToken ct)
    {
        var numero = await NextNumeroAsync(ct);

        var pedido = new PurchaseOrder
        {
            CompanyId = tenantContext.CompanyId,
            Number = numero,
            Date = req.Date,
            SupplierId = req.SupplierId,
            Notes = req.Notes,
            Status = StatusPedidoCompra.Rascunho,
        };

        foreach (var itemReq in req.Items)
        {
            pedido.Items.Add(new PurchaseOrderItem
            {
                CompanyId = tenantContext.CompanyId,
                PurchaseOrderId = pedido.Id,
                ProductId = itemReq.ProductId,
                Description = itemReq.Description,
                Quantity = itemReq.Quantity,
                EstimatedAmount = itemReq.EstimatedAmount,
            });
        }

        pedido.EstimatedAmount = pedido.Items.Sum(i => i.Quantity * i.EstimatedAmount);
        db.PurchaseOrders.Add(pedido);
        await db.SaveChangesAsync(ct);

        return await GetAsync(pedido.Id, ct);
    }

    public async Task<PedidoCompraResponse> UpdateAsync(Guid id, UpdatePedidoCompraRequest req, CancellationToken ct)
    {
        var pedido = await db.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("Pedido de compra não encontrado.", 404);

        if (pedido.Status is StatusPedidoCompra.RecebidoTotalmente or StatusPedidoCompra.Cancelado)
            throw new AppException("Este pedido não pode ser editado.", 400);

        pedido.SupplierId = req.SupplierId;
        pedido.Date = req.Date;
        pedido.Notes = req.Notes;

        db.PurchaseOrderItems.RemoveRange(pedido.Items);
        pedido.Items.Clear();

        foreach (var itemReq in req.Items)
        {
            pedido.Items.Add(new PurchaseOrderItem
            {
                CompanyId = tenantContext.CompanyId,
                PurchaseOrderId = pedido.Id,
                ProductId = itemReq.ProductId,
                Description = itemReq.Description,
                Quantity = itemReq.Quantity,
                EstimatedAmount = itemReq.EstimatedAmount,
            });
        }

        pedido.EstimatedAmount = pedido.Items.Sum(i => i.Quantity * i.EstimatedAmount);
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<CompraResponse> ConverterAsync(Guid id, CancellationToken ct)
    {
        var pedido = await db.PurchaseOrders
            .Include(p => p.Items)
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("Pedido de compra não encontrado.", 404);

        if (pedido.Status is StatusPedidoCompra.Cancelado or StatusPedidoCompra.RecebidoTotalmente)
            throw new AppException("Pedido não pode ser convertido.", 400);

        var numeroCompra = await db.Purchases
            .Where(c => c.CompanyId == tenantContext.CompanyId)
            .MaxAsync(c => (int?)c.Number, ct) ?? 0;

        var compra = new Purchase
        {
            CompanyId = tenantContext.CompanyId,
            Number = numeroCompra + 1,
            Date = DateTime.UtcNow.Date,
            SupplierId = pedido.SupplierId,
            PurchaseOrderId = pedido.Id,
            PurchaseType = "Mercadoria",
            PaymentTerms = "AVista",
            PaymentMethod = "PIX",
            Status = StatusCompra.Rascunho,
        };

        foreach (var item in pedido.Items)
        {
            compra.Items.Add(new PurchaseItem
            {
                CompanyId = tenantContext.CompanyId,
                PurchaseId = compra.Id,
                ProductId = item.ProductId,
                Description = item.Description,
                Destination = DestinoCompra.EstoqueParaVenda,
                Quantity = item.Quantity,
                UnitPrice = item.EstimatedAmount,
                TotalAmount = item.Quantity * item.EstimatedAmount,
            });
        }

        compra.TotalAmount = compra.Items.Sum(i => i.TotalAmount);
        db.Purchases.Add(compra);

        if (pedido.Status == StatusPedidoCompra.Rascunho || pedido.Status == StatusPedidoCompra.AguardandoAprovacao)
            pedido.Status = StatusPedidoCompra.Aprovado;

        await db.SaveChangesAsync(ct);

        var result = await db.Purchases
            .Include(c => c.Supplier)
            .Include(c => c.Items)
            .Include(c => c.InstallmentPlan)
            .FirstOrDefaultAsync(c => c.Id == compra.Id, ct);

        return CompraToResponse(result!);
    }

    public async Task<PedidoCompraResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var pedido = await db.PurchaseOrders.FindAsync([id], ct)
            ?? throw new AppException("Pedido de compra não encontrado.", 404);

        if (pedido.Status == StatusPedidoCompra.Cancelado)
            throw new AppException("Pedido já está cancelado.", 400);

        pedido.Status = StatusPedidoCompra.Cancelado;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    private async Task<int> NextNumeroAsync(CancellationToken ct)
    {
        var max = await db.PurchaseOrders
            .Where(p => p.CompanyId == tenantContext.CompanyId)
            .MaxAsync(p => (int?)p.Number, ct) ?? 0;
        return max + 1;
    }

    private static PedidoCompraResponse ToResponse(PurchaseOrder p) =>
        new(p.Id, p.Number, p.Date, p.SupplierId,
            p.Supplier?.Name ?? "",
            p.Status.ToString(), p.EstimatedAmount, p.Notes,
            p.CreatedAt,
            p.Items.Select(i => new ItemPedidoResponse(
                i.Id, i.ProductId, i.Description, i.Quantity, i.EstimatedAmount)).ToList());

    private static CompraResponse CompraToResponse(Purchase c)
    {
        var itens = c.Items.Select(i => new ItemCompraResponse(
            i.Id, i.ProductId, i.Description, i.Destination.ToString(),
            i.Quantity, i.UnitPrice, i.Discount, i.AllocatedFreight,
            i.Taxes, i.TotalAmount, i.FinancialCategory, i.CostCenter)).ToList();

        return new CompraResponse(
            c.Id, c.Number, c.Date, c.SupplierId,
            c.Supplier?.Name ?? "",
            c.PurchaseOrderId, c.PurchaseType, c.NoteNumber,
            c.PaymentTerms, c.PaymentMethod,
            c.Status.ToString(), c.TotalAmount, c.Notes,
            c.CreatedAt, itens, null);
    }
}
