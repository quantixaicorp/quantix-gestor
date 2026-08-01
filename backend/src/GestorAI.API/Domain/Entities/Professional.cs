namespace GestorAI.API.Domain.Entities;

public class Professional : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<WeeklyAvailability> Availabilities { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}
