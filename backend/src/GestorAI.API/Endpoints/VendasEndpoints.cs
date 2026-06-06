using GestorAI.API.DTOs.Vendas;
using GestorAI.API.Services.Vendas;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class VendasEndpoints
{
    public static void MapVendas(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vendas").RequireAuthorization();

        group.MapGet("/", async (
            DateTime? de, DateTime? ate, string? status,
            VendaService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(de, ate, status, ct)));

        group.MapGet("/{id:guid}", async (Guid id, VendaService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (
            CreateVendaRequest req, VendaService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/vendas/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateVendaRequest>>();

        group.MapPost("/{id:guid}/cancelar", async (
            Guid id, VendaService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));

        group.MapPost("/{id:guid}/fechar", async (
            Guid id, FecharVendaRequest req, VendaService svc, CancellationToken ct) =>
            Results.Ok(await svc.FecharAsync(id, req, ct)));

        group.MapDelete("/{id:guid}", async (
            Guid id, VendaService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");
    }
}
