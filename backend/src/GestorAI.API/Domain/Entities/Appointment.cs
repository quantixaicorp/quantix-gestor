using GestorAI.API.Domain.Enums;

namespace GestorAI.API.Domain.Entities;

public class Appointment : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid ProfessionalId { get; set; }
    public required string CustomerName { get; set; }
    public required string CustomerPhone { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public AgendamentoStatus Status { get; set; } = AgendamentoStatus.Agendado;
    public string? Notes { get; set; }
    public bool DepositPaid { get; set; } = false;
    public string? DepositAsaasId { get; set; }
    public string? DepositPixQrCode { get; set; }
    public Guid? SaleId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Professional? Professional { get; set; }
    public Product? Service { get; set; }
    public Customer? Customer { get; set; }
}
