using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Agendamentos;

public record AgendamentoListItem(
    Guid Id,
    Guid ProfessionalId,
    string ProfessionalName,
    string CustomerName,
    string ServicoNome,
    DateTime StartAt,
    DateTime EndAt,
    AgendamentoStatus Status
);

public record AgendamentoResponse(
    Guid Id,
    string ProfessionalName,
    string CustomerName,
    string CustomerPhone,
    Guid? CustomerId,
    string ServicoNome,
    int DurationMinutes,
    DateTime StartAt,
    DateTime EndAt,
    AgendamentoStatus Status,
    string? Notes,
    Guid? SaleId,
    DateTime CreatedAt
);

public record CriarAgendamentoRequest(
    Guid ProfessionalId,
    string CustomerName,
    string CustomerPhone,
    Guid? CustomerId,
    Guid ServiceId,
    DateTime StartAt,
    string? Notes
);

public record AtualizarAgendamentoRequest(string? Notes);

public record ConcluirResponse(Guid SaleId);
