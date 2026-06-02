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

        var vendasHoje = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora.Date == hoje)
            .SumAsync(v => v.Total, ct)
            + await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pago
                && l.DataPagamento.HasValue && l.DataPagamento!.Value.Date == hoje)
            .SumAsync(l => l.Valor, ct);

        var vendasMes = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora >= inicioMes)
            .SumAsync(v => v.Total, ct)
            + await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pago
                && l.DataPagamento.HasValue && l.DataPagamento!.Value.Date >= inicioMes)
            .SumAsync(l => l.Valor, ct);

        var lucroEstimado = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida && i.Venda.DataHora >= inicioMes)
            .Include(i => i.Produto)
            .SumAsync(i => (i.PrecoUnitario - i.Produto!.CustoMedio) * i.Quantidade, ct);

        var contasPagarVencidas = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa
                && l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date < hoje)
            .SumAsync(l => l.Valor, ct);

        var contasPagarProximas = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa
                && l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date >= hoje
                && l.DataVencimento.Date <= em7Dias)
            .SumAsync(l => l.Valor, ct);

        var contasReceberPendentes = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita && l.Status == StatusLancamento.Pendente)
            .SumAsync(l => l.Valor, ct);

        var estoqueBaixo = await db.Produtos
            .CountAsync(p => p.Ativo && p.Tipo == TipoProduto.Produto && p.EstoqueAtual <= p.EstoqueMinimo, ct);

        // Vendas últimos 7 dias
        var inicio7Dias = hoje.AddDays(-6);
        var vendasSerie = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora.Date >= inicio7Dias)
            .GroupBy(v => v.DataHora.Date)
            .Select(g => new VendasDiaResponse(g.Key, g.Sum(v => v.Total), g.Count()))
            .ToListAsync(ct);

        var vendas7Dias = Enumerable.Range(0, 7)
            .Select(i => inicio7Dias.AddDays(i))
            .Select(d => vendasSerie.FirstOrDefault(v => v.Data == d)
                ?? new VendasDiaResponse(d, 0m, 0))
            .ToList();

        // Fluxo do mês — load into memory to avoid EF translation issues with conditional Sum inside GroupBy
        var lancamentosPagos = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pago
                && l.DataPagamento.HasValue
                && l.DataPagamento!.Value.Date >= inicioMes)
            .ToListAsync(ct);

        var fluxoSerie = lancamentosPagos
            .GroupBy(l => l.DataPagamento!.Value.Date)
            .Select(g => new FluxoDiaResponse(
                g.Key,
                g.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor),
                g.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor)))
            .OrderBy(f => f.Data)
            .ToList();

        // Top 5 produtos do mês — load into memory first to avoid EF SQL translation issues
        var itensDoMes = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida && i.Venda.DataHora >= inicioMes)
            .Include(i => i.Produto)
            .ToListAsync(ct);

        var topProdutos = itensDoMes
            .GroupBy(i => new { i.ProdutoId, Nome = i.Produto?.Nome ?? "" })
            .Select(g => new TopProdutoResponse(
                g.Key.Nome,
                g.Sum(i => i.Quantidade),
                g.Sum(i => i.Total)))
            .OrderByDescending(p => p.QuantidadeVendida)
            .Take(5)
            .ToList();

        return new DashboardResponse(
            new KpiResponse(vendasHoje, vendasMes, lucroEstimado,
                contasPagarVencidas, contasPagarProximas, contasReceberPendentes, estoqueBaixo),
            vendas7Dias,
            fluxoSerie,
            topProdutos);
    }
}
