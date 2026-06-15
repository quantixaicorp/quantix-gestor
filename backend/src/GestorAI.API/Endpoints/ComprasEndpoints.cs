using GestorAI.API.DTOs.Compras;
using GestorAI.API.Services.Compras;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class ComprasEndpoints
{
    public static void MapCompras(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/compras").RequireAuthorization();

        group.MapGet("/", async (
            string? status, Guid? fornecedorId, DateTime? de, DateTime? ate,
            CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, fornecedorId, de, ate, ct)));

        group.MapGet("/resumo", async (CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetResumoAsync(ct)));

        group.MapGet("/dashboard", async (
            DateTime? de, DateTime? ate,
            ComprasDashboardService svc, CancellationToken ct) =>
        {
            var hoje = DateTime.UtcNow.Date;
            var from = de ?? new DateTime(hoje.Year, hoje.Month, 1);
            var to = ate ?? hoje;
            return Results.Ok(await svc.GetAsync(from, to, ct));
        });

        group.MapGet("/{id:guid}", async (Guid id, CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateCompraRequest req, CompraService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/compras/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateCompraRequest>>();

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateCompraRequest req, CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapPost("/{id:guid}/confirmar", async (Guid id, CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConfirmarAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (Guid id, CompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, CompraService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}
