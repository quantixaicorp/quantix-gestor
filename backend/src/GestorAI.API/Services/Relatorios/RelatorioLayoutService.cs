using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Dashboard;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GestorAI.API.Services.Relatorios;

public class RelatorioLayoutService(AppDbContext db, TenantContext tenantContext)
{
    private static readonly List<string> DefaultLayout =
    [
        "visao-geral",
        "vendas",
        "financeiro",
        "estoque",
        "curva-abc",
        "dre",
    ];

    public async Task<RelatorioLayoutResponse> GetAsync(CancellationToken ct)
    {
        var layout = await db.RelatorioLayouts.FirstOrDefaultAsync(ct);
        if (layout is null)
            return new RelatorioLayoutResponse(DefaultLayout);

        var tabs = JsonSerializer.Deserialize<List<string>>(layout.TabsJson) ?? DefaultLayout;
        return new RelatorioLayoutResponse(tabs);
    }

    public async Task<RelatorioLayoutResponse> UpdateAsync(UpdateRelatorioLayoutRequest req, CancellationToken ct)
    {
        var layout = await db.RelatorioLayouts.FirstOrDefaultAsync(ct);
        if (layout is null)
        {
            layout = new RelatorioLayout { EmpresaId = tenantContext.EmpresaId };
            db.RelatorioLayouts.Add(layout);
        }
        layout.TabsJson = JsonSerializer.Serialize(req.Tabs);
        layout.AtualizadoEm = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return new RelatorioLayoutResponse(req.Tabs);
    }
}
