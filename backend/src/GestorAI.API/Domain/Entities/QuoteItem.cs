using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class QuoteItem
{
    public Guid Id { get; set; }
    public Guid QuoteId { get; set; }
    public OrcamentoItemTipo Type { get; set; }
    public Guid? ProductId { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Quote? Quote { get; set; }
    public Product? Product { get; set; }
}
