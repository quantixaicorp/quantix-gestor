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

        var comprasConfirmadas = await db.Compras
            .Include(c => c.Itens)
            .Where(c => c.Status == StatusCompra.Confirmada)
            .ToListAsync(ct);

        var totalMes = comprasConfirmadas
            .Where(c => c.Data >= inicioMes)
            .Sum(c => c.ValorTotal);

        var totalAno = comprasConfirmadas
            .Where(c => c.Data >= inicioAno)
            .Sum(c => c.ValorTotal);

        var comprasMes = comprasConfirmadas
            .Where(c => c.Data >= inicioMes)
            .ToList();

        var qtdComprasMes = comprasMes.Count;
        var ticketMedio = qtdComprasMes > 0 ? totalMes / qtdComprasMes : 0m;

        var fornecedoresAtivos = await db.Compras
            .Where(c => c.Status == StatusCompra.Confirmada && c.Data >= inicioAno)
            .Select(c => c.FornecedorId)
            .Distinct()
            .CountAsync(ct);

        // Série mensal (período solicitado)
        var comprasPeriodo = comprasConfirmadas
            .Where(c => c.Data >= de && c.Data <= ate)
            .ToList();

        var seriesMensal = comprasPeriodo
            .GroupBy(c => new { c.Data.Year, c.Data.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new ComprasMensalSerieItem(
                $"{g.Key.Year}-{g.Key.Month:D2}",
                g.Sum(c => c.ValorTotal),
                g.Count()))
            .ToList();

        // Por fornecedor
        var fornecedorIds = comprasPeriodo.Select(c => c.FornecedorId).Distinct().ToList();
        var fornecedores = await db.Fornecedores
            .Where(f => fornecedorIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.Nome, ct);

        var porFornecedor = comprasPeriodo
            .GroupBy(c => c.FornecedorId)
            .Select(g => new ComprasPorFornecedorItem(
                fornecedores.GetValueOrDefault(g.Key, "Desconhecido"),
                g.Sum(c => c.ValorTotal)))
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();

        // Top produtos
        var topProdutos = comprasPeriodo
            .SelectMany(c => c.Itens)
            .Where(i => !string.IsNullOrEmpty(i.Descricao))
            .GroupBy(i => i.Descricao)
            .Select(g => new TopProdutoCompradoItem(
                g.Key,
                g.Sum(i => i.Quantidade),
                g.Sum(i => i.ValorTotal)))
            .OrderByDescending(x => x.ValorTotal)
            .Take(10)
            .ToList();

        return new ComprasDashboardResponse(
            totalMes, totalAno, ticketMedio, qtdComprasMes,
            fornecedoresAtivos, seriesMensal, porFornecedor, topProdutos);
    }
}
