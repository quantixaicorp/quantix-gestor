using GestorAI.API.DTOs.Contratos;
using GestorAI.API.Services.Contratos;

namespace GestorAI.API.Endpoints;

public static class ContratoTemplatesEndpoints
{
    public static void MapContratoTemplates(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/contrato-templates").RequireAuthorization();

        group.MapGet("/", async (ContratoTemplateService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, ContratoTemplateService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(id, ct)));

        group.MapPost("/", async (CreateContratoTemplateRequest req, ContratoTemplateService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/contrato-templates/{result.Id}", result);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ContratoTemplateService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        });
    }
}
