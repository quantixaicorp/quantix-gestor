using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GestorAI.API.Endpoints;

public static class BillingEndpoints
{
    public static void MapBilling(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/billing").RequireAuthorization();

        group.MapGet("/status", async (
            AppDbContext db, CancellationToken ct) =>
        {
            var cfg = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct);
            return Results.Ok(new
            {
                status = cfg?.StatusAssinatura ?? "SemPlano",
                proximaCobranca = cfg?.ProximaCobrancaEm,
            });
        });

        // Webhook Asaas — sem autenticação JWT
        app.MapPost("/webhook/asaas-billing", async (
            JsonElement body,
            BillingService svc, CancellationToken ct) =>
        {
            var assinaturaId = body.TryGetProperty("subscription", out var sub)
                ? sub.TryGetProperty("id", out var sid) ? sid.GetString() ?? "" : ""
                : "";
            var evento = body.TryGetProperty("event", out var ev) ? ev.GetString() ?? "" : "";
            if (!string.IsNullOrEmpty(assinaturaId))
                await svc.ProcessarWebhookAsync(assinaturaId, evento, ct);
            return Results.Ok();
        });
    }
}
