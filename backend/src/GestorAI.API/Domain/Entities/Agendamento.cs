using GestorAI.API.Domain.Enums;
using GestorAI.API.Shared.MultiTenancy;

namespace GestorAI.API.Domain.Entities;

public class Agendamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ProfissionalId { get; set; }
    public required string ClienteNome { get; set; }
    public required string ClienteTelefone { get; set; }
    public Guid? ClienteId { get; set; }
    public Guid ServicoId { get; set; }
    public DateTime DataHoraInicio { get; set; }
    public DateTime DataHoraFim { get; set; }
    public AgendamentoStatus Status { get; set; } = AgendamentoStatus.Agendado;
    public string? Observacao { get; set; }
    public Guid? VendaId { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Profissional? Profissional { get; set; }
    public Produto? Servico { get; set; }
    public Cliente? Cliente { get; set; }
}
