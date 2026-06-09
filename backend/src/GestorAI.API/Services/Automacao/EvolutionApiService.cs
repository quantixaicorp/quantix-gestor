using System.Net.Http.Json;

namespace GestorAI.API.Services.Automacao;

public class EvolutionApiService(IHttpClientFactory httpClientFactory) : IEvolutionApiService
{
    public async Task<bool> EnviarMensagemAsync(
        string apiUrl, string apiKey, string instance,
        string phone, string text, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("apikey", apiKey);
        var body = new { number = $"55{phone}", text };
        var res = await client.PostAsJsonAsync($"{apiUrl.TrimEnd('/')}/message/sendText/{instance}", body, ct);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> TestarConexaoAsync(
        string apiUrl, string apiKey, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("apikey", apiKey);
        var res = await client.GetAsync($"{apiUrl.TrimEnd('/')}/instance/fetchInstances", ct);
        return res.IsSuccessStatusCode;
    }
}
