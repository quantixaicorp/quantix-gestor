using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class AssinaturaCliente : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid PlanoAssinaturaId { get; set; }
    public Guid ContratoId { get; set; }
    public AssinaturaStatus Status { get; set; } = AssinaturaStatus.Ativa;
    public DateOnly DataInicio { get; set; }
    public DateOnly DataRenovacao { get; set; }
    public int CicloAtual { get; set; } = 1;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public PlanoAssinatura? Plano { get; set; }
    public Contrato? Contrato { get; set; }
}
