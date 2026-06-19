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
        var vendasHoje = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora.Date == hoje)
            .SumAsync(v => v.Total, ct)
            + await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita && l.Status == StatusLancamento.Pago
                && l.VendaId == null && l.DataPagamento.HasValue && l.DataPagamento!.Value.Date == hoje)
            .SumAsync(l => l.Valor, ct);

        // ── Vendas mês ───────────────────────────────────────────────────
        var vendasMesLista = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora >= inicioMes)
            .ToListAsync(ct);

        var vendasMes = vendasMesLista.Sum(v => v.Total)
            + await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita && l.Status == StatusLancamento.Pago
                && l.VendaId == null && l.DataPagamento.HasValue && l.DataPagamento!.Value.Date >= inicioMes)
            .SumAsync(l => l.Valor, ct);

        var qtyVendasMes = vendasMesLista.Count;
        var ticketMedio = qtyVendasMes > 0 ? vendasMesLista.Sum(v => v.Total) / qtyVendasMes : 0m;

        // ── Itens do mês (lucro + top produtos) ──────────────────────────
        var itensDoMes = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida && i.Venda.DataHora >= inicioMes)
            .Include(i => i.Produto)
            .ToListAsync(ct);

        var lucroEstimado = itensDoMes
            .Sum(i => (i.PrecoUnitario - (i.Produto?.CustoMedio ?? 0m)) * i.Quantidade);

        // ── Financeiro ───────────────────────────────────────────────────
        var contasPagarVencidas = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa && l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date < hoje)
            .SumAsync(l => l.Valor, ct);

        var contasPagarProximas = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa && l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date >= hoje && l.DataVencimento.Date <= em7Dias)
            .SumAsync(l => l.Valor, ct);

        var lancamentosReceita = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita && l.Status == StatusLancamento.Pendente
                && l.DataVencimento >= inicioMes && l.DataVencimento < inicioMes.AddMonths(1))
            .ToListAsync(ct);

        var contasReceberPendentes = lancamentosReceita.Sum(l => l.Valor);
        var receitasVencidas = lancamentosReceita.Where(l => l.DataVencimento.Date < hoje).Sum(l => l.Valor);
        var inadimplencia = contasReceberPendentes > 0
            ? Math.Round(receitasVencidas / contasReceberPendentes * 100m, 1) : 0m;

        var saldoProjetado = contasReceberPendentes - contasPagarVencidas - contasPagarProximas;

        // ── Estoque ──────────────────────────────────────────────────────
        var estoqueBaixo = await db.Produtos
            .CountAsync(p => p.Ativo && p.Tipo == TipoProduto.Produto && p.EstoqueAtual <= p.EstoqueMinimo, ct);

        var produtos = await db.Produtos.Where(p => p.Ativo).ToListAsync(ct);
        var valorEstoque = produtos.Sum(p => p.EstoqueAtual * p.CustoMedio);

        // ── Receitas/Despesas mês ────────────────────────────────────────
        var lancamentosMes = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pago
                && l.DataPagamento.HasValue && l.DataPagamento!.Value.Date >= inicioMes)
            .ToListAsync(ct);

        var totalReceitasMes = lancamentosMes.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor);
        var totalDespesasMes = lancamentosMes.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor);

        var despesasPorCategoria = lancamentosMes
            .Where(l => l.Tipo == TipoLancamento.Despesa)
            .GroupBy(l => string.IsNullOrEmpty(l.Categoria) ? "Sem categoria" : l.Categoria)
            .Select(g => new CategoriaDespesaDashResponse(g.Key, g.Sum(l => l.Valor)))
            .OrderByDescending(c => c.Total)
            .Take(8)
            .ToList();

        // ── Clientes ─────────────────────────────────────────────────────
        var totalClientes = await db.Clientes.CountAsync(ct);
        var clientesNovosMes = await db.Clientes
            .CountAsync(c => c.DataCadastro >= inicioMes, ct);

        var topClientes = vendasMesLista
            .Where(v => v.ClienteId != null)
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente?.Nome ?? "" })
            .Select(g => new TopClienteResponse(g.Key.Nome, g.Sum(v => v.Total)))
            .OrderByDescending(c => c.TotalGasto)
            .Take(10)
            .ToList();

        // ── Série vendas 7 dias ──────────────────────────────────────────
        var inicio7Dias = hoje.AddDays(-6);
        var vendasSerie = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora.Date >= inicio7Dias)
            .GroupBy(v => v.DataHora.Date)
            .Select(g => new VendasDiaResponse(g.Key, g.Sum(v => v.Total), g.Count()))
            .ToListAsync(ct);

        var vendas7Dias = Enumerable.Range(0, 7)
            .Select(i => inicio7Dias.AddDays(i))
            .Select(d => vendasSerie.FirstOrDefault(v => v.Data == d) ?? new VendasDiaResponse(d, 0m, 0))
            .ToList();

        // ── Fluxo do mês ─────────────────────────────────────────────────
        var fluxoSerie = lancamentosMes
            .GroupBy(l => l.DataPagamento!.Value.Date)
            .Select(g => new FluxoDiaResponse(
                g.Key,
                g.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor),
                g.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor)))
            .OrderBy(f => f.Data)
            .ToList();

        // ── Top 5 produtos ───────────────────────────────────────────────
        var topProdutos = itensDoMes
            .GroupBy(i => new { i.ProdutoId, Nome = i.Produto?.Nome ?? "" })
            .Select(g => new TopProdutoResponse(g.Key.Nome, g.Sum(i => i.Quantidade), g.Sum(i => i.Total)))
            .OrderByDescending(p => p.QuantidadeVendida)
            .Take(5)
            .ToList();

        // ── Top 10 clientes (inclui vendasMesLista com Cliente carregado) ─
        // vendasMesLista não tem Cliente — recarregar com Include
        var vendasMesComCliente = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora >= inicioMes && v.ClienteId != null)
            .Include(v => v.Cliente)
            .ToListAsync(ct);

        var topClientesComNome = vendasMesComCliente
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente?.Nome ?? "" })
            .Select(g => new TopClienteResponse(g.Key.Nome, g.Sum(v => v.Total)))
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
