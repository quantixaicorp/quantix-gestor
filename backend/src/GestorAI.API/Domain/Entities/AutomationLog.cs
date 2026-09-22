using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class AutomationLog
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ChargeId { get; set; }
    public AutomacaoTipoEvento EventType { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
