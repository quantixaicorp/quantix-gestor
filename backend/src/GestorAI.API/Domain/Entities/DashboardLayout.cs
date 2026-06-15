namespace GestorAI.API.Domain.Entities;

public class DashboardLayout : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public string WidgetsJson { get; set; } = "[]";
    public DateTime AtualizadoEm { get; set; }
}
