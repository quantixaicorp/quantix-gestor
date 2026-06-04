using GestorAI.API.DTOs.Fornecedores;
using GestorAI.API.Services.Fornecedores;
using GestorAI.API.Shared.Filters;

namespace GestorAI.API.Endpoints;

public static class FornecedoresEndpoints
{
    public static void MapFornecedores(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/fornecedores").RequireAuthorization();

        group.MapGet("/", async (string? busca, FornecedorService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(busca, ct)));

        group.MapGet("/{id:guid}", async (Guid id, FornecedorService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateFornecedorRequest req, FornecedorService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/fornecedores/{result.Id}", result);
        }).AddEndpointFilter<ValidationFilter<CreateFornecedorRequest>>();

        group.MapPut("/{id:guid}", async (
            Guid id, UpdateFornecedorRequest req, FornecedorService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapDelete("/{id:guid}", async (Guid id, FornecedorService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}
