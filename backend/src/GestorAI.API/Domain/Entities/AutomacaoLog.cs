using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class AutomacaoLog : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public Guid CobrancaId { get; set; }
    public AutomacaoTipoEvento TipoEvento { get; set; }
    public DateTime EnviadoEm { get; set; } = DateTime.UtcNow;
    public bool Sucesso { get; set; }
    public string? ErroMsg { get; set; }
    public Cobranca? Cobranca { get; set; }
}
