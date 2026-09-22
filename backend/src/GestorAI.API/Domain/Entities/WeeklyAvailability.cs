namespace GestorAI.API.Domain.Entities;

public class WeeklyAvailability
{
    public Guid Id { get; set; }
    public Guid ProfessionalId { get; set; }
    public int WeekDay { get; set; }  // 0=Sun ... 6=Sat
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    // Validity period (week, month, quarter, semester or year)
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public Professional? Professional { get; set; }
}
