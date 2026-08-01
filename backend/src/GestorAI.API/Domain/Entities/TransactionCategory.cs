using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class TransactionCategory : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public TipoLancamento Type { get; set; }
}
