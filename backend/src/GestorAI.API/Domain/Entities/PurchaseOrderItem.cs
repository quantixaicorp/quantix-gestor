namespace GestorAI.API.Domain.Entities;

public class PurchaseOrderItem : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid? ProductId { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal EstimatedAmount { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Product? Product { get; set; }
}
