using GestorAI.API.Shared.MultiTenancy;

namespace GestorAI.API.Domain.Entities;

public class Profissional : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public string? Telefone { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<DisponibilidadeSemanal> Disponibilidades { get; set; } = [];
    public ICollection<Agendamento> Agendamentos { get; set; } = [];
}
