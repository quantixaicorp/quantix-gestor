using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Parcelamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid? CompraId { get; set; }
    public required string Descricao { get; set; }
    public decimal ValorTotal { get; set; }
    public int QtdParcelas { get; set; }
    public StatusParcelamento Status { get; set; } = StatusParcelamento.EmAberto;
    public Compra? Compra { get; set; }
    public ICollection<Lancamento> Parcelas { get; set; } = [];
}
