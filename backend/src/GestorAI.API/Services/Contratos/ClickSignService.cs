using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GestorAI.API.Services.Contratos;

public class ClickSignService(IHttpClientFactory httpFactory)
{
    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private HttpClient CreateClient(bool sandbox)
    {
        var baseUrl = sandbox
            ? "https://sandbox.clicksign.com"
            : "https://app.clicksign.com";
        var client = httpFactory.CreateClient();
        client.BaseAddress = new Uri(baseUrl);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    }

    public async Task<ClickSignDocResult> CriarDocumentoAsync(
        string apiKey, bool sandbox,
        string nomeArquivo, byte[] pdfBytes,
        CancellationToken ct)
    {
        var client = CreateClient(sandbox);
        var base64 = Convert.ToBase64String(pdfBytes);

        var body = new
        {
            document = new
            {
                path = $"/{nomeArquivo}",
                content_base64 = $"data:application/pdf;base64,{base64}",
                deadline_at = (string?)null,
                auto_close = true,
                locale = "pt-BR",
                sequence_enabled = false,
            }
        };

        var url = $"/api/v1/documents?access_token={Uri.EscapeDataString(apiKey)}";
        var response = await client.PostAsJsonAsync(url, body, _json, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var docKey = json.GetProperty("document").GetProperty("key").GetString()!;
        var viewerUrl = json.GetProperty("document").GetProperty("url").GetString() ?? "";

        return new ClickSignDocResult(docKey, viewerUrl);
    }

    public async Task AdicionarSignatarioAsync(
        string apiKey, bool sandbox,
        string docKey, string nomeSignatario, string emailSignatario,
        CancellationToken ct)
    {
        var client = CreateClient(sandbox);

        var signerBody = new
        {
            signer = new
            {
                email = emailSignatario,
                phone_number = (string?)null,
                auth_type = "email",
                delivery = "email",
                name = nomeSignatario,
                has_documentation = false,
            }
        };

        var createUrl = $"/api/v1/signers?access_token={Uri.EscapeDataString(apiKey)}";
        var signerResp = await client.PostAsJsonAsync(createUrl, signerBody, _json, ct);
        signerResp.EnsureSuccessStatusCode();
        var signerJson = await signerResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var signerKey = signerJson.GetProperty("signer").GetProperty("key").GetString()!;

        var listBody = new
        {
            list = new
            {
                document_key = docKey,
                signer_key = signerKey,
                sign_as = "sign",
                refusable = false,
                message = "Por favor, assine o contrato.",
            }
        };

        var listUrl = $"/api/v1/lists?access_token={Uri.EscapeDataString(apiKey)}";
        var listResp = await client.PostAsJsonAsync(listUrl, listBody, _json, ct);
        listResp.EnsureSuccessStatusCode();
    }

    public async Task<string> ObterStatusAsync(
        string apiKey, bool sandbox, string docKey, CancellationToken ct)
    {
        var client = CreateClient(sandbox);
        var url = $"/api/v1/documents/{docKey}?access_token={Uri.EscapeDataString(apiKey)}";
        var resp = await client.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var status = json.GetProperty("document").GetProperty("status").GetString();
        return status == "closed" ? "Assinado" : "Pendente";
    }
}

public record ClickSignDocResult(string DocKey, string ViewerUrl);
