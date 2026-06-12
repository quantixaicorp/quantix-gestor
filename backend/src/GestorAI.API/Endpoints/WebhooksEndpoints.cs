using System.Security.Cryptography;
using System.Text;
using GestorAI.API.Services.Cobrancas;

namespace GestorAI.API.Endpoints;

public static class WebhooksEndpoints
{
    public static void MapWebhooks(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/webhooks/asaas", async (
            HttpContext ctx,
            AsaasWebhookPayload payload,
            CobrancaService cobrancaService,
            IConfiguration config,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            // Autentica a origem do webhook. O Asaas envia o token configurado no
            // painel no cabeçalho "asaas-access-token". Fail-closed: sem segredo
            // configurado, nenhum webhook é aceito.
            var expected = config["Asaas:WebhookToken"];
            if (string.IsNullOrWhiteSpace(expected))
            {
                loggerFactory.CreateLogger("AsaasWebhook")
                    .LogError("Asaas:WebhookToken não configurado — webhook rejeitado.");
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            var provided = ctx.Request.Headers["asaas-access-token"].ToString();
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected)))
                return Results.Unauthorized();

            if (payload.Event is not ("PAYMENT_RECEIVED" or "PAYMENT_CONFIRMED"))
                return Results.Ok();

            var asaasId = payload.Payment?.Id;
            if (string.IsNullOrEmpty(asaasId))
                return Results.Ok();

            await cobrancaService.ConfirmarPagamentoAsaasAsync(asaasId, payload.Payment?.BillingType, ct);
            return Results.Ok();
        }).AllowAnonymous();
    }
}

public record AsaasWebhookPayload(string Event, AsaasWebhookPaymentData? Payment);
public record AsaasWebhookPaymentData(string Id, string? BillingType, string? Status);
