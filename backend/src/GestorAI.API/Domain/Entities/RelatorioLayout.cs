namespace GestorAI.API.Domain.Entities;

public class RelatorioLayout : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmpresaId { get; set; }
    public string TabsJson { get; set; } = "[]";
    public DateTime AtualizadoEm { get; set; }
}
