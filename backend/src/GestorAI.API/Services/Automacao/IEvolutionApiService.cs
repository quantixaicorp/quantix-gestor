namespace GestorAI.API.Services.Automacao;

public interface IEvolutionApiService
{
    Task<bool> EnviarMensagemAsync(
        string apiUrl,
        string apiKey,
        string instance,
        string whatsapp,
        string mensagem,
        CancellationToken ct = default);
}
