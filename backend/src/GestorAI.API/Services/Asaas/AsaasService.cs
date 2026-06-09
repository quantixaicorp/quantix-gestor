using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace GestorAI.API.Services.Asaas;

public class AsaasService(IHttpClientFactory httpClientFactory)
{
    private HttpClient Client(string apiKey, bool sandbox)
    {
        var client = httpClientFactory.CreateClient();
        var baseUrl = sandbox
            ? "https://sandbox.asaas.com/api/v3"
            : "https://api.asaas.com/api/v3";
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("access_token", apiKey);
        return client;
    }

    public async Task<string> GetOrCreateCustomerAsync(
        string apiKey, bool sandbox,
        string nome, string? cpfCnpj, CancellationToken ct)
    {
        var client = Client(apiKey, sandbox);

        if (!string.IsNullOrWhiteSpace(cpfCnpj))
        {
            var search = await client.GetFromJsonAsync<AsaasListResponse<AsaasCustomer>>(
                $"/customers?cpfCnpj={Uri.EscapeDataString(cpfCnpj)}", ct);
            if (search?.Data?.Count > 0)
                return search.Data[0].Id;
        }

        var body = new { name = nome, cpfCnpj = cpfCnpj ?? "" };
        var res = await client.PostAsJsonAsync("/customers", body, ct);
        res.EnsureSuccessStatusCode();
        var created = await res.Content.ReadFromJsonAsync<AsaasCustomer>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Resposta inválida do Asaas ao criar cliente");
        return created.Id;
    }

    public async Task<AsaasPaymentResult> CreatePaymentAsync(
        string apiKey, bool sandbox,
        string customerId, decimal valor,
        DateOnly vencimento, string descricao,
        string billingType, CancellationToken ct)
    {
        var client = Client(apiKey, sandbox);
        var body = new
        {
            customer = customerId,
            billingType,
            value = valor,
            dueDate = vencimento.ToString("yyyy-MM-dd"),
            description = descricao,
        };
        var res = await client.PostAsJsonAsync("/payments", body, ct);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<AsaasPaymentResult>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Resposta inválida do Asaas ao criar pagamento");
    }
}

public record AsaasCustomer([property: JsonPropertyName("id")] string Id);

public record AsaasListResponse<T>([property: JsonPropertyName("data")] List<T>? Data);

public record AsaasPixQrCode(
    [property: JsonPropertyName("payload")] string? Payload,
    [property: JsonPropertyName("encodedImage")] string? EncodedImage);

public record AsaasPaymentResult(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("invoiceUrl")] string? InvoiceUrl,
    [property: JsonPropertyName("bankSlipUrl")] string? BankSlipUrl,
    [property: JsonPropertyName("pixQrCode")] AsaasPixQrCode? PixQrCode);
