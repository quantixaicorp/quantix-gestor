namespace GestorAI.API.Domain.Entities;

public class DisponibilidadeSemanal
{
    public Guid Id { get; set; }
    public Guid ProfissionalId { get; set; }
    public int DiaSemana { get; set; }  // 0=Dom ... 6=Sab
    public TimeSpan HoraInicio { get; set; }
    public TimeSpan HoraFim { get; set; }
    // Período de vigência (semana, mês, trimestre, semestre ou ano)
    public DateOnly DataInicio { get; set; }
    public DateOnly DataFim { get; set; }
    public Profissional? Profissional { get; set; }
}
