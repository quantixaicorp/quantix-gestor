using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class NotaFiscal : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid VendaId { get; set; }
    public ModeloNF Modelo { get; set; }
    public int? Numero { get; set; }
    public int? Serie { get; set; }
    public string? ChaveAcesso { get; set; }
    public StatusNF Status { get; set; } = StatusNF.Pendente;
    public string? FocusNfeId { get; set; }
    public string? FocusNfeRef { get; set; }
    public string? Protocolo { get; set; }
    public string? ProtocoloCancelamento { get; set; }
    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? MensagemErro { get; set; }
    public DateTime? AutorizadaEm { get; set; }
    public DateTime? CanceladaEm { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public Venda? Venda { get; set; }
    public ICollection<NotaFiscalItem> Itens { get; set; } = [];
}
