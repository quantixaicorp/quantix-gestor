using GestorAI.API.Shared.MultiTenancy;

namespace GestorAI.API.Domain.Entities;

public class BloqueioAgenda : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? ProfissionalId { get; set; }  // null = bloqueia todos
    public DateTime DataInicio { get; set; }
    public DateTime DataFim { get; set; }
    public string? Motivo { get; set; }
    public Profissional? Profissional { get; set; }
}
