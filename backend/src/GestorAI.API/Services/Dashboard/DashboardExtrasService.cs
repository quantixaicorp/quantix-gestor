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
        var maiorVendaDia = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida && v.SaleDate.Date == hoje)
            .Select(v => (decimal?)v.Total)
            .MaxAsync(ct) ?? 0m;

        // UltimasVendas — últimas 10 vendas Concluidas
        var ultimasVendas = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida)
            .Include(v => v.Customer)
            .OrderByDescending(v => v.SaleDate)
            .Take(10)
            .Select(v => new UltimaVendaResponse(
                v.Id,
                v.SaleDate,
                v.Customer != null ? v.Customer.Name : "Consumidor",
                v.Total,
                v.PaymentMethod.ToString()))
            .ToListAsync(ct);

        // VendasPorFormaPgto — vendas Concluidas do mês corrente agrupadas por FormaPagamento
        var vendasMes = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida && v.SaleDate >= inicioMes)
            .ToListAsync(ct);

        var vendasPorFormaPgto = vendasMes
            .GroupBy(v => v.PaymentMethod.ToString())
            .Select(g => new VendaFormaPgtoResponse(g.Key, g.Count(), g.Sum(v => v.Total)))
            .ToList();

        // ReceitasPorCategoria — Lancamentos Receita Pago com PaymentDate no mês corrente
        var lancamentosReceitaMes = await db.Transactions
            .Where(l => l.Type == TipoLancamento.Receita
                && l.Status == StatusLancamento.Pago
                && l.PaymentDate.HasValue
                && l.PaymentDate!.Value >= inicioMes
                && l.PaymentDate.Value < inicioMes.AddMonths(1))
            .ToListAsync(ct);

        var receitasPorCategoria = lancamentosReceitaMes
            .GroupBy(l => l.Category)
            .Select(g => new ReceitaCategoriaResponse(g.Key, g.Sum(l => l.Amount)))
            .OrderByDescending(r => r.Total)
            .ToList();

        // FluxoAnual — últimos 12 meses
        var fluxoAnual = new List<FluxoMensalResponse>();
        for (int i = 11; i >= 0; i--)
        {
            var inicioM = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(-i);
            var fimM = inicioM.AddMonths(1);
            var lancMes = await db.Transactions
                .Where(l => l.Status == StatusLancamento.Pago
                    && l.PaymentDate.HasValue
                    && l.PaymentDate!.Value >= inicioM
                    && l.PaymentDate.Value < fimM)
                .ToListAsync(ct);

            var receitas = lancMes.Where(l => l.Type == TipoLancamento.Receita).Sum(l => l.Amount);
            var despesas = lancMes.Where(l => l.Type == TipoLancamento.Despesa).Sum(l => l.Amount);
            fluxoAnual.Add(new FluxoMensalResponse(
                inicioM.ToString("MMM/yy", ptBr),
                receitas,
                despesas,
                receitas - despesas));
        }

        // ContasVencidas — Lancamentos Despesa Pendente com DueDate.Date < hoje
        var contasVencidas = await db.Transactions
            .Where(l => l.Type == TipoLancamento.Despesa
                && l.Status == StatusLancamento.Pendente
                && l.DueDate.Date < hoje)
            .ToListAsync(ct);

        var contasVencidasResp = contasVencidas
            .Select(l => new ContaVencidaDetalheResponse(
                l.Id,
                l.Description,
                l.Category,
                l.Amount,
                l.DueDate,
                (int)(hoje - l.DueDate.Date).TotalDays))
            .ToList();

        // ProximosVencimentos — Lancamentos Pendente com DueDate nos próximos 7 dias
        var em7Dias = hoje.AddDays(7);
        var proximosVencimentos = await db.Transactions
            .Where(l => l.Status == StatusLancamento.Pendente
                && l.DueDate.Date >= hoje
                && l.DueDate.Date <= em7Dias)
            .ToListAsync(ct);

        var proximosVencimentosResp = proximosVencimentos
            .Select(l => new ProximoVencimentoResponse(
                l.Id,
                l.Description,
                l.Category,
                l.Amount,
                l.DueDate,
                (int)(l.DueDate.Date - hoje).TotalDays))
            .ToList();

        // ProdutosAtivos
        var produtosAtivos = await db.Products.CountAsync(p => p.IsActive, ct);

        // DistribuicaoCategorias — Produtos Ativos agrupados por Category
        var produtosComCategoria = await db.Products
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .ToListAsync(ct);

        var distribuicaoCategorias = produtosComCategoria
            .GroupBy(p => p.Category?.Name ?? "Sem categoria")
            .Select(g => new EstoqueCategoriaResponse(
                g.Key,
                g.Count(),
                g.Sum(p => p.CurrentStock * p.AverageCost)))
            .ToList();

        // EstoqueBaixo — Produtos com IsActive e CurrentStock <= MinimumStock
        var estoqueBaixo = produtosComCategoria
            .Where(p => p.CurrentStock <= p.MinimumStock)
            .Select(p => new EstoqueBaixoDetalheResponse(
                p.Name,
                p.CurrentStock,
                p.MinimumStock,
                p.SalePrice))
            .ToList();

        // ClientesInativos — sem venda Concluida nos últimos 90 dias
        var idsAtivos = await db.Sales
            .Where(v => v.Status == StatusVenda.Concluida
                && v.CustomerId != null
                && v.SaleDate >= hoje.AddDays(-90))
            .Select(v => v.CustomerId!.Value)
            .Distinct()
            .ToListAsync(ct);

        var clientesInativos = await db.Customers.CountAsync(c => !idsAtivos.Contains(c.Id), ct);

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
