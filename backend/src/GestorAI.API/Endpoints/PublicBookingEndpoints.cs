using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.DTOs.PublicBooking;
using GestorAI.API.Services.Agendamentos;
using GestorAI.API.Services.Assinaturas;
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

        group.MapPost("/agendamentos/{id:guid}/cancelar", async (
            string slug, Guid id,
            PublicBookingService svc, AgendamentoService agendamentoSvc, CancellationToken ct) =>
        {
            await svc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await agendamentoSvc.CancelarPublicoAsync(id, ct));
        });

        group.MapGet("/planos", async (
            string slug, PlanoAssinaturaService svc,
            PublicBookingService publicSvc, CancellationToken ct) =>
        {
            var empresaId = await publicSvc.ResolveEmpresaAsync(slug, ct);
            var planos = await svc.ListPublicAsync(empresaId, ct);
            return Results.Ok(planos);
        });

        group.MapGet("/planos/{planoId:guid}", async (
            string slug, Guid planoId,
            PlanoAssinaturaService svc, PublicBookingService publicSvc, CancellationToken ct) =>
        {
            var empresaId = await publicSvc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.GetPublicAsync(empresaId, planoId, ct));
        });

        group.MapPost("/planos/{planoId:guid}/assinar", async (
            string slug, Guid planoId,
            AssinarRequest req,
            AssinaturaService svc, PublicBookingService publicSvc, CancellationToken ct) =>
        {
            var empresaId = await publicSvc.ResolveEmpresaAsync(slug, ct);
            return Results.Ok(await svc.AssinarAsync(empresaId, planoId, req, ct));
        });
    }
}
