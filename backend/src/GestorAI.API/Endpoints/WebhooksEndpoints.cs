using GestorAI.API.Services.Cobrancas;

namespace GestorAI.API.Endpoints;

public static class WebhooksEndpoints
{
    public static void MapWebhooks(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/webhooks/asaas", async (
            AsaasWebhookPayload payload,
            CobrancaService cobrancaService,
            CancellationToken ct) =>
        {
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
