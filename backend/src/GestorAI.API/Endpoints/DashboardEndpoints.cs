using GestorAI.API.DTOs.Dashboard;
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

        group.MapGet("/dashboard/modulos", async (ModuleDashboardService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(ct)));

        group.MapGet("/dashboard/layout", async (DashboardLayoutService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(ct)));

        group.MapPut("/dashboard/layout", async (UpdateDashboardLayoutRequest req, DashboardLayoutService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(req, ct)));

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

        group.MapGet("/relatorios/curva-abc/produtos", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetCurvaAbcProdutosAsync(de, ate, ct)));

        group.MapGet("/relatorios/curva-abc/clientes", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetCurvaAbcClientesAsync(de, ate, ct)));

        group.MapGet("/relatorios/dre", async (
            DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetDreAsync(de, ate, ct)));

        group.MapGet("/relatorios/layout", async (RelatorioLayoutService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(ct)));

        group.MapPut("/relatorios/layout", async (UpdateRelatorioLayoutRequest req, RelatorioLayoutService svc, CancellationToken ct) =>
            Results.Ok(await svc.UpdateAsync(req, ct)));

        group.MapGet("/dashboard/extras", async (DashboardExtrasService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAsync(ct)));

        group.MapGet("/relatorios/agendamentos", async (DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAgendamentosAsync(de, ate, ct)));

        group.MapGet("/relatorios/contratos", async (RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetContratosAsync(ct)));

        group.MapGet("/relatorios/cobrancas", async (RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetCobrancasAsync(ct)));

        group.MapGet("/relatorios/orcamentos", async (DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetOrcamentosAsync(de, ate, ct)));

        group.MapGet("/relatorios/assinaturas", async (DateTime de, DateTime ate, RelatorioService svc, CancellationToken ct) =>
            Results.Ok(await svc.GetAssinaturasAsync(de, ate, ct)));
    }
}
