using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.Services.Agendamentos;

namespace GestorAI.API.Endpoints;

public static class ProfissionaisEndpoints
{
    public static void MapProfissionais(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/profissionais").RequireAuthorization();

        group.MapGet("/", async (ProfissionalService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPost("/", async (
            CriarProfissionalRequest req, ProfissionalService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/profissionais/{result.Id}", result);
        }).RequireAuthorization("AdminOnly");

        group.MapPut("/{id:guid}", async (
            Guid id, AtualizarProfissionalRequest req, ProfissionalService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)))
            .RequireAuthorization("AdminOnly");

        group.MapDelete("/{id:guid}", async (
            Guid id, ProfissionalService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        group.MapGet("/{id:guid}/disponibilidade", async (
            Guid id, ProfissionalService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetDisponibilidadeAsync(id, ct)));

        group.MapPut("/{id:guid}/disponibilidade", async (
            Guid id, SalvarDisponibilidadeRequest req, ProfissionalService svc, CancellationToken ct) =>
        {
            await svc.SalvarDisponibilidadeAsync(id, req, ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        var bloqueiosGroup = app.MapGroup("/api/agenda/bloqueios").RequireAuthorization();

        bloqueiosGroup.MapGet("/", async (
            DateTime de, DateTime ate, ProfissionalService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListBloqueiosAsync(de, ate, ct)));

        bloqueiosGroup.MapPost("/", async (
            CriarBloqueioRequest req, ProfissionalService svc, CancellationToken ct) =>
        {
            var result = await svc.CriarBloqueioAsync(req, ct);
            return Results.Created($"/api/agenda/bloqueios/{result.Id}", result);
        });

        bloqueiosGroup.MapDelete("/{id:guid}", async (
            Guid id, ProfissionalService svc, CancellationToken ct) =>
        {
            await svc.DeleteBloqueioAsync(id, ct);
            return Results.NoContent();
        });
    }
}
