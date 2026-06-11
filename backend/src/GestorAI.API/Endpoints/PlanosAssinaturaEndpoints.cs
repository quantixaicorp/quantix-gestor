using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.Services.Assinaturas;

namespace GestorAI.API.Endpoints;

public static class PlanosAssinaturaEndpoints
{
    public static void MapPlanosAssinatura(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/planos-assinatura").RequireAuthorization();

        group.MapGet("/", async (PlanoAssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, PlanoAssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreatePlanoAssinaturaRequest req, PlanoAssinaturaService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/planos-assinatura/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdatePlanoAssinaturaRequest req, PlanoAssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, PlanoAssinaturaService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });

        app.MapGet("/api/nicho-templates", async (string? nicho, PlanoAssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListTemplatesAsync(nicho, ct))).AllowAnonymous();
    }
}
