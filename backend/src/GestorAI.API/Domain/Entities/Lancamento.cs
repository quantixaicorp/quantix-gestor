using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Lancamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public TipoLancamento Tipo { get; set; }
    public required string Descricao { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataVencimento { get; set; }
    public DateTime? DataPagamento { get; set; }
    public StatusLancamento Status { get; set; } = StatusLancamento.Pendente;
    public required string Categoria { get; set; }
    public Guid? VendaId { get; set; }
    public string? Observacao { get; set; }
    public Venda? Venda { get; set; }
    public Guid? ParcelamentoId { get; set; }
    public int? NumeroParcela { get; set; }
    public Parcelamento? Parcelamento { get; set; }
}
