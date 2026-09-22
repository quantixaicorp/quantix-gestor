using System.Globalization;
using GestorAI.API.Domain.Entities;
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
        var vendasPeriodo = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida
                && v.SaleDate.Date >= de.Date && v.SaleDate.Date <= ate.Date)
            .Include(v => v.Customer)
            .ToListAsync(ct);

        var faturamento = vendasPeriodo.Sum(v => v.Total);
        var totalVendas = vendasPeriodo.Count;
        var ticketMedio = totalVendas > 0 ? faturamento / totalVendas : 0m;
        var clientesAtendidos = vendasPeriodo.Where(v => v.CustomerId != null)
            .Select(v => v.CustomerId).Distinct().Count();

        var itens = await db.SaleItems
            .Where(i => i.Sale!.Status == StatusVenda.Concluida
                && i.Sale.SaleDate.Date >= de.Date && i.Sale.SaleDate.Date <= ate.Date)
            .Include(i => i.Product)
            .ToListAsync(ct);

        var lucro = itens.Sum(i => (i.UnitPrice - (i.Product?.AverageCost ?? 0m)) * i.Quantity);
        var margem = faturamento > 0 ? Math.Round(lucro / faturamento * 100m, 1) : 0m;

        // ── Financeiro (período) ─────────────────────────────────────────────
        var lancamentosPagos = await db.Transactions
            .Where(l => l.Status == StatusLancamento.Pago
                && l.PaymentDate.HasValue
                && l.PaymentDate!.Value.Date >= de.Date
                && l.PaymentDate.Value.Date <= ate.Date)
            .ToListAsync(ct);

        var totalReceitas = lancamentosPagos.Where(l => l.Type == TipoLancamento.Receita).Sum(l => l.Amount);
        var totalDespesas = lancamentosPagos.Where(l => l.Type == TipoLancamento.Despesa).Sum(l => l.Amount);
        var saldoPeriodo = totalReceitas - totalDespesas;

        var totalReceberPeriodo = await db.Transactions
            .Where(l => l.Type == TipoLancamento.Receita
                && l.DueDate.Date >= de.Date && l.DueDate.Date <= ate.Date)
            .SumAsync(l => l.Amount, ct);

        var lancamentosVencidos = await db.Transactions
            .Where(l => l.Type == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pendente
                && l.DueDate.Date < DateTime.UtcNow.Date)
            .SumAsync(l => l.Amount, ct);

        var inadimplencia = totalReceberPeriodo > 0
            ? Math.Round(lancamentosVencidos / totalReceberPeriodo * 100m, 1) : 0m;

        // ── Situação atual ───────────────────────────────────────────────────
        var contasReceber = await db.Transactions
            .Where(l => l.Type == TipoLancamento.Receita && l.Status == StatusLancamento.Pendente)
            .SumAsync(l => l.Amount, ct);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var cobrancasPendentes = await db.Charges
            .Where(c => c.Status == CobrancaStatus.Pendente)
            .ToListAsync(ct);
        var cobrancasVencidas = cobrancasPendentes.Where(c => c.DueDate < hoje).ToList();
        var totalVencidoCobrancas = cobrancasVencidas.Sum(c => c.Amount);

        var contratos = await db.Contracts
            .Where(c => c.Status == ContratoStatus.Ativo)
            .ToListAsync(ct);
        var mrrContratos = contratos.Sum(c => c.Frequency switch
        {
            Periodicidade.Mensal => c.Amount,
            Periodicidade.Trimestral => c.Amount / 3m,
            Periodicidade.Semestral => c.Amount / 6m,
            Periodicidade.Anual => c.Amount / 12m,
            _ => 0m,
        });

        var orcamentosAbertos = await db.Quotes
            .Where(o => o.Status == OrcamentoStatus.Enviado || o.Status == OrcamentoStatus.Aprovado)
            .CountAsync(ct);

        var agendamentosNoPeriodo = await db.Appointments
            .Where(a => a.StartAt.Date >= de.Date && a.StartAt.Date <= ate.Date)
            .CountAsync(ct);

        var tendenciaVendas = vendasPeriodo
            .GroupBy(v => v.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TendenciaVendasResponse(g.Key, g.Sum(v => v.Total), g.Count()))
            .ToList();

        var fluxoPorDia = lancamentosPagos
            .GroupBy(l => l.PaymentDate!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var r = g.Where(l => l.Type == TipoLancamento.Receita).Sum(l => l.Amount);
                var d = g.Where(l => l.Type == TipoLancamento.Despesa).Sum(l => l.Amount);
                return new FluxoCaixaDiaResponse(g.Key, r, d, r - d);
            })
            .ToList();

        var topProdutos = itens
            .GroupBy(i => new { i.ProductId, Name = i.Product?.Name ?? "" })
            .Select(g => new RankingProdutoResponse(g.Key.Name, g.Sum(i => i.Quantity), g.Sum(i => i.Total)))
            .OrderByDescending(p => p.Total)
            .Take(5)
            .ToList();

        var topClientes = vendasPeriodo
            .Where(v => v.CustomerId != null)
            .GroupBy(v => new { v.CustomerId, Name = v.Customer?.Name ?? "" })
            .Select(g => new RankingClienteResponse(g.Key.Name, g.Count(), g.Sum(v => v.Total)))
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
        var vendas = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida
                && v.SaleDate.Date >= de.Date && v.SaleDate.Date <= ate.Date)
            .Include(v => v.Customer)
            .ToListAsync(ct);

        var tendencia = vendas
            .GroupBy(v => v.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TendenciaVendasResponse(g.Key, g.Sum(v => v.Total), g.Count()))
            .ToList();

        var itens = await db.SaleItems
            .Where(i => i.Sale!.Status == StatusVenda.Concluida
                && i.Sale.SaleDate.Date >= de.Date && i.Sale.SaleDate.Date <= ate.Date)
            .Include(i => i.Product)
            .ToListAsync(ct);

        var topProdutos = itens
            .GroupBy(i => new { i.ProductId, Name = i.Product?.Name ?? "" })
            .Select(g => new RankingProdutoResponse(g.Key.Name, g.Sum(i => i.Quantity), g.Sum(i => i.Total)))
            .OrderByDescending(p => p.Total)
            .Take(10)
            .ToList();

        var topClientes = vendas
            .Where(v => v.CustomerId != null)
            .GroupBy(v => new { v.CustomerId, Name = v.Customer?.Name ?? "" })
            .Select(g => new RankingClienteResponse(g.Key.Name, g.Count(), g.Sum(v => v.Total)))
            .OrderByDescending(c => c.Total)
            .Take(10)
            .ToList();

        var porPagamento = vendas
            .GroupBy(v => v.PaymentMethod)
            .Select(g => new VendasPorPagamentoResponse(g.Key.ToString(), g.Count(), g.Sum(v => v.Total)))
            .ToList();

        return new RelatorioVendasResponse(tendencia, topProdutos, topClientes, porPagamento);
    }

    public async Task<RelatorioFinanceiroResponse> GetFinanceiroAsync(
        DateTime de, DateTime ate, string tipoData, CancellationToken ct)
    {
        IQueryable<Transaction> query;
        if (tipoData == "vencimento")
        {
            query = db.Transactions.Where(l =>
                l.DueDate.Date >= de.Date && l.DueDate.Date <= ate.Date);
        }
        else
        {
            // default: pagamento
            query = db.Transactions.Where(l =>
                l.Status == StatusLancamento.Pago
                && l.PaymentDate.HasValue
                && l.PaymentDate!.Value.Date >= de.Date
                && l.PaymentDate.Value.Date <= ate.Date);
        }

        var lancamentos = await query.ToListAsync(ct);

        var dataRef = (Transaction l) => tipoData == "vencimento"
            ? l.DueDate.Date
            : l.PaymentDate!.Value.Date;

        var fluxoPorDia = lancamentos
            .Where(l => tipoData != "pagamento" || l.PaymentDate.HasValue)
            .GroupBy(dataRef)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var r = g.Where(l => l.Type == TipoLancamento.Receita).Sum(l => l.Amount);
                var d = g.Where(l => l.Type == TipoLancamento.Despesa).Sum(l => l.Amount);
                return new FluxoCaixaDiaResponse(g.Key, r, d, r - d);
            })
            .ToList();

        var categoriasDespesas = lancamentos
            .Where(l => l.Type == TipoLancamento.Despesa)
            .GroupBy(l => l.Category)
            .Select(g => new CategoriaDespesaResponse(g.Key, g.Sum(l => l.Amount)))
            .OrderByDescending(c => c.Total)
            .ToList();

        var totalReceitas = lancamentos.Where(l => l.Type == TipoLancamento.Receita).Sum(l => l.Amount);
        var totalDespesas = lancamentos.Where(l => l.Type == TipoLancamento.Despesa).Sum(l => l.Amount);

        var analitico = lancamentos
            .OrderByDescending(l => l.DueDate)
            .Select(l => new LancamentoAnaliticoResponse(
                l.Id,
                l.Type.ToString(),
                l.Description,
                l.Category,
                l.Amount,
                l.DueDate,
                l.PaymentDate,
                l.Status.ToString()))
            .ToList();

        return new RelatorioFinanceiroResponse(
            totalReceitas, totalDespesas, totalReceitas - totalDespesas,
            fluxoPorDia, categoriasDespesas, analitico);
    }

    public async Task<RelatorioEstoqueResponse> GetEstoqueAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var produtos = await db.Products.Where(p => p.IsActive).ToListAsync(ct);
        var valorTotal = produtos.Sum(p => p.CurrentStock * p.AverageCost);
        var estoqueBaixo = produtos.Count(p => p.CurrentStock <= p.MinimumStock);

        var movimentos = await db.StockMovements
            .Include(m => m.Product)
            .Where(m => m.MovementDate.Date >= de.Date && m.MovementDate.Date <= ate.Date)
            .ToListAsync(ct);

        var giro = movimentos
            .GroupBy(m => new { m.ProductId, Name = m.Product?.Name ?? "" })
            .Select(g =>
            {
                var entradas = g.Where(m => m.Type == TipoMovimentacao.Entrada).Sum(m => m.Quantity);
                var saidas = g.Where(m => m.Type == TipoMovimentacao.Saida).Sum(m => m.Quantity);
                return new GiroProdutoResponse(g.Key.Name, entradas, saidas, saidas - entradas);
            })
            .OrderByDescending(g => g.Saidas)
            .Take(20)
            .ToList();

        var produtosMovimentadosIds = movimentos.Select(m => m.ProductId).Distinct().ToHashSet();
        var semMovimentacao = produtos
            .Where(p => !produtosMovimentadosIds.Contains(p.Id))
            .Select(p => new ProdutoSemMovimentacaoResponse(
                p.Name, p.CurrentStock, p.CurrentStock * p.AverageCost))
            .OrderByDescending(p => p.ValorEmEstoque)
            .Take(20)
            .ToList();

        return new RelatorioEstoqueResponse(
            valorTotal, produtos.Count, estoqueBaixo, giro, semMovimentacao);
    }

    public async Task<CurvaAbcResponse> GetCurvaAbcProdutosAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var itens = await db.SaleItems
            .Where(i => i.Sale!.Status == StatusVenda.Concluida
                && i.Sale.SaleDate.Date >= de.Date && i.Sale.SaleDate.Date <= ate.Date)
            .Include(i => i.Product)
            .ToListAsync(ct);

        var totalGeral = itens.Sum(i => i.Total);

        var agrupados = itens
            .GroupBy(i => new { i.ProductId, Name = i.Product?.Name ?? "Sem nome" })
            .Select(g => new { g.Key.Name, Total = g.Sum(i => i.Total), Quantity = g.Sum(i => i.Quantity) })
            .OrderByDescending(p => p.Total)
            .ToList();

        decimal acumulado = 0;
        var resultado = agrupados.Select(p =>
        {
            acumulado += p.Total;
            var pct = totalGeral > 0 ? Math.Round(p.Total / totalGeral * 100m, 2) : 0m;
            var pctAcum = totalGeral > 0 ? Math.Round(acumulado / totalGeral * 100m, 2) : 0m;
            var classe = pctAcum <= 80m ? "A" : pctAcum <= 95m ? "B" : "C";
            return new CurvaAbcItemResponse(p.Name, p.Quantity, p.Total, pct, pctAcum, classe);
        }).ToList();

        return new CurvaAbcResponse(resultado, totalGeral);
    }

    public async Task<CurvaAbcResponse> GetCurvaAbcClientesAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var vendas = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida
                && v.SaleDate.Date >= de.Date && v.SaleDate.Date <= ate.Date)
            .Include(v => v.Customer)
            .ToListAsync(ct);

        var totalGeral = vendas.Sum(v => v.Total);

        var agrupados = vendas
            .GroupBy(v => new { v.CustomerId, Name = v.Customer?.Name ?? "Sem identificação" })
            .Select(g => new { g.Key.Name, Total = g.Sum(v => v.Total), Quantity = (decimal)g.Count() })
            .OrderByDescending(c => c.Total)
            .ToList();

        decimal acumulado = 0;
        var resultado = agrupados.Select(c =>
        {
            acumulado += c.Total;
            var pct = totalGeral > 0 ? Math.Round(c.Total / totalGeral * 100m, 2) : 0m;
            var pctAcum = totalGeral > 0 ? Math.Round(acumulado / totalGeral * 100m, 2) : 0m;
            var classe = pctAcum <= 80m ? "A" : pctAcum <= 95m ? "B" : "C";
            return new CurvaAbcItemResponse(c.Name, c.Quantity, c.Total, pct, pctAcum, classe);
        }).ToList();

        return new CurvaAbcResponse(resultado, totalGeral);
    }

    public async Task<DreResponse> GetDreAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var vendas = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida
                && v.SaleDate.Date >= de.Date && v.SaleDate.Date <= ate.Date)
            .ToListAsync(ct);

        var receitaBrutaVendas = vendas.Sum(v => v.Total);
        var totalDescontos = vendas.Sum(v => v.Discount);

        var outrasReceitas = await db.Transactions
            .Where(l => l.Type == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pago
                && l.SaleId == null
                && l.PaymentDate.HasValue
                && l.PaymentDate!.Value.Date >= de.Date
                && l.PaymentDate.Value.Date <= ate.Date)
            .SumAsync(l => l.Amount, ct);

        var itens = await db.SaleItems
            .Where(i => i.Sale!.Status == StatusVenda.Concluida
                && i.Sale.SaleDate.Date >= de.Date && i.Sale.SaleDate.Date <= ate.Date
                && i.Product!.Type == TipoProduto.Produto)
            .Include(i => i.Product)
            .ToListAsync(ct);

        var cmv = itens.Sum(i => (i.Product?.AverageCost ?? 0m) * i.Quantity);

        var lancamentosDespesa = await db.Transactions
            .Where(l => l.Type == TipoLancamento.Despesa
                && l.Status == StatusLancamento.Pago
                && l.PaymentDate.HasValue
                && l.PaymentDate!.Value.Date >= de.Date
                && l.PaymentDate.Value.Date <= ate.Date)
            .ToListAsync(ct);

        var despesas = lancamentosDespesa
            .GroupBy(l => string.IsNullOrEmpty(l.Category) ? "Sem categoria" : l.Category)
            .Select(g => new DreLinhaResponse(g.Key, g.Sum(l => l.Amount)))
            .OrderByDescending(d => d.Amount)
            .ToList();

        var totalDespesas = despesas.Sum(d => d.Amount);
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
        var totalClientes = await db.Customers.CountAsync(ct);

        var vendas = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida
                && v.CustomerId != null
                && v.SaleDate.Date >= de.Date && v.SaleDate.Date <= ate.Date)
            .Include(v => v.Customer)
            .ToListAsync(ct);

        var clientesCompraram = vendas.Select(v => v.CustomerId).Distinct().Count();
        var totalFaturado = vendas.Sum(v => v.Total);
        var ticketMedio = clientesCompraram > 0 ? totalFaturado / clientesCompraram : 0m;

        var topClientes = vendas
            .GroupBy(v => new { v.CustomerId, Name = v.Customer?.Name ?? "", WhatsApp = v.Customer?.WhatsApp ?? "" })
            .Select(g => new ClienteRankingResponse(
                g.Key.Name, g.Key.WhatsApp, g.Count(), g.Sum(v => v.Total)))
            .OrderByDescending(c => c.TotalGasto)
            .Take(10)
            .ToList();

        return new RelatorioClientesResponse(
            totalClientes, clientesCompraram, ticketMedio, topClientes);
    }

    public async Task<RelatorioAgendamentosResponse> GetAgendamentosAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var agendamentos = await db.Appointments
            .Where(a => a.StartAt.Date >= de.Date && a.StartAt.Date <= ate.Date)
            .Include(a => a.Professional)
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
            .GroupBy(a => a.Professional?.Name ?? "Sem profissional")
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

        var contratos = await db.Contracts
            .Include(c => c.Customer)
            .ToListAsync(ct);

        var ativos = contratos.Where(c => c.Status == ContratoStatus.Ativo).ToList();

        var mrr = ativos.Sum(c => c.Frequency switch
        {
            Periodicidade.Mensal => c.Amount,
            Periodicidade.Trimestral => c.Amount / 3m,
            Periodicidade.Semestral => c.Amount / 6m,
            Periodicidade.Anual => c.Amount / 12m,
            _ => 0m,
        });

        var vencendoEm30 = ativos.Count(c =>
            c.EndDate.HasValue && c.EndDate.Value >= hoje && c.EndDate.Value <= em30Dias);

        var detalhe = contratos
            .Select(c => new ContratoDetalheRel(
                c.Title,
                c.Customer?.Name ?? "",
                c.Amount,
                c.Frequency.ToString(),
                c.EndDate,
                c.Status.ToString()))
            .ToList();

        return new RelatorioContratosResponse(ativos.Count, Math.Round(mrr, 2), vencendoEm30, detalhe);
    }

    public async Task<RelatorioCobrancasResponse> GetCobrancasAsync(CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        var cobrancas = await db.Charges
            .Where(c => c.Status == CobrancaStatus.Pendente || c.Status == CobrancaStatus.Pago)
            .Include(c => c.Customer)
            .OrderBy(c => c.DueDate)
            .ToListAsync(ct);

        var pendentes = cobrancas.Where(c => c.Status == CobrancaStatus.Pendente).ToList();
        var totalReceber = pendentes.Sum(c => c.Amount);
        var vencidas = pendentes.Where(c => c.DueDate < hoje).ToList();
        var totalVencido = vencidas.Sum(c => c.Amount);
        var taxaInadimplencia = totalReceber > 0
            ? Math.Round(totalVencido / totalReceber * 100m, 1) : 0m;

        var cobrancasDetalhe = cobrancas
            .Select(c =>
            {
                var diasAtraso = c.Status == CobrancaStatus.Pendente && c.DueDate < hoje
                    ? (int)(hoje.ToDateTime(TimeOnly.MinValue) - c.DueDate.ToDateTime(TimeOnly.MinValue)).TotalDays
                    : 0;
                return new CobrancaDetalheRel(
                    c.Reference,
                    c.Customer?.Name ?? "",
                    c.Amount,
                    c.DueDate,
                    c.Status.ToString(),
                    diasAtraso);
            })
            .ToList();

        var vencidasDetalhe = cobrancasDetalhe.Where(c => c.DiasAtraso > 0).ToList();

        var aging = new List<AgingFaixaRel>
        {
            new("1-7 dias",
                vencidasDetalhe.Count(v => v.DiasAtraso >= 1 && v.DiasAtraso <= 7),
                vencidasDetalhe.Where(v => v.DiasAtraso >= 1 && v.DiasAtraso <= 7).Sum(v => v.Amount)),
            new("8-30 dias",
                vencidasDetalhe.Count(v => v.DiasAtraso >= 8 && v.DiasAtraso <= 30),
                vencidasDetalhe.Where(v => v.DiasAtraso >= 8 && v.DiasAtraso <= 30).Sum(v => v.Amount)),
            new("31-60 dias",
                vencidasDetalhe.Count(v => v.DiasAtraso >= 31 && v.DiasAtraso <= 60),
                vencidasDetalhe.Where(v => v.DiasAtraso >= 31 && v.DiasAtraso <= 60).Sum(v => v.Amount)),
            new("+60 dias",
                vencidasDetalhe.Count(v => v.DiasAtraso > 60),
                vencidasDetalhe.Where(v => v.DiasAtraso > 60).Sum(v => v.Amount)),
        };

        return new RelatorioCobrancasResponse(
            totalReceber, totalVencido, vencidas.Count, taxaInadimplencia,
            aging, cobrancasDetalhe);
    }

    public async Task<RelatorioOrcamentosResponse> GetOrcamentosAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var orcamentos = await db.Quotes
            .Where(o => o.CreatedAt.Date >= de.Date && o.CreatedAt.Date <= ate.Date)
            .Include(o => o.Items)
            .Include(o => o.Customer)
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
        var valorPipeline = abertos.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity));

        var porStatus = orcamentos
            .GroupBy(o => o.Status.ToString())
            .Select(g => new OrcamentoStatusItemRel(
                g.Key,
                g.Count(),
                g.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity))))
            .ToList();

        var detalhe = orcamentos
            .Select(o => new OrcamentoDetalheRel(
                o.Number,
                o.Title,
                o.Customer?.Name ?? "",
                o.Items.Sum(i => i.UnitPrice * i.Quantity),
                o.Status.ToString(),
                o.CreatedAt))
            .OrderByDescending(o => o.CreatedAt)
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

        var assinaturas = await db.CustomerSubscriptions
            .Include(a => a.Customer)
            .Include(a => a.Plan)
            .ToListAsync(ct);

        var ativas = assinaturas.Where(a => a.Status == AssinaturaStatus.Ativa).ToList();

        var mrr = ativas.Sum(a => a.Plan is null ? 0m : a.Plan.Frequency switch
        {
            Periodicidade.Mensal => a.Plan.Price,
            Periodicidade.Trimestral => a.Plan.Price / 3m,
            Periodicidade.Semestral => a.Plan.Price / 6m,
            Periodicidade.Anual => a.Plan.Price / 12m,
            _ => 0m,
        });

        var canceladasNoPeriodo = assinaturas.Count(a =>
            (a.Status == AssinaturaStatus.Cancelada || a.Status == AssinaturaStatus.Expirada)
            && a.CreatedAt.Date >= de.Date && a.CreatedAt.Date <= ate.Date);

        var taxaChurn = ativas.Count > 0
            ? Math.Round((decimal)canceladasNoPeriodo / (ativas.Count + canceladasNoPeriodo) * 100m, 1) : 0m;

        // Evolucao 12 meses
        var evolucao = new List<EvolucaoAssinaturaMesRel>();
        for (int i = 11; i >= 0; i--)
        {
            var inicioM = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(-i);
            var ultimoDiaMes = inicioM.AddMonths(1).AddDays(-1);

            var ativasNoMes = assinaturas.Count(a =>
                a.Status == AssinaturaStatus.Ativa && a.CreatedAt.Date <= ultimoDiaMes);

            var novasNoMes = assinaturas.Count(a =>
                a.Status == AssinaturaStatus.Ativa
                && a.CreatedAt >= inicioM && a.CreatedAt <= ultimoDiaMes);

            var canceladasNoMes = assinaturas.Count(a =>
                (a.Status == AssinaturaStatus.Cancelada || a.Status == AssinaturaStatus.Expirada)
                && a.CreatedAt >= inicioM);

            evolucao.Add(new EvolucaoAssinaturaMesRel(
                inicioM.ToString("MMM/yy", ptBr),
                ativasNoMes,
                novasNoMes,
                canceladasNoMes));
        }

        var detalhe = assinaturas
            .Select(a => new AssinaturaDetalheRel(
                a.Customer?.Name ?? "",
                a.Plan?.Name ?? "",
                a.Plan?.Price ?? 0m,
                a.Plan?.Frequency.ToString() ?? "",
                a.StartDate,
                a.RenewalDate,
                a.Status.ToString()))
            .ToList();

        return new RelatorioAssinaturasResponse(
            ativas.Count, Math.Round(mrr, 2), canceladasNoPeriodo, taxaChurn,
            evolucao, detalhe);
    }

    // ── Histórico de Clientes (lifetime) ─────────────────────────────────────
    private static int? TempoMedioEntreCompras(int qtd, DateTime primeira, DateTime ultima)
        => qtd >= 2 ? (int)Math.Round((ultima - primeira).TotalDays / (qtd - 1)) : null;

    private static (string Classificacao, bool EmRisco) Classificar(int qtd, int diasDesdeUltima)
    {
        if (diasDesdeUltima > 90) return ("Inativo", false);
        if (qtd == 1) return ("Novo", false);
        // qtd >= 2 && diasDesdeUltima <= 90 => Recorrente (em risco se 61-90 dias)
        return ("Recorrente", diasDesdeUltima >= 61);
    }

    public async Task<HistoricoClientesResponse> GetHistoricoClientesAsync(CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;

        // Agregação no banco (GROUP BY) — não materializa todas as vendas.
        var agregados = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida && v.CustomerId != null)
            .GroupBy(v => v.CustomerId!.Value)
            .Select(g => new
            {
                CustomerId = g.Key,
                Qtd = g.Count(),
                Total = g.Sum(v => v.Total),
                Primeira = g.Min(v => v.SaleDate),
                Ultima = g.Max(v => v.SaleDate),
            })
            .ToListAsync(ct);

        var ids = agregados.Select(a => a.CustomerId).ToList();
        var clientes = await db.Customers
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.Name, c.WhatsApp })
            .ToListAsync(ct);
        var clienteMap = clientes.ToDictionary(c => c.Id);

        var itens = agregados
            .Select(a =>
            {
                var diasDesdeUltima = (int)(hoje - a.Ultima.Date).TotalDays;
                var (classificacao, _) = Classificar(a.Qtd, diasDesdeUltima);
                clienteMap.TryGetValue(a.CustomerId, out var c);
                return new HistoricoClienteItemResponse(
                    a.CustomerId,
                    c?.Name ?? "Sem identificação",
                    c?.WhatsApp ?? "",
                    a.Qtd,
                    a.Total,
                    a.Qtd > 0 ? a.Total / a.Qtd : 0m,
                    a.Primeira,
                    a.Ultima,
                    TempoMedioEntreCompras(a.Qtd, a.Primeira, a.Ultima),
                    diasDesdeUltima,
                    classificacao);
            })
            .OrderByDescending(c => c.TotalGasto)
            .ToList();

        var emRisco = agregados.Count(a =>
            Classificar(a.Qtd, (int)(hoje - a.Ultima.Date).TotalDays).EmRisco);

        var totalClientes = itens.Count;
        var recorrentes = itens.Count(i => i.Classificacao == "Recorrente");
        var inativos = itens.Count(i => i.Classificacao == "Inativo");
        var novos = itens.Count(i => i.Classificacao == "Novo");

        var ltvMedio = totalClientes > 0 ? itens.Average(i => i.TotalGasto) : 0m;
        var totalPedidos = itens.Sum(i => i.QtdPedidos);
        var ticketMedioGeral = totalPedidos > 0 ? itens.Sum(i => i.TotalGasto) / totalPedidos : 0m;

        var comTempo = itens.Where(i => i.TempoMedioEntreComprasDias.HasValue).ToList();
        int? tempoMedioGeral = comTempo.Count > 0
            ? (int)Math.Round(comTempo.Average(i => i.TempoMedioEntreComprasDias!.Value))
            : null;

        return new HistoricoClientesResponse(
            totalClientes, recorrentes, inativos, novos, emRisco,
            Math.Round(ltvMedio, 2), Math.Round(ticketMedioGeral, 2), tempoMedioGeral,
            itens);
    }

    public async Task<HistoricoClienteDetalheResponse?> GetHistoricoClienteDetalheAsync(
        Guid clienteId, CancellationToken ct)
    {
        var cliente = await db.Customers.FirstOrDefaultAsync(c => c.Id == clienteId, ct);
        if (cliente is null) return null;

        var vendas = await db.Sales
            .Where(v => v.CustomerId == clienteId && v.Status == StatusVenda.Concluida)
            .Include(v => v.Items)
            .OrderByDescending(v => v.SaleDate)
            .ToListAsync(ct);

        var compras = vendas
            .Select(v => new CompraHistoricoItemResponse(
                v.Id,
                v.SaleDate,
                v.Items.Count,
                v.Total,
                v.PaymentMethod.ToString(),
                v.Status.ToString()))
            .ToList();

        var qtd = vendas.Count;
        var total = vendas.Sum(v => v.Total);
        var primeira = qtd > 0 ? vendas.Min(v => v.SaleDate) : cliente.CreatedAt;
        var ultima = qtd > 0 ? vendas.Max(v => v.SaleDate) : cliente.CreatedAt;
        var diasDesdeUltima = (int)(DateTime.UtcNow.Date - ultima.Date).TotalDays;
        var classificacao = qtd > 0 ? Classificar(qtd, diasDesdeUltima).Classificacao : "Sem compras";

        return new HistoricoClienteDetalheResponse(
            cliente.Id,
            cliente.Name,
            cliente.WhatsApp,
            cliente.Email,
            cliente.CreatedAt,
            qtd,
            total,
            qtd > 0 ? total / qtd : 0m,
            primeira,
            ultima,
            TempoMedioEntreCompras(qtd, primeira, ultima),
            classificacao,
            compras);
    }
}
