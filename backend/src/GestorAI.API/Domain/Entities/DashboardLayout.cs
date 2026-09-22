namespace GestorAI.API.Domain.Entities;

public class DashboardLayout : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public string WidgetsJson { get; set; } = "[]";
    public DateTime UpdatedAt { get; set; }
}
