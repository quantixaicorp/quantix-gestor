using GestorAI.API.DTOs.Contabilidade;
using GestorAI.API.Services.Contabilidade;

namespace GestorAI.API.Endpoints;

public static class ContabilidadeEndpoints
{
    public static void MapContabilidade(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        // Chart of Accounts
        group.MapGet("/chart-of-accounts", async (
            ChartOfAccountService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPost("/chart-of-accounts", async (
            CreateChartOfAccountRequest req, ChartOfAccountService svc, CancellationToken ct) =>
        {
            var result = await svc.CreateAsync(req, ct);
            return Results.Created($"/api/chart-of-accounts/{result.Id}", result);
        });

        group.MapPut("/chart-of-accounts/{id:guid}", async (
            Guid id, UpdateChartOfAccountRequest req, ChartOfAccountService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(id, req, ct)));

        group.MapDelete("/chart-of-accounts/{id:guid}", async (
            Guid id, ChartOfAccountService svc, CancellationToken ct) =>
        {
            await svc.DeleteAsync(id, ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        group.MapPost("/chart-of-accounts/load-template", async (
            ChartOfAccountService svc, CancellationToken ct) =>
        {
            await svc.LoadTemplateAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization("AdminOnly");

        // Account Mappings
        group.MapGet("/account-mappings", async (
            AccountMappingService svc, CancellationToken ct) =>
            Results.Ok(await svc.ListAsync(ct)));

        group.MapPut("/account-mappings", async (
            BulkUpsertAccountMappingsRequest req, AccountMappingService svc, CancellationToken ct) =>
        {
            await svc.BulkUpsertAsync(req, ct);
            return Results.NoContent();
        });

        // Accounting Settings
        group.MapGet("/accounting-settings", async (
            AccountingSettingsService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(ct)));

        group.MapPut("/accounting-settings", async (
            UpdateAccountingSettingsRequest req, AccountingSettingsService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(req, ct)));

        // Export
        group.MapPost("/accounting-export/download", async (
            AccountingExportRequest req, AccountingExportService svc, CancellationToken ct) =>
        {
            var (content, fileName, contentType) = await svc.ExportAsync(req, ct);
            return Results.File(content, contentType, fileName);
        });
    }
}
