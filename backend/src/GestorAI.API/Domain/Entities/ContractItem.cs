namespace GestorAI.API.Domain.Entities;

public class ContractItem
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
