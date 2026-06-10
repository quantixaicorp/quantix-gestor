using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class CategoriaLancamento : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public required string Nome { get; set; }
    public TipoLancamento Tipo { get; set; }
}
