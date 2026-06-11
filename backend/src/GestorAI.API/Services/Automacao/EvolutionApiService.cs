using System.Net.Http.Json;
using GestorAI.API.Shared.Net;

namespace GestorAI.API.Services.Automacao;

public class EvolutionApiService(IHttpClientFactory httpClientFactory) : IEvolutionApiService
{
    public async Task<bool> EnviarMensagemAsync(
        string apiUrl, string apiKey, string instance,
        string phone, string text, CancellationToken ct)
    {
        var baseUri = OutboundUrlGuard.EnsurePublicHttp(apiUrl);
        var client = CreateClient(apiKey);
        var body = new { number = NormalizePhone(phone), text };
        var res = await client.PostAsJsonAsync(
            $"{baseUri.AbsoluteUri.TrimEnd('/')}/message/sendText/{instance}", body, ct);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> TestarConexaoAsync(
        string apiUrl, string apiKey, CancellationToken ct)
    {
        var baseUri = OutboundUrlGuard.EnsurePublicHttp(apiUrl);
        var client = CreateClient(apiKey);
        var res = await client.GetAsync(
            $"{baseUri.AbsoluteUri.TrimEnd('/')}/instance/fetchInstances", ct);
        return res.IsSuccessStatusCode;
    }

    private HttpClient CreateClient(string apiKey)
    {
        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("apikey", apiKey);
        return client;
    }

    private static string NormalizePhone(string phone)
    {
        var digits = phone.TrimStart('+');
        if (digits.StartsWith("55") && digits.Length > 11)
            return digits;
        return $"55{digits}";
    }
}
