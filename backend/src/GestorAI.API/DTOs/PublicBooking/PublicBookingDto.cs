// backend/src/GestorAI.API/DTOs/PublicBooking/PublicBookingDto.cs
namespace GestorAI.API.DTOs.PublicBooking;

public record PublicEmpresaInfo(
    string Nome,
    string? LogoUrl,
    string? CorPrimaria,
    string? Descricao);

public record PublicServicoResponse(
    Guid Id,
    string Nome,
    decimal Preco,
    int? DuracaoMinutos);

public record PublicProfissionalResponse(
    Guid Id,
    string Nome);

public record PublicDisponibilidadeResponse(
    List<int> DiasComDisponibilidade);

public record PublicCriarAgendamentoRequest(
    Guid ServicoId,
    Guid ProfissionalId,
    DateTime DataHoraInicio,
    string ClienteNome,
    string ClienteTelefone);

public record PublicAgendamentoConfirmado(
    Guid Id,
    string ServicoNome,
    string ProfissionalNome,
    DateTime DataHoraInicio,
    DateTime DataHoraFim);

public record ConfigurarBrandingRequest(
    string Slug,
    string? NomeExibicao,
    string? CorPrimaria,
    string? DescricaoPublica);
