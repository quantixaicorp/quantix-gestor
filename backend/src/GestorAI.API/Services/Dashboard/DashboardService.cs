using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Dashboard;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Dashboard;

public class DashboardService(AppDbContext db)
{
    public async Task<DashboardResponse> GetDashboardAsync(CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var em7Dias = hoje.AddDays(7);

        // ── Vendas hoje + receitas manuais hoje ──────────────────────────
        var vendasHoje = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida && v.SaleDate.Date == hoje)
            .SumAsync(v => v.Total, ct)
            + await db.Transactions
            .Where(l => l.Type == TipoLancamento.Receita && l.Status == StatusLancamento.Pago
                && l.SaleId == null && l.PaymentDate.HasValue && l.PaymentDate!.Value.Date == hoje)
            .SumAsync(l => l.Amount, ct);

        // ── Vendas mês ───────────────────────────────────────────────────
        var vendasMesLista = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida && v.SaleDate >= inicioMes)
            .ToListAsync(ct);

        var vendasMes = vendasMesLista.Sum(v => v.Total)
            + await db.Transactions
            .Where(l => l.Type == TipoLancamento.Receita && l.Status == StatusLancamento.Pago
                && l.SaleId == null && l.PaymentDate.HasValue && l.PaymentDate!.Value.Date >= inicioMes)
            .SumAsync(l => l.Amount, ct);

        var qtyVendasMes = vendasMesLista.Count;
        var ticketMedio = qtyVendasMes > 0 ? vendasMesLista.Sum(v => v.Total) / qtyVendasMes : 0m;

        // ── Items do mês (lucro + top produtos) ──────────────────────────
        var itensDoMes = await db.SaleItems
            .Where(i => i.Sale!.Status == StatusVenda.Concluida && i.Sale.SaleDate >= inicioMes)
            .Include(i => i.Product)
            .ToListAsync(ct);

        var lucroEstimado = itensDoMes
            .Sum(i => (i.UnitPrice - (i.Product?.AverageCost ?? 0m)) * i.Quantity);

        // ── Financeiro ───────────────────────────────────────────────────
        var contasPagarVencidas = await db.Transactions
            .Where(l => l.Type == TipoLancamento.Despesa && l.Status == StatusLancamento.Pendente
                && l.DueDate.Date < hoje)
            .SumAsync(l => l.Amount, ct);

        var contasPagarProximas = await db.Transactions
            .Where(l => l.Type == TipoLancamento.Despesa && l.Status == StatusLancamento.Pendente
                && l.DueDate.Date >= hoje && l.DueDate.Date <= em7Dias)
            .SumAsync(l => l.Amount, ct);

        var lancamentosReceita = await db.Transactions
            .Where(l => l.Type == TipoLancamento.Receita && l.Status == StatusLancamento.Pendente
                && l.DueDate >= inicioMes && l.DueDate < inicioMes.AddMonths(1))
            .ToListAsync(ct);

        var contasReceberPendentes = lancamentosReceita.Sum(l => l.Amount);
        var receitasVencidas = lancamentosReceita.Where(l => l.DueDate.Date < hoje).Sum(l => l.Amount);
        var inadimplencia = contasReceberPendentes > 0
            ? Math.Round(receitasVencidas / contasReceberPendentes * 100m, 1) : 0m;

        var saldoProjetado = contasReceberPendentes - contasPagarVencidas - contasPagarProximas;

        // ── Estoque ──────────────────────────────────────────────────────
        var estoqueBaixo = await db.Products
            .CountAsync(p => p.IsActive && p.Type == TipoProduto.Produto && p.CurrentStock <= p.MinimumStock, ct);

        var produtos = await db.Products.Where(p => p.IsActive).ToListAsync(ct);
        var valorEstoque = produtos.Sum(p => p.CurrentStock * p.AverageCost);

        // ── Receitas/Despesas mês ────────────────────────────────────────
        var lancamentosMes = await db.Transactions
            .Where(l => l.Status == StatusLancamento.Pago
                && l.PaymentDate.HasValue && l.PaymentDate!.Value.Date >= inicioMes)
            .ToListAsync(ct);

        var totalReceitasMes = lancamentosMes.Where(l => l.Type == TipoLancamento.Receita).Sum(l => l.Amount);
        var totalDespesasMes = lancamentosMes.Where(l => l.Type == TipoLancamento.Despesa).Sum(l => l.Amount);

        var despesasPorCategoria = lancamentosMes
            .Where(l => l.Type == TipoLancamento.Despesa)
            .GroupBy(l => string.IsNullOrEmpty(l.Category) ? "Sem categoria" : l.Category)
            .Select(g => new CategoriaDespesaDashResponse(g.Key, g.Sum(l => l.Amount)))
            .OrderByDescending(c => c.Total)
            .Take(8)
            .ToList();

        // ── Clientes ─────────────────────────────────────────────────────
        var totalClientes = await db.Customers.CountAsync(ct);
        var clientesNovosMes = await db.Customers
            .CountAsync(c => c.CreatedAt >= inicioMes, ct);

        var topClientes = vendasMesLista
            .Where(v => v.CustomerId != null)
            .GroupBy(v => new { v.CustomerId, Name = v.Customer?.Name ?? "" })
            .Select(g => new TopClienteResponse(g.Key.Name, g.Sum(v => v.Total)))
            .OrderByDescending(c => c.TotalGasto)
            .Take(10)
            .ToList();

        // ── Série vendas 7 dias ──────────────────────────────────────────
        var inicio7Dias = hoje.AddDays(-6);
        var vendasSerie = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida && v.SaleDate.Date >= inicio7Dias)
            .GroupBy(v => v.SaleDate.Date)
            .Select(g => new VendasDiaResponse(g.Key, g.Sum(v => v.Total), g.Count()))
            .ToListAsync(ct);

        var vendas7Dias = Enumerable.Range(0, 7)
            .Select(i => inicio7Dias.AddDays(i))
            .Select(d => vendasSerie.FirstOrDefault(v => v.Data == d) ?? new VendasDiaResponse(d, 0m, 0))
            .ToList();

        // ── Fluxo do mês ─────────────────────────────────────────────────
        var fluxoSerie = lancamentosMes
            .GroupBy(l => l.PaymentDate!.Value.Date)
            .Select(g => new FluxoDiaResponse(
                g.Key,
                g.Where(l => l.Type == TipoLancamento.Receita).Sum(l => l.Amount),
                g.Where(l => l.Type == TipoLancamento.Despesa).Sum(l => l.Amount)))
            .OrderBy(f => f.Data)
            .ToList();

        // ── Top 5 produtos ───────────────────────────────────────────────
        var topProdutos = itensDoMes
            .GroupBy(i => new { i.ProductId, Name = i.Product?.Name ?? "" })
            .Select(g => new TopProdutoResponse(g.Key.Name, g.Sum(i => i.Quantity), g.Sum(i => i.Total)))
            .OrderByDescending(p => p.QuantidadeVendida)
            .Take(5)
            .ToList();

        // ── Top 10 clientes (inclui vendasMesLista com Customer carregado) ─
        // vendasMesLista não tem Customer — recarregar com Include
        var vendasMesComCliente = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida && v.SaleDate >= inicioMes && v.CustomerId != null)
            .Include(v => v.Customer)
            .ToListAsync(ct);

        var topClientesComNome = vendasMesComCliente
            .GroupBy(v => new { v.CustomerId, Name = v.Customer?.Name ?? "" })
            .Select(g => new TopClienteResponse(g.Key.Name, g.Sum(v => v.Total)))
            .OrderByDescending(c => c.TotalGasto)
            .Take(10)
            .ToList();

        return new DashboardResponse(
            new KpiResponse(
                vendasHoje, vendasMes, lucroEstimado,
                contasPagarVencidas, contasPagarProximas, contasReceberPendentes, estoqueBaixo,
                totalReceitasMes, totalDespesasMes,
                qtyVendasMes, ticketMedio, totalClientes, clientesNovosMes,
                valorEstoque, inadimplencia, saldoProjetado),
            vendas7Dias,
            fluxoSerie,
            topProdutos,
            topClientesComNome,
            despesasPorCategoria);
    }
}
