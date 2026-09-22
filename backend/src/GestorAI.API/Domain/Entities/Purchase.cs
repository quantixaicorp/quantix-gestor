using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Purchase : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int Number { get; set; }
    public DateTime Date { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public required string PurchaseType { get; set; }
    public string? NoteNumber { get; set; }
    public required string PaymentTerms { get; set; }
    public required string PaymentMethod { get; set; }
    public StatusCompra Status { get; set; } = StatusCompra.Rascunho;
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Supplier? Supplier { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }
    public ICollection<PurchaseItem> Items { get; set; } = [];
    public InstallmentPlan? InstallmentPlan { get; set; }
}
