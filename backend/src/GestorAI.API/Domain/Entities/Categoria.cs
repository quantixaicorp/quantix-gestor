namespace GestorAI.API.Domain.Entities;

public class Categoria : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public ICollection<Produto> Produtos { get; set; } = [];
}
