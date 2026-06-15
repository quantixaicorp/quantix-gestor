using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Compra : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public int Numero { get; set; }
    public DateTime Data { get; set; }
    public Guid FornecedorId { get; set; }
    public Guid? PedidoCompraId { get; set; }
    public required string TipoCompra { get; set; }
    public string? NumeroNota { get; set; }
    public required string CondicaoPagamento { get; set; }
    public required string FormaPagamento { get; set; }
    public StatusCompra Status { get; set; } = StatusCompra.Rascunho;
    public decimal ValorTotal { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CriadaEm { get; set; } = DateTime.UtcNow;
    public Fornecedor? Fornecedor { get; set; }
    public PedidoCompra? PedidoCompra { get; set; }
    public ICollection<ItemCompra> Itens { get; set; } = [];
    public Parcelamento? Parcelamento { get; set; }
}
