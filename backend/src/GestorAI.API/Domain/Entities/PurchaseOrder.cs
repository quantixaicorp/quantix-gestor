using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class PurchaseOrder : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int Number { get; set; }
    public DateTime Date { get; set; }
    public Guid SupplierId { get; set; }
    public StatusPedidoCompra Status { get; set; } = StatusPedidoCompra.Rascunho;
    public decimal EstimatedAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Supplier? Supplier { get; set; }
    public ICollection<PurchaseOrderItem> Items { get; set; } = [];
}
