using GestorAI.API.DTOs.Orcamentos;
using GestorAI.API.Services.Orcamentos;

namespace GestorAI.API.Endpoints;

public static class OrcamentosEndpoints
{
    public static void MapOrcamentos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orcamentos").RequireAuthorization();

        group.MapGet("/", async (
            string? status, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, ct)));

        group.MapGet("/{id:guid}", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (
            CreateOrcamentoRequest req, OrcamentoService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/orcamentos/{result.Id}", result);
        });

        group.MapPost("/{id:guid}/enviar", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.EnviarAsync(id, ct)));

        group.MapPost("/{id:guid}/aprovar", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.AprovarAsync(id, ct)));

        group.MapPost("/{id:guid}/rejeitar", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.RejeitarAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapPost("/{id:guid}/converter", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConvertAsync(id, ct)));

        group.MapPost("/{id:guid}/gerar-cobranca", async (
            Guid id, GerarCobrancaOrcamentoRequest req,
            OrcamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GerarCobrancaAsync(id, req.DataVencimento, ct)));

        group.MapGet("/{id:guid}/pdf", async (
            Guid id, OrcamentoService svc, CancellationToken ct) =>
        {
            var html = await svc.GetPdfHtmlAsync(id, ct);
            return Results.Content(html, "text/html");
        });
    }
}
