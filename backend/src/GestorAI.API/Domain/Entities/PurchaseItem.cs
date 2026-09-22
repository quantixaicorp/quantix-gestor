using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class PurchaseItem : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid PurchaseId { get; set; }
    public Guid? ProductId { get; set; }
    public required string Description { get; set; }
    public DestinoCompra Destination { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal AllocatedFreight { get; set; }
    public decimal Taxes { get; set; }
    public decimal TotalAmount { get; set; }
    public string? FinancialCategory { get; set; }
    public string? CostCenter { get; set; }
    public Purchase? Purchase { get; set; }
    public Product? Product { get; set; }
}
