using GestorAI.API.Services.Compras;

namespace GestorAI.API.Endpoints;

public static class ParcelamentosEndpoints
{
    public static void MapParcelamentos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/parcelamentos").RequireAuthorization();

        group.MapGet("/", async (
            string? status, Guid? compraId,
            ParcelamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, compraId, ct)));

        group.MapGet("/{id:guid}", async (Guid id, ParcelamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));
    }
}
