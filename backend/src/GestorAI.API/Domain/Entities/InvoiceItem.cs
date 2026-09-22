namespace GestorAI.API.Domain.Entities;

public class InvoiceItem : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid InvoiceId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Ncm { get; set; }
    public string? Cfop { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public Invoice? Invoice { get; set; }
}
