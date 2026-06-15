using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class ItemCompra : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid CompraId { get; set; }
    public Guid? ProdutoId { get; set; }
    public required string Descricao { get; set; }
    public DestinoCompra DestinoCompra { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal FreteRateado { get; set; }
    public decimal Impostos { get; set; }
    public decimal ValorTotal { get; set; }
    public string? CategoriaFinanceira { get; set; }
    public string? CentroCusto { get; set; }
    public Compra? Compra { get; set; }
    public Produto? Produto { get; set; }
}
