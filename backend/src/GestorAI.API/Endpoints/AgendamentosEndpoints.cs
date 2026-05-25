using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.Services.Agendamentos;

namespace GestorAI.API.Endpoints;

public static class AgendamentosEndpoints
{
    public static void MapAgendamentos(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/agendamentos").RequireAuthorization();

        group.MapGet("/slots", async (
            Guid profissionalId, DateOnly data, Guid servicoId,
            AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.SlotsAsync(profissionalId, data, servicoId, ct)));

        group.MapGet("/", async (
            DateOnly data, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(data, ct)));

        group.MapGet("/{id:guid}", async (
            Guid id, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (
            CriarAgendamentoRequest req, AgendamentoService svc, CancellationToken ct) =>
        {
            var result = await svc.CriarAsync(req, ct);
            return Results.Created($"/api/agendamentos/{result.Id}", result);
        });

        group.MapPut("/{id:guid}", async (
            Guid id, AtualizarAgendamentoRequest req, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.AtualizarAsync(id, req, ct)));

        group.MapPost("/{id:guid}/confirmar", async (
            Guid id, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConfirmarAsync(id, ct)));

        group.MapPost("/{id:guid}/concluir", async (
            Guid id, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.ConcluirAsync(id, ct)));

        group.MapPost("/{id:guid}/cancelar", async (
            Guid id, AgendamentoService svc, CancellationToken ct) =>
            Results.Ok(await svc.CancelarAsync(id, ct)));
    }
}
