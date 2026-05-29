namespace GestorAI.API.Domain.Entities;

public class ContratoItem
{
    public Guid Id { get; set; }
    public Guid ContratoId { get; set; }
    public required string Descricao { get; set; }
    public decimal Quantidade { get; set; }
    public decimal ValorUnitario { get; set; }
}
