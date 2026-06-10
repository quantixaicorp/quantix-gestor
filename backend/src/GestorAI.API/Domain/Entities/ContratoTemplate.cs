using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class ContratoTemplate : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public required string Objeto { get; set; }
    public TipoCobranca TipoCobranca { get; set; }
    public Periodicidade Periodicidade { get; set; }
    public int DiaVencimento { get; set; } = 10;
    public decimal? ValorPadrao { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<ContratoTemplateItem> Itens { get; set; } = [];
}

public class ContratoTemplateItem
{
    public Guid Id { get; set; }
    public Guid ContratoTemplateId { get; set; }
    public required string Descricao { get; set; }
    public decimal Quantidade { get; set; } = 1;
    public decimal ValorUnitario { get; set; }
}
