// backend/src/GestorAI.API/DTOs/PublicBooking/PublicBookingDto.cs
namespace GestorAI.API.DTOs.PublicBooking;

public record PublicEmpresaInfo(
    string Name,
    string? LogoUrl,
    string? PrimaryColor,
    string? Description);

public record PublicServicoResponse(
    Guid Id,
    string Name,
    decimal Price,
    int? DurationMinutes);

public record PublicProfissionalResponse(
    Guid Id,
    string Name);

public record PublicDisponibilidadeResponse(
    List<int> DiasComDisponibilidade);

public record PublicCriarAgendamentoRequest(
    Guid ServiceId,
    Guid ProfessionalId,
    DateTime StartAt,
    string CustomerName,
    string CustomerPhone);

public record PublicAgendamentoConfirmado(
    Guid Id,
    string ServicoNome,
    string ProfessionalName,
    DateTime StartAt,
    DateTime EndAt,
    string? DepositPixQrCode = null,
    decimal? SinalValor = null);

public record ConfigurarBrandingRequest(
    string Slug,
    string? NomeExibicao,
    string? PrimaryColor,
    string? PublicDescription);
