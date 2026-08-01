using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Contract : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public int Number { get; set; }
    public Guid CustomerId { get; set; }
    public required string Title { get; set; }
    public required string Subject { get; set; }
    public TipoCobranca ChargeType { get; set; }
    public decimal Amount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Periodicidade Frequency { get; set; }
    public int DueDay { get; set; }
    public ContratoStatus Status { get; set; } = ContratoStatus.Rascunho;
    public string? Notes { get; set; }
    public Guid? CustomerSubscriptionId { get; set; }
    // Digital signature (ClickSign)
    public string? ClickSignDocKey { get; set; }
    public string? ClickSignStatus { get; set; }
    public string? ClickSignViewerUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Customer? Customer { get; set; }
    public ICollection<ContractItem> Items { get; set; } = [];
    public ICollection<Charge> Charges { get; set; } = [];
}
