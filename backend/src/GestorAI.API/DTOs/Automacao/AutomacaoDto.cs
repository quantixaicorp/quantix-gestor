using GestorAI.API.Domain.Enums;

namespace GestorAI.API.DTOs.Automacao;

public record AutomacaoLogResponse(
    Guid Id,
    DateTime EnviadoEm,
    string ClienteNome,
    string Referencia,
    AutomacaoTipoEvento TipoEvento,
    bool Sucesso,
    string? ErroMsg);

public record TestarConexaoRequest(string ApiUrl, string ApiKey);
