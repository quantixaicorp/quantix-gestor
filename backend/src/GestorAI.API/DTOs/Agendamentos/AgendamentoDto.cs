using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Agendamentos;

public record AgendamentoListItem(
    Guid Id,
    Guid ProfissionalId,
    string ProfissionalNome,
    string ClienteNome,
    string ServicoNome,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    AgendamentoStatus Status
);

public record AgendamentoResponse(
    Guid Id,
    string ProfissionalNome,
    string ClienteNome,
    string ClienteTelefone,
    Guid? ClienteId,
    string ServicoNome,
    int DuracaoMinutos,
    DateTime DataHoraInicio,
    DateTime DataHoraFim,
    AgendamentoStatus Status,
    string? Observacao,
    Guid? VendaId,
    DateTime CriadoEm
);

public record CriarAgendamentoRequest(
    Guid ProfissionalId,
    string ClienteNome,
    string ClienteTelefone,
    Guid? ClienteId,
    Guid ServicoId,
    DateTime DataHoraInicio,
    string? Observacao
);

public record AtualizarAgendamentoRequest(string? Observacao);

public record ConcluirResponse(Guid VendaId);
