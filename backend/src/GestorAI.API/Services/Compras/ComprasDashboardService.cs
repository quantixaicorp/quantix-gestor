using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Compras;

public class ComprasDashboardService(AppDbContext db)
{
    public async Task<ComprasDashboardResponse> GetAsync(DateTime de, DateTime ate, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var inicioAno = new DateTime(hoje.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var comprasConfirmadas = await db.Purchases
            .Include(c => c.Items)
            .Where(c => c.Status == StatusCompra.Confirmada)
            .ToListAsync(ct);

        var totalMes = comprasConfirmadas
            .Where(c => c.Date >= inicioMes)
            .Sum(c => c.TotalAmount);

        var totalAno = comprasConfirmadas
            .Where(c => c.Date >= inicioAno)
            .Sum(c => c.TotalAmount);

        var comprasMes = comprasConfirmadas
            .Where(c => c.Date >= inicioMes)
            .ToList();

        var qtdComprasMes = comprasMes.Count;
        var ticketMedio = qtdComprasMes > 0 ? totalMes / qtdComprasMes : 0m;

        var fornecedoresAtivos = await db.Purchases
            .Where(c => c.Status == StatusCompra.Confirmada && c.Date >= inicioAno)
            .Select(c => c.SupplierId)
            .Distinct()
            .CountAsync(ct);

        // Série mensal (período solicitado)
        var comprasPeriodo = comprasConfirmadas
            .Where(c => c.Date >= de && c.Date <= ate)
            .ToList();

        var seriesMensal = comprasPeriodo
            .GroupBy(c => new { c.Date.Year, c.Date.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new ComprasMensalSerieItem(
                $"{g.Key.Year}-{g.Key.Month:D2}",
                g.Sum(c => c.TotalAmount),
                g.Count()))
            .ToList();

        // Por fornecedor
        var fornecedorIds = comprasPeriodo.Select(c => c.SupplierId).Distinct().ToList();
        var fornecedores = await db.Suppliers
            .Where(f => fornecedorIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Name, ct);

        var porFornecedor = comprasPeriodo
            .GroupBy(c => c.SupplierId)
            .Select(g => new ComprasPorFornecedorItem(
                fornecedores.GetValueOrDefault(g.Key, "Desconhecido"),
                g.Sum(c => c.TotalAmount)))
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();

        // Top produtos
        var topProdutos = comprasPeriodo
            .SelectMany(c => c.Items)
            .Where(i => !string.IsNullOrEmpty(i.Description))
            .GroupBy(i => i.Description)
            .Select(g => new TopProdutoCompradoItem(
                g.Key,
                g.Sum(i => i.Quantity),
                g.Sum(i => i.TotalAmount)))
            .OrderByDescending(x => x.TotalAmount)
            .Take(10)
            .ToList();

        return new ComprasDashboardResponse(
            totalMes, totalAno, ticketMedio, qtdComprasMes,
            fornecedoresAtivos, seriesMensal, porFornecedor, topProdutos);
    }
}
