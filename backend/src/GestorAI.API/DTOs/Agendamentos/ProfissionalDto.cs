namespace GestorAI.API.DTOs.Agendamentos;

public record ProfissionalResponse(Guid Id, string Name, string? Phone, bool IsActive);

public record CriarProfissionalRequest(string Name, string? Phone);

public record AtualizarProfissionalRequest(string Name, string? Phone, bool IsActive);

public record DisponibilidadeItem(int WeekDay, string StartTime, string EndTime);

public record DisponibilidadePeriodoResponse(
    DateOnly StartDate,
    DateOnly EndDate,
    List<DisponibilidadeItem> Faixas);

public record SalvarDisponibilidadeRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    List<DisponibilidadeItem> Faixas);

public record CriarBloqueioRequest(Guid? ProfessionalId, DateTime StartDate, DateTime EndDate, string? Reason);

public record BloqueioResponse(
    Guid Id,
    Guid? ProfessionalId,
    string? ProfessionalName,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason
);
