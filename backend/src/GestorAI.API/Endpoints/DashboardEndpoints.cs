using GestorAI.API.Services.Dashboard;
using GestorAI.API.Services.Relatorios;

namespace GestorAI.API.Endpoints;

public static class DashboardEndpoints
{
    public static void MapDashboard(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        group.MapGet("/dashboard", async (DashboardService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetDashboardAsync(ct)));

        group.MapGet("/relatorios/kpis", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetKpisGeralAsync(de, ate, ct)));

        group.MapGet("/relatorios/vendas", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetVendasAsync(de, ate, ct)));

        group.MapGet("/relatorios/financeiro", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetFinanceiroAsync(de, ate, ct)));

        group.MapGet("/relatorios/estoque", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetEstoqueAsync(de, ate, ct)));

        group.MapGet("/relatorios/clientes", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetClientesAsync(de, ate, ct)));
    }
}
