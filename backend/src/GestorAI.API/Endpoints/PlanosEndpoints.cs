using GestorAI.API.Services;

namespace GestorAI.API.Endpoints;

public static class PlanosEndpoints
{
    public static void MapPlanos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/planos").RequireAuthorization();

        group.MapGet("/", async (PlanoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListPlanosAsync(ct)));

        group.MapGet("/atual", async (PlanoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetPlanoAtualAsync(ct)));

        group.MapPost("/ativar/{planoId:guid}", async (
            Guid planoId, PlanoService svc, CancellationToken ct) =>
        {
            await svc.AtivarPlanoAsync(planoId, ct);
            return Results.Ok(new { sucesso = true });
        });
    }
}
