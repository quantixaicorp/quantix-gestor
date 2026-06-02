using GestorAI.API.DTOs.PublicBooking;
using GestorAI.API.Services.PublicBooking;

namespace GestorAI.API.Endpoints;

public static class PublicBookingEndpoints
{
    public static void MapPublicBooking(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/public/{slug}");

        group.MapGet("/info", async (
            string slug, PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetInfoAsync(empresaId, ct));
        });

        group.MapGet("/servicos", async (
            string slug, PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetServicosAsync(empresaId, ct));
        });

        group.MapGet("/profissionais", async (
            string slug, PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetProfissionaisAsync(empresaId, ct));
        });

        group.MapGet("/disponibilidade", async (
            string slug, Guid profissionalId,
            PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            var dias = await svc.GetDiasComDisponibilidadeAsync(empresaId, profissionalId, ct);
            return Results.Ok(new PublicDisponibilidadeResponse(dias));
        });

        group.MapGet("/slots", async (
            string slug, Guid profissionalId, Guid servicoId, DateOnly data,
            PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetSlotsAsync(empresaId, profissionalId, servicoId, data, ct));
        });

        group.MapPost("/agendamentos", async (
            string slug, PublicCriarAgendamentoRequest req,
            PublicBookingService svc, CancellationToken ct) =>
        {
            var empresaId = await svc.ResolveEmpresaAsync(slug, ct);
            var result = await svc.CriarAgendamentoAsync(empresaId, req, ct);
            return Results.Created($"/public/{slug}/agendamentos/{result.Id}", result);
        });
    }
}
