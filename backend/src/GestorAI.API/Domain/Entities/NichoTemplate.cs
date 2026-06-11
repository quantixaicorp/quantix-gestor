using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class NichoTemplate
{
    public Guid Id { get; set; }
    public required string Nicho { get; set; }
    public required string NomePlano { get; set; }
    public string? Descricao { get; set; }
    public decimal PrecoSugerido { get; set; }
    public bool MaisVendido { get; set; }
    public Periodicidade Periodicidade { get; set; }
    public ICollection<NichoTemplateItem> Itens { get; set; } = [];
}

public class NichoTemplateItem
{
    public Guid Id { get; set; }
    public Guid NichoTemplateId { get; set; }
    public required string Descricao { get; set; }
    public int QuantidadePorCiclo { get; set; }
    public TipoItemPlano Tipo { get; set; }
    public decimal? PercentualDesconto { get; set; }
    public NichoTemplate? Template { get; set; }
}
