namespace GestorAI.API.Domain.Entities;

public class ItemPedidoCompra : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid PedidoCompraId { get; set; }
    public Guid? ProdutoId { get; set; }
    public required string Descricao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorEstimado { get; set; }
    public PedidoCompra? PedidoCompra { get; set; }
    public Produto? Produto { get; set; }
}
