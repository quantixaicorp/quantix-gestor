using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Services.Cobrancas;

namespace GestorAI.API.Endpoints;

public static class CobrancasEndpoints
{
    public static void MapCobrancas(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cobrancas").RequireAuthorization();

        group.MapGet("/", async (
            string? status, Guid? clienteId, string? mes,
            CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, clienteId, mes, ct)));

        group.MapGet("/resumo", async (CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetResumoAsync(ct)));

        group.MapGet("/aging", async (
            CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAgingAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateCobrancaRequest req, CobrancaService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/cobrancas/{result.Id}", result);
        });

        group.MapPost("/{id:guid}/pagar", async (
            Guid id, PagarCobrancaRequest req, CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.PagarAsync(id, req, ct)));

        group.MapPost("/{id:guid}/cancelar", async (Guid id, CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapGet("/{id:guid}/whatsapp", async (Guid id, CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetWhatsappUrlAsync(id, ct)));

        group.MapPost("/{id:guid}/enviar-asaas", async (
            Guid id, EnviarAsaasRequest req, CobrancaService svc, CancellationToken ct) =>
            Results.Ok(await svc.EnviarAsaasAsync(id, req, ct)));
    }
}
