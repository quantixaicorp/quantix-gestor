using System.Globalization;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Dashboard;
using GestorAI.API.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Dashboard;

public class ModuleDashboardService(AppDbContext db)
{
    public async Task<ModulosDashboardResponse> GetAsync(CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var em30Dias = hoje.AddDays(30);
        var inicioMesDate = DateOnly.FromDateTime(inicioMes);
        var hojeDate = DateOnly.FromDateTime(hoje);
        var em30DiasDate = DateOnly.FromDateTime(em30Dias);

        var agendamentos = await GetAgendamentosAsync(hoje, inicioMes, ct);
        var contratos = await GetContratosAsync(hojeDate, em30DiasDate, ct);
        var cobrancas = await GetCobrancasAsync(hojeDate, ct);
        var orcamentos = await GetOrcamentosAsync(inicioMes, ct);
        var assinaturas = await GetAssinaturasAsync(inicioMes, ct);

        return new ModulosDashboardResponse(agendamentos, contratos, cobrancas, orcamentos, assinaturas);
    }

    private async Task<AgendamentosDashResponse> GetAgendamentosAsync(
        DateTime hoje, DateTime inicioMes, CancellationToken ct)
    {
        var agendamentosHoje = await db.Appointments
            .Where(a => a.StartAt.Date == hoje && a.Status != AgendamentoStatus.Cancelado)
            .CountAsync(ct);

        var confirmadosHoje = await db.Appointments
            .Where(a => a.StartAt.Date == hoje
                && (a.Status == AgendamentoStatus.Confirmado || a.Status == AgendamentoStatus.Concluido))
            .CountAsync(ct);

        var canceladosMes = await db.Appointments
            .Where(a => a.StartAt >= inicioMes && a.Status == AgendamentoStatus.Cancelado)
            .CountAsync(ct);

        var totalMes = await db.Appointments
            .Where(a => a.StartAt >= inicioMes && a.Status != AgendamentoStatus.Cancelado)
            .CountAsync(ct);

        var concluidosMes = await db.Appointments
            .Where(a => a.StartAt >= inicioMes && a.Status == AgendamentoStatus.Concluido)
            .CountAsync(ct);

        var taxaConclusao = totalMes > 0 ? Math.Round((decimal)concluidosMes / totalMes * 100m, 1) : 0m;
        var taxaOcupacao = Math.Round((concluidosMes / (decimal)Math.Max(totalMes, 1)) * 100m, 1);

        // PorStatus — agrupar agendamentos de hoje por Status
        var agendamentosHojeList = await db.Appointments
            .Where(a => a.StartAt.Date == hoje)
            .ToListAsync(ct);

        var porStatus = agendamentosHojeList
            .GroupBy(a => a.Status.ToString())
            .Select(g => new AgendamentoStatusItem(g.Key, g.Count()))
            .ToList();

        // PorProfissional — agendamentos do mês não cancelados com profissional
        var agendamentosMesList = await db.Appointments
            .Where(a => a.StartAt >= inicioMes && a.Status != AgendamentoStatus.Cancelado)
            .Include(a => a.Professional)
            .ToListAsync(ct);

        var porProfissional = agendamentosMesList
            .GroupBy(a => a.Professional?.Name ?? "Sem profissional")
            .Select(g => new AgendamentoProfissionalItem(
                g.Key,
                g.Count(),
                g.Count(a => a.Status == AgendamentoStatus.Concluido)))
            .ToList();

        // AgendaHoje — agendamentos de hoje não cancelados ordenados por StartAt
        var agendaHoje = await db.Appointments
            .Where(a => a.StartAt.Date == hoje && a.Status != AgendamentoStatus.Cancelado)
            .Include(a => a.Service)
            .OrderBy(a => a.StartAt)
            .ToListAsync(ct);

        var agendaHojeResp = agendaHoje
            .Select(a => new AgendamentoDoDiaItem(
                a.CustomerName,
                a.Service?.Name ?? "",
                a.StartAt.ToString("HH:mm"),
                a.Status.ToString()))
            .ToList();

        return new AgendamentosDashResponse(
            agendamentosHoje, confirmadosHoje, canceladosMes, taxaConclusao,
            taxaOcupacao, porStatus, porProfissional, agendaHojeResp);
    }

    private async Task<ContratosDashResponse> GetContratosAsync(
        DateOnly hoje, DateOnly em30Dias, CancellationToken ct)
    {
        var ativos = await db.Contracts
            .Where(c => c.Status == ContratoStatus.Ativo)
            .Include(c => c.Customer)
            .ToListAsync(ct);

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

        var contratosVencendo = ativos
            .Where(c => c.EndDate.HasValue && c.EndDate.Value >= hoje && c.EndDate.Value <= em30Dias)
            .Select(c => new ContratoVencendoItem(
                c.Title,
                c.Customer?.Name ?? "",
                c.Amount,
                c.EndDate!.Value,
                (c.EndDate!.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).Days))
            .ToList();

        return new ContratosDashResponse(ativos.Count, Math.Round(mrr, 2), vencendoEm30, contratosVencendo);
    }

