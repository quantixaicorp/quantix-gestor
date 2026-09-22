using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class ContractTemplate : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string Subject { get; set; }
    public TipoCobranca ChargeType { get; set; }
    public Periodicidade Frequency { get; set; }
    public int DueDay { get; set; } = 10;
    public decimal? DefaultAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ContractTemplateItem> Items { get; set; } = [];
}

public class ContractTemplateItem
{
    public Guid Id { get; set; }
    public Guid ContractTemplateId { get; set; }
    public required string Description { get; set; }
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
