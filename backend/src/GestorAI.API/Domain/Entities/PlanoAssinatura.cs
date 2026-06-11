using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class PlanoAssinatura : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public string? Descricao { get; set; }
    public string Nicho { get; set; } = "Personalizado";
    public decimal Preco { get; set; }
    public Periodicidade Periodicidade { get; set; }
    public bool Ativo { get; set; } = true;
    public bool MaisVendido { get; set; } = false;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public ICollection<PlanoAssinaturaItem> Itens { get; set; } = [];
    public ICollection<AssinaturaCliente> Assinantes { get; set; } = [];
}
