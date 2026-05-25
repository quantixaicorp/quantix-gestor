using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class OrcamentoItem
{
    public Guid Id { get; set; }
    public Guid OrcamentoId { get; set; }
    public OrcamentoItemTipo Tipo { get; set; }
    public Guid? ProdutoId { get; set; }
    public required string Descricao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public Orcamento? Orcamento { get; set; }
    public Produto? Produto { get; set; }
}
