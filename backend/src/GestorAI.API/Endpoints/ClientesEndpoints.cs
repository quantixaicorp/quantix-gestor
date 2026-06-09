using GestorAI.API.DTOs.Clientes;
using GestorAI.API.Services.Clientes;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class ClientesEndpoints
{
    public static void MapClientes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/clientes").RequireAuthorization();

        group.MapGet("/", async (string? busca, ClienteService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(busca, ct)));

        group.MapGet("/{id:guid}", async (Guid id, ClienteService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateClienteRequest req, ClienteService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/clientes/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateClienteRequest>>();

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateClienteRequest req, ClienteService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, ClienteService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}
