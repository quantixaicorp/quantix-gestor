using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Product : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CategoryId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal SalePrice { get; set; }
    public decimal AverageCost { get; set; }
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public TipoProduto Type { get; set; } = TipoProduto.Produto;
    public int? DurationMinutes { get; set; }
    public Category? Category { get; set; }
    public ICollection<SaleItem> SaleItems { get; set; } = [];
    public ICollection<StockMovement> StockMovements { get; set; } = [];
}
