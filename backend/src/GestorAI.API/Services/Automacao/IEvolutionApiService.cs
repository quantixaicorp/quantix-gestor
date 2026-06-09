namespace GestorAI.API.Services.Automacao;

public interface IEvolutionApiService
{
    Task<bool> EnviarMensagemAsync(
        string apiUrl, string apiKey, string instance,
        string phone, string text, CancellationToken ct);

    Task<bool> TestarConexaoAsync(
        string apiUrl, string apiKey, CancellationToken ct);
}
