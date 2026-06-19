using System.ComponentModel.DataAnnotations.Schema;
using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Venda : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? ClienteId { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    public StatusVenda Status { get; set; } = StatusVenda.Aberta;
    public decimal Subtotal { get; set; }
    public decimal Desconto { get; set; }
    public decimal Total { get; set; }
    public FormaPagamento FormaPagamento { get; set; }
    public int? Parcelas { get; set; }
    public string? Observacao { get; set; }
    public Cliente? Cliente { get; set; }
    public ICollection<ItemVenda> Itens { get; set; } = [];
    public Lancamento? Lancamento { get; set; }

    public Guid?   ProfissionalId   { get; set; }
    public string? ProfissionalNome { get; set; }
    [Column("observacao_os")]
    public string? ObservacaoOS     { get; set; }
}
