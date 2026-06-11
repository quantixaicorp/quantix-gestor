using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class PlanoAssinaturaItem
{
    public Guid Id { get; set; }
    public Guid PlanoAssinaturaId { get; set; }
    public required string Descricao { get; set; }
    public Guid? ServicoId { get; set; }
    public int QuantidadePorCiclo { get; set; } = 1;
    public TipoItemPlano Tipo { get; set; }
    public decimal? PercentualDesconto { get; set; }
    public PlanoAssinatura? Plano { get; set; }
}
