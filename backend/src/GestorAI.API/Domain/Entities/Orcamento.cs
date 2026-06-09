using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Orcamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? ClienteId { get; set; }
    public int Numero { get; set; }
    public required string Titulo { get; set; }
    public DateTime DataValidade { get; set; }
    public OrcamentoStatus Status { get; set; } = OrcamentoStatus.Rascunho;
    public string? Observacao { get; set; }
    public Guid? VendaId { get; set; }
    public Guid? TokenPublico { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Cliente? Cliente { get; set; }
    public ICollection<OrcamentoItem> Itens { get; set; } = [];
}
