using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Cobranca : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ClienteId { get; set; }
    public Guid? ContratoId { get; set; }
    public required string Referencia { get; set; }
    public decimal Valor { get; set; }
    public DateOnly DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public CobrancaStatus Status { get; set; } = CobrancaStatus.Pendente;
    public FormaPagamento? FormaPagamento { get; set; }
    public string? Observacao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public Contrato? Contrato { get; set; }
}
