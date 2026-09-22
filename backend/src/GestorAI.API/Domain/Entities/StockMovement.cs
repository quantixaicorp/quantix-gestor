using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class StockMovement : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProductId { get; set; }
    public TipoMovimentacao Type { get; set; }
    public decimal Quantity { get; set; }
    public OrigemMovimentacao Source { get; set; }
    public Guid? ReferenceId { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public Product? Product { get; set; }
}
