using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class PedidoCompra : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public int Numero { get; set; }
    public DateTime Data { get; set; }
    public Guid FornecedorId { get; set; }
    public StatusPedidoCompra Status { get; set; } = StatusPedidoCompra.Rascunho;
    public decimal ValorEstimado { get; set; }
    public string? Observacoes { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public Fornecedor? Fornecedor { get; set; }
    public ICollection<ItemPedidoCompra> Itens { get; set; } = [];
}
