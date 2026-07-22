namespace GestorAI.API.Domain.Entities;

public class ScheduleBlock : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? ProfessionalId { get; set; }  // null = bloqueia todos
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public Professional? Professional { get; set; }
}
