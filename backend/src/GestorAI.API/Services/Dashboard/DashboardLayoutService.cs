using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Dashboard;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GestorAI.API.Services.Dashboard;

public class DashboardLayoutService(AppDbContext db, TenantContext tenantContext)
{
    private static readonly List<string> DefaultLayout =
    [
        "kpi-saldo-mes",
        "kpi-vendas-hoje",
        "kpi-contas-vencidas",
        "kpi-contas-receber",
        "grafico-tendencia-vendas",
        "grafico-fluxo-caixa",
        "tabela-top-produtos",
        "alerta-estoque-baixo",
    ];

    public async Task<DashboardLayoutResponse> GetAsync(CancellationToken ct)
    {
        var layout = await db.DashboardLayouts.FirstOrDefaultAsync(ct);
        if (layout is null)
            return new DashboardLayoutResponse(DefaultLayout);

        var widgets = JsonSerializer.Deserialize<List<string>>(layout.WidgetsJson) ?? DefaultLayout;
        return new DashboardLayoutResponse(widgets);
    }

    public async Task<DashboardLayoutResponse> UpdateAsync(UpdateDashboardLayoutRequest req, CancellationToken ct)
    {
        var layout = await db.DashboardLayouts.FirstOrDefaultAsync(ct);
        if (layout is null)
        {
            layout = new DashboardLayout { EmpresaId = tenantContext.EmpresaId };
            db.DashboardLayouts.Add(layout);
        }
        layout.WidgetsJson = JsonSerializer.Serialize(req.Widgets);
        layout.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new DashboardLayoutResponse(req.Widgets);
    }
}
