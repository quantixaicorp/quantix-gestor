namespace GestorAI.API.Domain.Entities;

public class ItemVenda
{
    public Guid Id { get; set; }
    public Guid VendaId { get; set; }
    public Guid ProdutoId { get; set; }
    public decimal Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public Venda? Venda { get; set; }
    public Produto? Produto { get; set; }
}
