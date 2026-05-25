using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Relatorios;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Relatorios;

public class RelatorioService(AppDbContext db)
{
    public async Task<KpisGeralResponse> GetKpisGeralAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var vendasPeriodo = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .ToListAsync(ct);

        var faturamento = vendasPeriodo.Sum(v => v.Total);
        var totalVendas = vendasPeriodo.Count;
        var ticketMedio = totalVendas > 0 ? faturamento / totalVendas : 0m;

        var itens = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida
                && i.Venda.DataHora.Date >= de.Date && i.Venda.DataHora.Date <= ate.Date)
            .Include(i => i.Produto)
            .ToListAsync(ct);

        var lucro = itens.Sum(i => (i.PrecoUnitario - (i.Produto?.CustoMedio ?? 0m)) * i.Quantidade);
        var margem = faturamento > 0 ? lucro / faturamento * 100m : 0m;

        var totalReceberPeriodo = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.DataVencimento.Date >= de.Date && l.DataVencimento.Date <= ate.Date)
            .SumAsync(l => l.Valor, ct);

        var vencidas = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date < DateTime.UtcNow.Date)
            .SumAsync(l => l.Valor, ct);

        var inadimplencia = totalReceberPeriodo > 0 ? vencidas / totalReceberPeriodo * 100m : 0m;

        return new KpisGeralResponse(faturamento, ticketMedio, margem, inadimplencia, totalVendas);
    }

    public async Task<RelatorioVendasResponse> GetVendasAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var vendas = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .Include(v => v.Cliente)
            .ToListAsync(ct);

        var tendencia = vendas
            .GroupBy(v => v.DataHora.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TendenciaVendasResponse(g.Key, g.Sum(v => v.Total), g.Count()))
            .ToList();

        var itens = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida
                && i.Venda.DataHora.Date >= de.Date && i.Venda.DataHora.Date <= ate.Date)
            .Include(i => i.Produto)
            .ToListAsync(ct);

        var topProdutos = itens
            .GroupBy(i => new { i.ProdutoId, Nome = i.Produto?.Nome ?? "" })
            .Select(g => new RankingProdutoResponse(g.Key.Nome, g.Sum(i => i.Quantidade), g.Sum(i => i.Total)))
            .OrderByDescending(p => p.Total)
            .Take(10)
            .ToList();

        var topClientes = vendas
            .Where(v => v.ClienteId != null)
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente?.Nome ?? "" })
            .Select(g => new RankingClienteResponse(g.Key.Nome, g.Count(), g.Sum(v => v.Total)))
            .OrderByDescending(c => c.Total)
            .Take(10)
            .ToList();

        var porPagamento = vendas
            .GroupBy(v => v.FormaPagamento)
            .Select(g => new VendasPorPagamentoResponse(g.Key.ToString(), g.Count(), g.Sum(v => v.Total)))
            .ToList();

        return new RelatorioVendasResponse(tendencia, topProdutos, topClientes, porPagamento);
    }

    public async Task<RelatorioFinanceiroResponse> GetFinanceiroAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var lancamentos = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pago
                && l.DataPagamento.HasValue
                && l.DataPagamento!.Value.Date >= de.Date
                && l.DataPagamento.Value.Date <= ate.Date)
            .ToListAsync(ct);

        var fluxoPorDia = lancamentos
            .GroupBy(l => l.DataPagamento!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var r = g.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor);
                var d = g.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor);
                return new FluxoCaixaDiaResponse(g.Key, r, d, r - d);
            })
            .ToList();

        var categoriasDespesas = lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa)
            .GroupBy(l => l.Categoria)
            .Select(g => new CategoriaDespesaResponse(g.Key, g.Sum(l => l.Valor)))
            .OrderByDescending(c => c.Total)
            .ToList();

        var totalReceitas = lancamentos.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor);
        var totalDespesas = lancamentos.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor);

        return new RelatorioFinanceiroResponse(
            totalReceitas, totalDespesas, totalReceitas - totalDespesas,
            fluxoPorDia, categoriasDespesas);
    }

    public async Task<RelatorioEstoqueResponse> GetEstoqueAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var produtos = await db.Produtos.Where(p => p.Ativo).ToListAsync(ct);
        var valorTotal = produtos.Sum(p => p.EstoqueAtual * p.CustoMedio);
        var estoqueBaixo = produtos.Count(p => p.EstoqueAtual <= p.EstoqueMinimo);

        var movimentos = await db.MovimentacoesEstoque
            .Include(m => m.Produto)
            .Where(m => m.DataHora.Date >= de.Date && m.DataHora.Date <= ate.Date)
            .ToListAsync(ct);

        var giro = movimentos
            .GroupBy(m => new { m.ProdutoId, Nome = m.Produto?.Nome ?? "" })
            .Select(g =>
            {
                var entradas = g.Where(m => m.Tipo == TipoMovimentacao.Entrada).Sum(m => m.Quantidade);
                var saidas = g.Where(m => m.Tipo == TipoMovimentacao.Saida).Sum(m => m.Quantidade);
                return new GiroProdutoResponse(g.Key.Nome, entradas, saidas, saidas - entradas);
            })
            .OrderByDescending(g => g.Saidas)
            .Take(20)
            .ToList();

        var produtosMovimentadosIds = movimentos.Select(m => m.ProdutoId).Distinct().ToHashSet();
        var semMovimentacao = produtos
            .Where(p => !produtosMovimentadosIds.Contains(p.Id))
            .Select(p => new ProdutoSemMovimentacaoResponse(
                p.Nome, p.EstoqueAtual, p.EstoqueAtual * p.CustoMedio))
            .OrderByDescending(p => p.ValorEmEstoque)
            .Take(20)
            .ToList();

        return new RelatorioEstoqueResponse(
            valorTotal, produtos.Count, estoqueBaixo, giro, semMovimentacao);
    }

    public async Task<RelatorioClientesResponse> GetClientesAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var totalClientes = await db.Clientes.CountAsync(ct);

        var vendas = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.ClienteId != null
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .Include(v => v.Cliente)
            .ToListAsync(ct);

        var clientesCompraram = vendas.Select(v => v.ClienteId).Distinct().Count();
        var totalFaturado = vendas.Sum(v => v.Total);
        var ticketMedio = clientesCompraram > 0 ? totalFaturado / clientesCompraram : 0m;

        var topClientes = vendas
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente?.Nome ?? "", Whatsapp = v.Cliente?.Whatsapp ?? "" })
            .Select(g => new ClienteRankingResponse(
                g.Key.Nome, g.Key.Whatsapp, g.Count(), g.Sum(v => v.Total)))
            .OrderByDescending(c => c.TotalGasto)
            .Take(10)
            .ToList();

        return new RelatorioClientesResponse(
            totalClientes, clientesCompraram, ticketMedio, topClientes);
    }
}
