namespace GestorAI.API.DTOs.Agendamentos;

public record ProfissionalResponse(Guid Id, string Nome, string? Telefone, bool Ativo);

public record CriarProfissionalRequest(string Nome, string? Telefone);

public record AtualizarProfissionalRequest(string Nome, string? Telefone, bool Ativo);

public record DisponibilidadeItem(int DiaSemana, string HoraInicio, string HoraFim);

public record DisponibilidadePeriodoResponse(
    DateOnly DataInicio,
    DateOnly DataFim,
    List<DisponibilidadeItem> Faixas);

public record SalvarDisponibilidadeRequest(
    DateOnly DataInicio,
    DateOnly DataFim,
    List<DisponibilidadeItem> Faixas);

public record CriarBloqueioRequest(Guid? ProfissionalId, DateTime DataInicio, DateTime DataFim, string? Motivo);

public record BloqueioResponse(
    Guid Id,
    Guid? ProfissionalId,
    string? ProfissionalNome,
    DateTime DataInicio,
    DateTime DataFim,
    string? Motivo
);
