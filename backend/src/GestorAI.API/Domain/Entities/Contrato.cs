using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Contrato : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public int Numero { get; set; }
    public Guid ClienteId { get; set; }
    public required string Titulo { get; set; }
    public required string Objeto { get; set; }
    public TipoCobranca TipoCobranca { get; set; }
    public decimal Valor { get; set; }
    public DateOnly DataInicio { get; set; }
    public DateOnly? DataFim { get; set; }
    public Periodicidade Periodicidade { get; set; }
    public int DiaVencimento { get; set; }
    public ContratoStatus Status { get; set; } = ContratoStatus.Rascunho;
    public string? Observacao { get; set; }
    // Assinatura digital (ClickSign)
    public string? ClickSignDocKey { get; set; }
    public string? ClickSignStatus { get; set; }
    public string? ClickSignViewerUrl { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public ICollection<ContratoItem> Itens { get; set; } = [];
    public ICollection<Cobranca> Cobrancas { get; set; } = [];
}
