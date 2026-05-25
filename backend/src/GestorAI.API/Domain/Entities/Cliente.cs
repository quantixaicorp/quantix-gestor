namespace GestorAI.API.Domain.Entities;

public class Cliente : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public required string Whatsapp { get; set; }
    public string? Email { get; set; }
    public string? Observacoes { get; set; }
    public DateTime DataCadastro { get; set; } = DateTime.UtcNow;
    public ICollection<Venda> Vendas { get; set; } = [];
}
