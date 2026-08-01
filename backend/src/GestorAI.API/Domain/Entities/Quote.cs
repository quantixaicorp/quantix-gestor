using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Quote : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? CustomerId { get; set; }
    public int Number { get; set; }
    public required string Title { get; set; }
    public DateTime ExpirationDate { get; set; }
    public OrcamentoStatus Status { get; set; } = OrcamentoStatus.Rascunho;
    public string? Notes { get; set; }
    public Guid? SaleId { get; set; }
    public Guid? PublicToken { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Customer? Customer { get; set; }
    public ICollection<QuoteItem> Items { get; set; } = [];
}
