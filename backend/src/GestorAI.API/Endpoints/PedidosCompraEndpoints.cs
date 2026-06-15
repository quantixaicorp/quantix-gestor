using GestorAI.API.DTOs.Compras;
using GestorAI.API.Services.Compras;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class PedidosCompraEndpoints
{
    public static void MapPedidosCompra(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pedidos-compra").RequireAuthorization();

        group.MapGet("/", async (
            string? status, Guid? fornecedorId,
            PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(status, fornecedorId, ct)));

        group.MapGet("/{id:guid}", async (Guid id, PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (
            CreatePedidoCompraRequest req, PedidoCompraService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/pedidos-compra/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreatePedidoCompraRequest>>();

        group.MapPut("/{id:guid}", async (
            Guid id, UpdatePedidoCompraRequest req, PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapPost("/{id:guid}/converter", async (Guid id, PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConverterAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (Guid id, PedidoCompraService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));
    }
}
