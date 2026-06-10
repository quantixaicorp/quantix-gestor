using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Asaas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace GestorAI.API.Services;

public class BillingService(
    AppDbContext db,
    AsaasService asaasService,
    IConfiguration config)
{
    private string ApiKey => config["SaaS:AsaasMarketplaceApiKey"] ?? "";
    private bool Sandbox => bool.Parse(config["SaaS:AsaasMarketplaceSandbox"] ?? "true");
    private string BaseUrl => Sandbox
        ? "https://sandbox.asaas.com/api/v3"
        : "https://api.asaas.com/api/v3";

    public async Task<string> CriarAssinaturaAsync(
        Guid empresaId, string nomeEmpresa, string emailEmpresa,
        decimal valor, CancellationToken ct)
    {
        var configEmpresa = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId, ct)
            ?? throw new Exception("Configuração da empresa não encontrada.");

        var clienteId = configEmpresa.AsaasClienteIdSaaS
            ?? await asaasService.GetOrCreateCustomerAsync(
                ApiKey, Sandbox, nomeEmpresa, null, ct);

        var assinaturaId = await CriarAssinaturaAsaasAsync(clienteId, valor, ct);

        configEmpresa.AsaasClienteIdSaaS = clienteId;
        configEmpresa.AssinaturaAsaasId = assinaturaId;
        configEmpresa.StatusAssinatura = "Ativo";
        configEmpresa.ProximaCobrancaEm = DateTime.UtcNow.AddMonths(1);
        await db.SaveChangesAsync(ct);

        return assinaturaId;
    }

    public async Task CancelarAssinaturaAsync(Guid empresaId, CancellationToken ct)
    {
        var configEmpresa = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId, ct);

        if (configEmpresa?.AssinaturaAsaasId is null) return;

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("access_token", ApiKey);
        await http.DeleteAsync($"{BaseUrl}/subscriptions/{configEmpresa.AssinaturaAsaasId}", ct);

        configEmpresa.StatusAssinatura = "Cancelado";
        await db.SaveChangesAsync(ct);
    }

    public async Task ProcessarWebhookAsync(string assinaturaId, string evento, CancellationToken ct)
    {
        var configEmpresa = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.AssinaturaAsaasId == assinaturaId, ct);

        if (configEmpresa is null) return;

        configEmpresa.StatusAssinatura = evento switch
        {
            "PAYMENT_RECEIVED" or "PAYMENT_CONFIRMED" => "Ativo",
            "PAYMENT_OVERDUE" => "Inadimplente",
            "SUBSCRIPTION_DELETED" => "Cancelado",
            _ => configEmpresa.StatusAssinatura,
        };

        if (evento == "PAYMENT_CONFIRMED")
            configEmpresa.ProximaCobrancaEm = DateTime.UtcNow.AddMonths(1);

        await db.SaveChangesAsync(ct);
    }

    private async Task<string> CriarAssinaturaAsaasAsync(string clienteId, decimal valor, CancellationToken ct)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("access_token", ApiKey);

        var body = JsonSerializer.Serialize(new
        {
            customer = clienteId,
            billingType = "BOLETO",
            value = valor,
            nextDueDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
            cycle = "MONTHLY",
            description = "Assinatura GestorAI",
        });

        var resp = await http.PostAsync($"{BaseUrl}/subscriptions",
            new StringContent(body, System.Text.Encoding.UTF8, "application/json"), ct);
        resp.EnsureSuccessStatusCode();

        var json = await JsonSerializer.DeserializeAsync<JsonElement>(
            await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return json.GetProperty("id").GetString()!;
    }
}
