using GestorAI.API.Domain.Enums;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Endpoints;

public static class WebhooksEndpoints
{
    public static void MapWebhooks(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/webhooks/asaas", async (
            AsaasWebhookPayload payload,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (payload.Event is not ("PAYMENT_RECEIVED" or "PAYMENT_CONFIRMED"))
                return Results.Ok();

            var asaasId = payload.Payment?.Id;
            if (string.IsNullOrEmpty(asaasId))
                return Results.Ok();

            var cobranca = await db.Cobrancas
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.AsaasId == asaasId, ct);

            if (cobranca is null)
                return Results.Ok();

            if (cobranca.Status == CobrancaStatus.Pendente)
            {
                cobranca.Status = CobrancaStatus.Pago;
                cobranca.DataPagamento = DateTime.UtcNow;
                cobranca.FormaPagamento = payload.Payment?.BillingType switch
                {
                    "PIX" => FormaPagamento.Pix,
                    "BOLETO" => FormaPagamento.Outro,
                    "CREDIT_CARD" => FormaPagamento.Cartao,
                    _ => FormaPagamento.Outro,
                };
                await db.SaveChangesAsync(ct);
            }

            return Results.Ok();
        }).AllowAnonymous();
    }
}

public record AsaasWebhookPayload(string Event, AsaasWebhookPaymentData? Payment);

public record AsaasWebhookPaymentData(string Id, string? BillingType, string? Status);
