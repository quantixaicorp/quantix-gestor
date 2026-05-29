namespace GestorAI.API.Domain.Entities;

public class NotaFiscalItem : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid NotaFiscalId { get; set; }
    public string NomeProduto { get; set; } = string.Empty;
    public string? Ncm { get; set; }
    public string? Cfop { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Total { get; set; }
    public NotaFiscal? NotaFiscal { get; set; }
}
