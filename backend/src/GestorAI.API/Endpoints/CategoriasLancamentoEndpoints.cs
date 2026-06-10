using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Services.Financeiro;

namespace GestorAI.API.Endpoints;

public static class CategoriasLancamentoEndpoints
{
    public static void MapCategoriasLancamento(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categorias-lancamento").RequireAuthorization();

        group.MapGet("", async (string? tipo, CategoriaLancamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(tipo, ct)));

        group.MapPost("", async (
            CreateCategoriaLancamentoRequest req,
            CategoriaLancamentoService svc,
            CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/categorias-lancamento/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCategoriaLancamentoRequest req,
            CategoriaLancamentoService svc,
            CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapDelete("/{id:guid}", async (
            Guid id,
            CategoriaLancamentoService svc,
            CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}
