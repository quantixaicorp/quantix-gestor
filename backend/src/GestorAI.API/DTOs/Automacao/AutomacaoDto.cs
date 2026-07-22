using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Automacao;

public record AutomacaoLogResponse(
    Guid Id,
    DateTime EnviadoEm,
    string CustomerName,
    string Reference,
    AutomacaoTipoEvento EventType,
    bool Success,
    string? ErrorMessage);

public record TestarConexaoRequest(string ApiUrl, string ApiKey);
