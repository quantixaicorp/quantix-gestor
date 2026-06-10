namespace GestorAI.API.Domain.Entities;

public class EmpresaPlano
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid PlanoSaaSId { get; set; }
    public DateTime InicioEm { get; set; } = DateTime.UtcNow;
    public DateTime? FimEm { get; set; }
    public bool Ativo { get; set; } = true;
    public PlanoSaaS? Plano { get; set; }
}