    private async Task<CobrancasDashResponse> GetCobrancasAsync(DateOnly hoje, CancellationToken ct)
    {
        var pendentes = await db.Charges
            .Where(c => c.Status == CobrancaStatus.Pendente)
            .Include(c => c.Customer)
            .ToListAsync(ct);

        var totalReceber = pendentes.Sum(c => c.Amount);
        var vencidas = pendentes.Where(c => c.DueDate < hoje).ToList();

        var cobrancasVencidas = vencidas
            .Select(c => new CobrancaVencidaItem(
                c.Reference,
                c.Customer?.Name ?? "",
                c.Amount,
                c.DueDate,
                (int)(hoje.ToDateTime(TimeOnly.MinValue) - c.DueDate.ToDateTime(TimeOnly.MinValue)).TotalDays))
            .ToList();

        var aging = new List<AgingFaixa>
        {
            new("1-7 dias",
                cobrancasVencidas.Count(v => v.DiasAtraso >= 1 && v.DiasAtraso <= 7),
                cobrancasVencidas.Where(v => v.DiasAtraso >= 1 && v.DiasAtraso <= 7).Sum(v => v.Amount)),
            new("8-30 dias",
                cobrancasVencidas.Count(v => v.DiasAtraso >= 8 && v.DiasAtraso <= 30),
                cobrancasVencidas.Where(v => v.DiasAtraso >= 8 && v.DiasAtraso <= 30).Sum(v => v.Amount)),
            new("31-60 dias",
                cobrancasVencidas.Count(v => v.DiasAtraso >= 31 && v.DiasAtraso <= 60),
                cobrancasVencidas.Where(v => v.DiasAtraso >= 31 && v.DiasAtraso <= 60).Sum(v => v.Amount)),
            new("+60 dias",
                cobrancasVencidas.Count(v => v.DiasAtraso > 60),
                cobrancasVencidas.Where(v => v.DiasAtraso > 60).Sum(v => v.Amount)),
        };

        return new CobrancasDashResponse(totalReceber, vencidas.Count, vencidas.Sum(c => c.Amount), cobrancasVencidas, aging);
    }

    private async Task<OrcamentosDashResponse> GetOrcamentosAsync(DateTime inicioMes, CancellationToken ct)
    {
        var abertosStatus = new[] { OrcamentoStatus.Enviado, OrcamentoStatus.Aprovado };

        var abertos = await db.Quotes
            .Where(o => abertosStatus.Contains(o.Status))
            .Include(o => o.Items)
            .ToListAsync(ct);

        var valorPipeline = abertos.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity));

        var orcamentosMes = await db.Quotes
            .Where(o => o.CreatedAt >= inicioMes && o.Status != OrcamentoStatus.Rascunho && o.Status != OrcamentoStatus.Cancelado)
            .CountAsync(ct);

        var convertidosMes = await db.Quotes
            .Where(o => o.CreatedAt >= inicioMes && o.Status == OrcamentoStatus.Convertido)
            .CountAsync(ct);

        var taxaConversao = orcamentosMes > 0
            ? Math.Round((decimal)convertidosMes / orcamentosMes * 100m, 1) : 0m;

        var todos = await db.Quotes
            .Where(o => o.Status != OrcamentoStatus.Rascunho)
            .Include(o => o.Items)
            .ToListAsync(ct);

        var porStatus = todos
            .GroupBy(o => o.Status.ToString())
            .Select(g => new OrcamentoStatusItem(
                g.Key,
                g.Count(),
                g.Sum(o => o.Items.Sum(i => i.UnitPrice * i.Quantity))))
            .ToList();

        return new OrcamentosDashResponse(abertos.Count, taxaConversao, Math.Round(valorPipeline, 2), porStatus);
    }

    private async Task<AssinaturasDashResponse> GetAssinaturasAsync(DateTime inicioMes, CancellationToken ct)
    {
        var ativas = await db.CustomerSubscriptions
            .Where(a => a.Status == AssinaturaStatus.Ativa)
            .Include(a => a.Plan)
            .ToListAsync(ct);

        var mrr = ativas.Sum(a => a.Plan is null ? 0m : a.Plan.Frequency switch
        {
            Periodicidade.Mensal => a.Plan.Price,
            Periodicidade.Trimestral => a.Plan.Price / 3m,
            Periodicidade.Semestral => a.Plan.Price / 6m,
            Periodicidade.Anual => a.Plan.Price / 12m,
            _ => 0m,
        });

        var canceladasMes = await db.CustomerSubscriptions
            .Where(a => (a.Status == AssinaturaStatus.Cancelada || a.Status == AssinaturaStatus.Expirada)
                && a.CreatedAt >= inicioMes)
            .CountAsync(ct);

        var novasMes = await db.CustomerSubscriptions
            .Where(a => a.Status == AssinaturaStatus.Ativa && a.CreatedAt >= inicioMes)
            .CountAsync(ct);

        // Evolucao12Meses
        var ptBr = CultureInfo.GetCultureInfo("pt-BR");
        var hoje = DateTime.UtcNow.Date;
        var todasAssinaturas = await db.CustomerSubscriptions.ToListAsync(ct);
        var evolucao = new List<EvolucaoAssinaturaMes>();

        for (int i = 11; i >= 0; i--)
        {
            var inicioM = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(-i);
            var ultimoDiaMes = inicioM.AddMonths(1).AddDays(-1);

            var ativasNoMes = todasAssinaturas.Count(a =>
                a.Status == AssinaturaStatus.Ativa && a.CreatedAt.Date <= ultimoDiaMes);

            var novasNoMes = todasAssinaturas.Count(a =>
                a.Status == AssinaturaStatus.Ativa
                && a.CreatedAt >= inicioM && a.CreatedAt <= ultimoDiaMes);

            var canceladasNoMes = todasAssinaturas.Count(a =>
                (a.Status == AssinaturaStatus.Cancelada || a.Status == AssinaturaStatus.Expirada)
                && a.CreatedAt >= inicioM);

            evolucao.Add(new EvolucaoAssinaturaMes(
                inicioM.ToString("MMM/yy", ptBr),
                ativasNoMes,
                novasNoMes,
                canceladasNoMes));
        }

        return new AssinaturasDashResponse(ativas.Count, Math.Round(mrr, 2), canceladasMes, novasMes, evolucao);
    }
}
