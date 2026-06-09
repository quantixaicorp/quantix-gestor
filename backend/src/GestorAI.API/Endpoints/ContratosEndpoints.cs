using GestorAI.API.DTOs.Contratos;
using GestorAI.API.Services.Contratos;

namespace GestorAI.API.Endpoints;

public static class ContratosEndpoints
{
    public static void MapContratos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contratos").RequireAuthorization();

        group.MapGet("/", async (string? status, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, ct)));

        group.MapGet("/vencendo", async (
            int? dias, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListVencendoAsync(dias ?? 30, ct)));

        group.MapGet("/{id:guid}", async (Guid id, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateContratoRequest req, ContratoService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/contratos/{result.Id}", result);
        });

        group.MapPost("/{id:guid}/ativar", async (Guid id, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.AtivarAsync(id, ct)));

        group.MapPost("/{id:guid}/encerrar", async (Guid id, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.EncerrarAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (Guid id, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapPost("/{id:guid}/renovar", async (Guid id, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.RenovarAsync(id, ct)));

        group.MapGet("/{id:guid}/pdf", async (Guid id, ContratoService svc, CancellationToken ct) =>
        {
            var html = await svc.GetPdfHtmlAsync(id, ct);
            return Results.Content(html, "text/html");
        });

        group.MapPost("/{id:guid}/gerar-cobrancas", async (
            Guid id, GerarCobrancasRequest req, ContratoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GerarCobrancasAsync(id, req, ct)));
    }
}
