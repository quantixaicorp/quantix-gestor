using GestorAI.API.Services.Assinaturas;

namespace GestorAI.API.Endpoints;

public static class AssinaturasEndpoints
{
    public static void MapAssinaturas(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/assinaturas").RequireAuthorization();

        group.MapGet("/", async (Guid? planoId, string? status, AssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(planoId, status, ct)));

        group.MapGet("/{id:guid}", async (Guid id, AssinaturaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (Guid id, AssinaturaService svc, CancellationToken ct) =>
        {
            await svc.CancelarAsync(id, ct);
            return Results.NoContent();
        });
    }
}
