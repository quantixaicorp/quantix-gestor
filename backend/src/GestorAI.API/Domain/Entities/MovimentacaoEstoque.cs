using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class MovimentacaoEstoque : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid ProdutoId { get; set; }
    public TipoMovimentacao Tipo { get; set; }
    public decimal Quantidade { get; set; }
    public OrigemMovimentacao Origem { get; set; }
    public Guid? ReferenciaId { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
    public string? Observacao { get; set; }
    public Produto? Produto { get; set; }
}
