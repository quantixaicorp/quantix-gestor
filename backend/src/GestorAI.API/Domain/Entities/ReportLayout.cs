namespace GestorAI.API.Domain.Entities;

public class ReportLayout : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string TabsJson { get; set; } = "[]";
    public DateTime UpdatedAt { get; set; }
}
