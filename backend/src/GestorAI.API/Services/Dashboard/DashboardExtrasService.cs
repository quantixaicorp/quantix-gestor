using System.Globalization;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Dashboard;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Dashboard;

public class DashboardExtrasService(AppDbContext db)
{
    public async Task<DashboardExtrasResponse> GetAsync(CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var ptBr = CultureInfo.GetCultureInfo("pt-BR");

        // MaiorVendaDia — máximo de Total entre vendas Concluidas de hoje
        var maiorVendaDia = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora.Date == hoje)
            .Select(v => (decimal?)v.Total)
            .MaxAsync(ct) ?? 0m;

        // UltimasVendas — últimas 10 vendas Concluidas
        var ultimasVendas = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida)
            .Include(v => v.Cliente)
            .OrderByDescending(v => v.DataHora)
            .Take(10)
            .Select(v => new UltimaVendaResponse(
                v.Id,
                v.DataHora,
                v.Cliente != null ? v.Cliente.Nome : "Consumidor",
                v.Total,
                v.FormaPagamento.ToString()))
            .ToListAsync(ct);

        // VendasPorFormaPgto — vendas Concluidas do mês corrente agrupadas por FormaPagamento
        var vendasMes = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida && v.DataHora >= inicioMes)
            .ToListAsync(ct);

        var vendasPorFormaPgto = vendasMes
            .GroupBy(v => v.FormaPagamento.ToString())
            .Select(g => new VendaFormaPgtoResponse(g.Key, g.Count(), g.Sum(v => v.Total)))
            .ToList();

        // ReceitasPorCategoria — Lancamentos Receita Pago com DataPagamento no mês corrente
        var lancamentosReceitaMes = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pago
                && l.DataPagamento.HasValue
                && l.DataPagamento!.Value >= inicioMes
                && l.DataPagamento.Value < inicioMes.AddMonths(1))
            .ToListAsync(ct);

        var receitasPorCategoria = lancamentosReceitaMes
            .GroupBy(l => l.Categoria)
            .Select(g => new ReceitaCategoriaResponse(g.Key, g.Sum(l => l.Valor)))
            .OrderByDescending(r => r.Total)
            .ToList();

        // FluxoAnual — últimos 12 meses
        var fluxoAnual = new List<FluxoMensalResponse>();
        for (int i = 11; i >= 0; i--)
        {
            var inicioM = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(-i);
            var fimM = inicioM.AddMonths(1);
            var lancMes = await db.Lancamentos
                .Where(l => l.Status == StatusLancamento.Pago
                    && l.DataPagamento.HasValue
                    && l.DataPagamento!.Value >= inicioM
                    && l.DataPagamento.Value < fimM)
                .ToListAsync(ct);

            var receitas = lancMes.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor);
            var despesas = lancMes.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor);
            fluxoAnual.Add(new FluxoMensalResponse(
                inicioM.ToString("MMM/yy", ptBr),
                receitas,
                despesas,
                receitas - despesas));
        }

        // ContasVencidas — Lancamentos Despesa Pendente com DataVencimento.Date < hoje
        var contasVencidas = await db.Lancamentos
            .Where(l => l.Tipo == TipoLancamento.Despesa
                && l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date < hoje)
            .ToListAsync(ct);

        var contasVencidasResp = contasVencidas
            .Select(l => new ContaVencidaDetalheResponse(
                l.Id,
                l.Descricao,
                l.Categoria,
                l.Valor,
                l.DataVencimento,
                (int)(hoje - l.DataVencimento.Date).TotalDays))
            .ToList();

        // ProximosVencimentos — Lancamentos Pendente com DataVencimento nos próximos 7 dias
        var em7Dias = hoje.AddDays(7);
        var proximosVencimentos = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pendente
                && l.DataVencimento.Date >= hoje
                && l.DataVencimento.Date <= em7Dias)
            .ToListAsync(ct);

        var proximosVencimentosResp = proximosVencimentos
            .Select(l => new ProximoVencimentoResponse(
                l.Id,
                l.Descricao,
                l.Categoria,
                l.Valor,
                l.DataVencimento,
                (int)(l.DataVencimento.Date - hoje).TotalDays))
            .ToList();

        // ProdutosAtivos
        var produtosAtivos = await db.Produtos.CountAsync(p => p.Ativo, ct);

        // DistribuicaoCategorias — Produtos Ativos agrupados por Categoria
        var produtosComCategoria = await db.Produtos
            .Where(p => p.Ativo)
            .Include(p => p.Categoria)
            .ToListAsync(ct);

        var distribuicaoCategorias = produtosComCategoria
            .GroupBy(p => p.Categoria?.Nome ?? "Sem categoria")
            .Select(g => new EstoqueCategoriaResponse(
                g.Key,
                g.Count(),
                g.Sum(p => p.EstoqueAtual * p.CustoMedio)))
            .ToList();

        // EstoqueBaixo — Produtos com Ativo e EstoqueAtual <= EstoqueMinimo
        var estoqueBaixo = produtosComCategoria
            .Where(p => p.EstoqueAtual <= p.EstoqueMinimo)
            .Select(p => new EstoqueBaixoDetalheResponse(
                p.Nome,
                p.EstoqueAtual,
                p.EstoqueMinimo,
                p.PrecoVenda))
            .ToList();

        // ClientesInativos — sem venda Concluida nos últimos 90 dias
        var idsAtivos = await db.Vendas
            .Where(v => v.Status == StatusVenda.Concluida
                && v.ClienteId != null
                && v.DataHora >= hoje.AddDays(-90))
            .Select(v => v.ClienteId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var clientesInativos = await db.Clientes.CountAsync(c => !idsAtivos.Contains(c.Id), ct);

        return new DashboardExtrasResponse(
            maiorVendaDia,
            ultimasVendas,
            vendasPorFormaPgto,
            receitasPorCategoria,
            fluxoAnual,
            contasVencidasResp,
            proximosVencimentosResp,
            produtosAtivos,
            distribuicaoCategorias,
            estoqueBaixo,
            clientesInativos);
    }
}
