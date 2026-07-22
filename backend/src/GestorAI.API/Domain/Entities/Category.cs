namespace GestorAI.API.Domain.Entities;

public class Category : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public ICollection<Product> Products { get; set; } = [];
}
