using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Invoice : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SaleId { get; set; }
    public ModeloNF Model { get; set; }
    public int? Number { get; set; }
    public int? Series { get; set; }
    public string? AccessKey { get; set; }
    public StatusNF Status { get; set; } = StatusNF.Pendente;
    public string? FocusNfeId { get; set; }
    public string? FocusNfeRef { get; set; }
    public string? Protocol { get; set; }
    public string? CancellationProtocol { get; set; }
    public string? XmlUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Sale? Sale { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = [];
}
