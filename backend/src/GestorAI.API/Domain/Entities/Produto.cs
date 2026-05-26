using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Produto : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid CategoriaId { get; set; }
    public required string Nome { get; set; }
    public string? Descricao { get; set; }
    public decimal PrecoVenda { get; set; }
    public decimal CustoMedio { get; set; }
    public decimal EstoqueAtual { get; set; }
    public decimal EstoqueMinimo { get; set; }
    public string? CodigoBarras { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
    public TipoProduto Tipo { get; set; } = TipoProduto.Produto;
    public int? DuracaoMinutos { get; set; }
    public Categoria? Categoria { get; set; }
    public ICollection<ItemVenda> ItensVenda { get; set; } = [];
    public ICollection<MovimentacaoEstoque> Movimentacoes { get; set; } = [];
}
