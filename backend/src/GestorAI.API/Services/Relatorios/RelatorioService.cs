using System.Globalization;
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
        // ── Vendas ──────────────────────────────────────────────────────────
        var vendasPeriodo = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .Include(v => v.Cliente)
            .ToListAsync(ct);

        var faturamento = vendasPeriodo.Sum(v => v.Total);
        var totalVendas = vendasPeriodo.Count;
        var ticketMedio = totalVendas > 0 ? faturamento / totalVendas : 0m;
        var clientesAtendidos = vendasPeriodo.Where(v => v.ClienteId != null)
            .Select(v => v.ClienteId).Distinct().Count();

        var itens = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida
                && i.Venda.DataHora.Date >= de.Date && i.Venda.DataHora.Date <= ate.Date)
            .Include(i => i.Produto)
            .ToListAsync(ct);

        var lucro = itens.Sum(i => (i.PrecoUnitario - (i.Produto?.CustoMedio ?? 0m)) * i.Quantidade);
        var margem = faturamento > 0 ? Math.Round(lucro / faturamento * 100m, 1) : 0m;

        // ── Financeiro (período) ─────────────────────────────────────────────
        var lancamentosPagos = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pago
                && l.DataPagamento.HasValue
                && l.DataPagamento!.Value.Date >= de.Date
                && l.DataPagamento.Value.Date <= ate.Date)
            .ToListAsync(ct);

        var totalReceitas = lancamentosPagos.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor);
        var totalDespesas = lancamentosPagos.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor);
        var saldoPeriodo = totalReceitas - totalDespesas;

        var totalReceberPeriodo = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.DataVencimento.Date >= de.Date && l.DataVencimento.Date <= ate.Date)
            .SumAsync(l => l.Valor, ct);

        var lancamentosVencidos = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date < DateTime.UtcNow.Date)
            .SumAsync(l => l.Valor, ct);

        var inadimplencia = totalReceberPeriodo > 0
            ? Math.Round(lancamentosVencidos / totalReceberPeriodo * 100m, 1) : 0m;

        // ── Situação atual ───────────────────────────────────────────────────
        var contasReceber = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita && l.Status == StatusLancamento.Pendente)
            .SumAsync(l => l.Valor, ct);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var cobrancasPendentes = await db.Cobrancas
            .Where(c => c.Status == CobrancaStatus.Pendente)
            .ToListAsync(ct);
        var cobrancasVencidas = cobrancasPendentes.Where(c => c.DataVencimento < hoje).ToList();
        var totalVencidoCobrancas = cobrancasVencidas.Sum(c => c.Valor);

        var contratos = await db.Contratos
            .Where(c => c.Status == ContratoStatus.Ativo)
            .ToListAsync(ct);
        var mrrContratos = contratos.Sum(c => c.Periodicidade switch
        {
            Periodicidade.Mensal => c.Valor,
            Periodicidade.Trimestral => c.Valor / 3m,
            Periodicidade.Semestral => c.Valor / 6m,
            Periodicidade.Anual => c.Valor / 12m,
            _ => 0m,
        });

        var orcamentosAbertos = await db.Orcamentos
            .Where(o => o.Status == OrcamentoStatus.Enviado || o.Status == OrcamentoStatus.Aprovado)
            .CountAsync(ct);

        var agendamentosNoPeriodo = await db.Agendamentos
            .Where(a => a.DataHoraInicio.Date >= de.Date && a.DataHoraInicio.Date <= ate.Date)
            .CountAsync(ct);

        var tendenciaVendas = vendasPeriodo
            .GroupBy(v => v.DataHora.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TendenciaVendasResponse(g.Key, g.Sum(v => v.Total), g.Count()))
            .ToList();

        var fluxoPorDia = lancamentosPagos
            .GroupBy(l => l.DataPagamento!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var r = g.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor);
                var d = g.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor);
                return new FluxoCaixaDiaResponse(g.Key, r, d, r - d);
            })
            .ToList();

        var topProdutos = itens
            .GroupBy(i => new { i.ProdutoId, Nome = i.Produto?.Nome ?? "" })
            .Select(g => new RankingProdutoResponse(g.Key.Nome, g.Sum(i => i.Quantidade), g.Sum(i => i.Total)))
            .OrderByDescending(p => p.Total)
            .Take(5)
            .ToList();

        var topClientes = vendasPeriodo
            .Where(v => v.ClienteId != null)
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente?.Nome ?? "" })
            .Select(g => new RankingClienteResponse(g.Key.Nome, g.Count(), g.Sum(v => v.Total)))
            .OrderByDescending(c => c.Total)
            .Take(5)
            .ToList();

        return new KpisGeralResponse(
            faturamento, ticketMedio, margem, totalVendas, clientesAtendidos,
            totalReceitas, totalDespesas, saldoPeriodo, inadimplencia,
            contasReceber, totalVencidoCobrancas, contratos.Count, Math.Round(mrrContratos, 2),
            cobrancasVencidas.Count, orcamentosAbertos, agendamentosNoPeriodo,
            tendenciaVendas, fluxoPorDia, topProdutos, topClientes);
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

    public async Task<CurvaAbcResponse> GetCurvaAbcProdutosAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var itens = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida
                && i.Venda.DataHora.Date >= de.Date && i.Venda.DataHora.Date <= ate.Date)
            .Include(i => i.Produto)
            .ToListAsync(ct);

        var totalGeral = itens.Sum(i => i.Total);

        var agrupados = itens
            .GroupBy(i => new { i.ProdutoId, Nome = i.Produto?.Nome ?? "Sem nome" })
            .Select(g => new { g.Key.Nome, Total = g.Sum(i => i.Total), Quantidade = g.Sum(i => i.Quantidade) })
            .OrderByDescending(p => p.Total)
            .ToList();

        decimal acumulado = 0;
        var resultado = agrupados.Select(p =>
        {
            acumulado += p.Total;
            var pct = totalGeral > 0 ? Math.Round(p.Total / totalGeral * 100m, 2) : 0m;
            var pctAcum = totalGeral > 0 ? Math.Round(acumulado / totalGeral * 100m, 2) : 0m;
            var classe = pctAcum <= 80m ? "A" : pctAcum <= 95m ? "B" : "C";
            return new CurvaAbcItemResponse(p.Nome, p.Quantidade, p.Total, pct, pctAcum, classe);
        }).ToList();

        return new CurvaAbcResponse(resultado, totalGeral);
    }

    public async Task<CurvaAbcResponse> GetCurvaAbcClientesAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var vendas = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .Include(v => v.Cliente)
            .ToListAsync(ct);

        var totalGeral = vendas.Sum(v => v.Total);

        var agrupados = vendas
            .GroupBy(v => new { v.ClienteId, Nome = v.Cliente?.Nome ?? "Sem identificação" })
            .Select(g => new { g.Key.Nome, Total = g.Sum(v => v.Total), Quantidade = (decimal)g.Count() })
            .OrderByDescending(c => c.Total)
            .ToList();

        decimal acumulado = 0;
        var resultado = agrupados.Select(c =>
        {
            acumulado += c.Total;
            var pct = totalGeral > 0 ? Math.Round(c.Total / totalGeral * 100m, 2) : 0m;
            var pctAcum = totalGeral > 0 ? Math.Round(acumulado / totalGeral * 100m, 2) : 0m;
            var classe = pctAcum <= 80m ? "A" : pctAcum <= 95m ? "B" : "C";
            return new CurvaAbcItemResponse(c.Nome, c.Quantidade, c.Total, pct, pctAcum, classe);
        }).ToList();

        return new CurvaAbcResponse(resultado, totalGeral);
    }

    public async Task<DreResponse> GetDreAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var vendas = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.DataHora.Date >= de.Date && v.DataHora.Date <= ate.Date)
            .ToListAsync(ct);

        var receitaBrutaVendas = vendas.Sum(v => v.Total);
        var totalDescontos = vendas.Sum(v => v.Desconto);

        var outrasReceitas = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pago
                && l.VendaId == null
                && l.DataPagamento.HasValue
                && l.DataPagamento!.Value.Date >= de.Date
                && l.DataPagamento.Value.Date <= ate.Date)
            .SumAsync(l => l.Valor, ct);

        var itens = await db.ItensVenda
            .Where(i => i.Venda!.Status == StatusVenda.Concluida
                && i.Venda.DataHora.Date >= de.Date && i.Venda.DataHora.Date <= ate.Date
                && i.Produto!.Tipo == TipoProduto.Produto)
            .Include(i => i.Produto)
            .ToListAsync(ct);

        var cmv = itens.Sum(i => (i.Produto?.CustoMedio ?? 0m) * i.Quantidade);

        var lancamentosDespesa = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa
                && l.Status == StatusLancamento.Pago
                && l.DataPagamento.HasValue
                && l.DataPagamento!.Value.Date >= de.Date
                && l.DataPagamento.Value.Date <= ate.Date)
            .ToListAsync(ct);

        var despesas = lancamentosDespesa
            .GroupBy(l => string.IsNullOrEmpty(l.Categoria) ? "Sem categoria" : l.Categoria)
            .Select(g => new DreLinhaResponse(g.Key, g.Sum(l => l.Valor)))
            .OrderByDescending(d => d.Valor)
            .ToList();

        var totalDespesas = despesas.Sum(d => d.Valor);
        var receitaLiquida = receitaBrutaVendas - totalDescontos + outrasReceitas;
        var lucroBruto = receitaLiquida - cmv;
        var resultadoOperacional = lucroBruto - totalDespesas;

        return new DreResponse(
            receitaBrutaVendas,
            outrasReceitas,
            totalDescontos,
            receitaLiquida,
            cmv,
            lucroBruto,
            receitaLiquida > 0 ? Math.Round(lucroBruto / receitaLiquida * 100m, 1) : 0m,
            despesas,
            totalDespesas,
            resultadoOperacional,
            receitaLiquida > 0 ? Math.Round(resultadoOperacional / receitaLiquida * 100m, 1) : 0m);
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

    public async Task<RelatorioAgendamentosResponse> GetAgendamentosAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var agendamentos = await db.Agendamentos
            .Where(a => a.DataHoraInicio.Date >= de.Date && a.DataHoraInicio.Date <= ate.Date)
            .Include(a => a.Profissional)
            .ToListAsync(ct);

        var total = agendamentos.Count;
        var concluidos = agendamentos.Count(a => a.Status == AgendamentoStatus.Concluido);
        var cancelados = agendamentos.Count(a => a.Status == AgendamentoStatus.Cancelado);
        var naoCancelados = total - cancelados;
        var taxaConclusao = naoCancelados > 0
            ? Math.Round((decimal)concluidos / naoCancelados * 100m, 1) : 0m;
        var taxaOcupacao = Math.Round((concluidos / (decimal)Math.Max(naoCancelados, 1)) * 100m, 1);

        var porStatus = agendamentos
            .GroupBy(a => a.Status.ToString())
            .Select(g => new AgendamentoStatusItemRel(g.Key, g.Count()))
            .ToList();

        var porProfissional = agendamentos
            .GroupBy(a => a.Profissional?.Nome ?? "Sem profissional")
            .Select(g =>
            {
                var tot = g.Count();
                var conc = g.Count(a => a.Status == AgendamentoStatus.Concluido);
                var nCanc = g.Count(a => a.Status != AgendamentoStatus.Cancelado);
                var taxa = nCanc > 0 ? Math.Round((decimal)conc / nCanc * 100m, 1) : 0m;
                return new AgendamentoProfissionalItemRel(g.Key, tot, conc, taxa);
            })
            .ToList();

        return new RelatorioAgendamentosResponse(
            total, concluidos, cancelados, taxaConclusao, taxaOcupacao,
            porStatus, porProfissional);
    }

    public async Task<RelatorioContratosResponse> GetContratosAsync(CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var em30Dias = hoje.AddDays(30);

        var contratos = await db.Contratos
            .Include(c => c.Cliente)
            .ToListAsync(ct);

        var ativos = contratos.Where(c => c.Status == ContratoStatus.Ativo).ToList();

        var mrr = ativos.Sum(c => c.Periodicidade switch
        {
            Periodicidade.Mensal => c.Valor,
            Periodicidade.Trimestral => c.Valor / 3m,
            Periodicidade.Semestral => c.Valor / 6m,
            Periodicidade.Anual => c.Valor / 12m,
            _ => 0m,
        });

        var vencendoEm30 = ativos.Count(c =>
            c.DataFim.HasValue && c.DataFim.Value >= hoje && c.DataFim.Value <= em30Dias);

        var detalhe = contratos
            .Select(c => new ContratoDetalheRel(
                c.Titulo,
                c.Cliente?.Nome ?? "",
                c.Valor,
                c.Periodicidade.ToString(),
                c.DataFim,
                c.Status.ToString()))
            .ToList();

        return new RelatorioContratosResponse(ativos.Count, Math.Round(mrr, 2), vencendoEm30, detalhe);
    }

    public async Task<RelatorioCobrancasResponse> GetCobrancasAsync(CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var cobrancas = await db.Cobrancas
            .Where(c => c.Status == CobrancaStatus.Pendente || c.Status == CobrancaStatus.Pago)
            .Include(c => c.Cliente)
            .OrderBy(c => c.DataVencimento)
            .ToListAsync(ct);

        var pendentes = cobrancas.Where(c => c.Status == CobrancaStatus.Pendente).ToList();
        var totalReceber = pendentes.Sum(c => c.Valor);
        var vencidas = pendentes.Where(c => c.DataVencimento < hoje).ToList();
        var totalVencido = vencidas.Sum(c => c.Valor);
        var taxaInadimplencia = totalReceber > 0
            ? Math.Round(totalVencido / totalReceber * 100m, 1) : 0m;

        var cobrancasDetalhe = cobrancas
            .Select(c =>
            {
                var diasAtraso = c.Status == CobrancaStatus.Pendente && c.DataVencimento < hoje
                    ? (int)(hoje.ToDateTime(TimeOnly.MinValue) - c.DataVencimento.ToDateTime(TimeOnly.MinValue)).TotalDays
                    : 0;
                return new CobrancaDetalheRel(
                    c.Referencia,
                    c.Cliente?.Nome ?? "",
                    c.Valor,
                    c.DataVencimento,
                    c.Status.ToString(),
                    diasAtraso);
            })
            .ToList();

        var vencidasDetalhe = cobrancasDetalhe.Where(c => c.DiasAtraso > 0).ToList();

        var aging = new List<AgingFaixaRel>
        {
            new("1-7 dias",
                vencidasDetalhe.Count(v => v.DiasAtraso >= 1 && v.DiasAtraso <= 7),
                vencidasDetalhe.Where(v => v.DiasAtraso >= 1 && v.DiasAtraso <= 7).Sum(v => v.Valor)),
            new("8-30 dias",
                vencidasDetalhe.Count(v => v.DiasAtraso >= 8 && v.DiasAtraso <= 30),
                vencidasDetalhe.Where(v => v.DiasAtraso >= 8 && v.DiasAtraso <= 30).Sum(v => v.Valor)),
            new("31-60 dias",
                vencidasDetalhe.Count(v => v.DiasAtraso >= 31 && v.DiasAtraso <= 60),
                vencidasDetalhe.Where(v => v.DiasAtraso >= 31 && v.DiasAtraso <= 60).Sum(v => v.Valor)),
            new("+60 dias",
                vencidasDetalhe.Count(v => v.DiasAtraso > 60),
                vencidasDetalhe.Where(v => v.DiasAtraso > 60).Sum(v => v.Valor)),
        };

        return new RelatorioCobrancasResponse(
            totalReceber, totalVencido, vencidas.Count, taxaInadimplencia,
            aging, cobrancasDetalhe);
    }

    public async Task<RelatorioOrcamentosResponse> GetOrcamentosAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var orcamentos = await db.Orcamentos
            .Where(o => o.CriadoEm.Date >= de.Date && o.CriadoEm.Date <= ate.Date)
            .Include(o => o.Itens)
            .Include(o => o.Cliente)
            .ToListAsync(ct);

        var total = orcamentos.Count;
        var naoRascunhoNaoCancelado = orcamentos
            .Where(o => o.Status != OrcamentoStatus.Rascunho && o.Status != OrcamentoStatus.Cancelado)
            .ToList();
        var convertidos = orcamentos.Count(o => o.Status == OrcamentoStatus.Convertido);
        var taxaConversao = naoRascunhoNaoCancelado.Count > 0
            ? Math.Round((decimal)convertidos / naoRascunhoNaoCancelado.Count * 100m, 1) : 0m;

        var abertos = orcamentos
            .Where(o => o.Status == OrcamentoStatus.Enviado || o.Status == OrcamentoStatus.Aprovado);
        var valorPipeline = abertos.Sum(o => o.Itens.Sum(i => i.ValorUnitario * i.Quantidade));

        var porStatus = orcamentos
            .GroupBy(o => o.Status.ToString())
            .Select(g => new OrcamentoStatusItemRel(
                g.Key,
                g.Count(),
                g.Sum(o => o.Itens.Sum(i => i.ValorUnitario * i.Quantidade))))
            .ToList();

        var detalhe = orcamentos
            .Select(o => new OrcamentoDetalheRel(
                o.Numero,
                o.Titulo,
                o.Cliente?.Nome ?? "",
                o.Itens.Sum(i => i.ValorUnitario * i.Quantidade),
                o.Status.ToString(),
                o.CriadoEm))
            .OrderByDescending(o => o.CriadoEm)
            .ToList();

        return new RelatorioOrcamentosResponse(
            total, taxaConversao, Math.Round(valorPipeline, 2),
            porStatus, detalhe);
    }

    public async Task<RelatorioAssinaturasResponse> GetAssinaturasAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var ptBr = CultureInfo.GetCultureInfo("pt-BR");
        var hoje = DateTime.UtcNow.Date;

        var assinaturas = await db.AssinaturasCliente
            .Include(a => a.Cliente)
            .Include(a => a.Plano)
            .ToListAsync(ct);

        var ativas = assinaturas.Where(a => a.Status == AssinaturaStatus.Ativa).ToList();

        var mrr = ativas.Sum(a => a.Plano is null ? 0m : a.Plano.Periodicidade switch
        {
            Periodicidade.Mensal => a.Plano.Preco,
            Periodicidade.Trimestral => a.Plano.Preco / 3m,
            Periodicidade.Semestral => a.Plano.Preco / 6m,
            Periodicidade.Anual => a.Plano.Preco / 12m,
            _ => 0m,
        });

        var canceladasNoPeriodo = assinaturas.Count(a =>
            (a.Status == AssinaturaStatus.Cancelada || a.Status == AssinaturaStatus.Expirada)
            && a.CriadoEm.Date >= de.Date && a.CriadoEm.Date <= ate.Date);

        var taxaChurn = ativas.Count > 0
            ? Math.Round((decimal)canceladasNoPeriodo / (ativas.Count + canceladasNoPeriodo) * 100m, 1) : 0m;

        // Evolucao 12 meses
        var evolucao = new List<EvolucaoAssinaturaMesRel>();
        for (int i = 11; i >= 0; i--)
        {
            var inicioM = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(-i);
            var ultimoDiaMes = inicioM.AddMonths(1).AddDays(-1);

            var ativasNoMes = assinaturas.Count(a =>
                a.Status == AssinaturaStatus.Ativa && a.CriadoEm.Date <= ultimoDiaMes);

            var novasNoMes = assinaturas.Count(a =>
                a.Status == AssinaturaStatus.Ativa
                && a.CriadoEm >= inicioM && a.CriadoEm <= ultimoDiaMes);

            var canceladasNoMes = assinaturas.Count(a =>
                (a.Status == AssinaturaStatus.Cancelada || a.Status == AssinaturaStatus.Expirada)
                && a.CriadoEm >= inicioM);

            evolucao.Add(new EvolucaoAssinaturaMesRel(
                inicioM.ToString("MMM/yy", ptBr),
                ativasNoMes,
                novasNoMes,
                canceladasNoMes));
        }

        var detalhe = assinaturas
            .Select(a => new AssinaturaDetalheRel(
                a.Cliente?.Nome ?? "",
                a.Plano?.Nome ?? "",
                a.Plano?.Preco ?? 0m,
                a.Plano?.Periodicidade.ToString() ?? "",
                a.DataInicio,
                a.DataRenovacao,
                a.Status.ToString()))
            .ToList();

        return new RelatorioAssinaturasResponse(
            ativas.Count, Math.Round(mrr, 2), canceladasNoPeriodo, taxaChurn,
            evolucao, detalhe);
    }
}
