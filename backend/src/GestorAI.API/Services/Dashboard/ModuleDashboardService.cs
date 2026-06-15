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
        var agendamentosHoje = await db.Agendamentos
            .Where(a => a.DataHoraInicio.Date == hoje && a.Status != AgendamentoStatus.Cancelado)
            .CountAsync(ct);

        var confirmadosHoje = await db.Agendamentos
            .Where(a => a.DataHoraInicio.Date == hoje
                && (a.Status == AgendamentoStatus.Confirmado || a.Status == AgendamentoStatus.Concluido))
            .CountAsync(ct);

        var canceladosMes = await db.Agendamentos
            .Where(a => a.DataHoraInicio >= inicioMes && a.Status == AgendamentoStatus.Cancelado)
            .CountAsync(ct);

        var totalMes = await db.Agendamentos
            .Where(a => a.DataHoraInicio >= inicioMes && a.Status != AgendamentoStatus.Cancelado)
            .CountAsync(ct);

        var concluidosMes = await db.Agendamentos
            .Where(a => a.DataHoraInicio >= inicioMes && a.Status == AgendamentoStatus.Concluido)
            .CountAsync(ct);

        var taxaConclusao = totalMes > 0 ? Math.Round((decimal)concluidosMes / totalMes * 100m, 1) : 0m;
        var taxaOcupacao = Math.Round((concluidosMes / (decimal)Math.Max(totalMes, 1)) * 100m, 1);

        // PorStatus — agrupar agendamentos de hoje por Status
        var agendamentosHojeList = await db.Agendamentos
            .Where(a => a.DataHoraInicio.Date == hoje)
            .ToListAsync(ct);

        var porStatus = agendamentosHojeList
            .GroupBy(a => a.Status.ToString())
            .Select(g => new AgendamentoStatusItem(g.Key, g.Count()))
            .ToList();

        // PorProfissional — agendamentos do mês não cancelados com profissional
        var agendamentosMesList = await db.Agendamentos
            .Where(a => a.DataHoraInicio >= inicioMes && a.Status != AgendamentoStatus.Cancelado)
            .Include(a => a.Profissional)
            .ToListAsync(ct);

        var porProfissional = agendamentosMesList
            .GroupBy(a => a.Profissional?.Nome ?? "Sem profissional")
            .Select(g => new AgendamentoProfissionalItem(
                g.Key,
                g.Count(),
                g.Count(a => a.Status == AgendamentoStatus.Concluido)))
            .ToList();

        // AgendaHoje — agendamentos de hoje não cancelados ordenados por DataHoraInicio
        var agendaHoje = await db.Agendamentos
            .Where(a => a.DataHoraInicio.Date == hoje && a.Status != AgendamentoStatus.Cancelado)
            .Include(a => a.Servico)
            .OrderBy(a => a.DataHoraInicio)
            .ToListAsync(ct);

        var agendaHojeResp = agendaHoje
            .Select(a => new AgendamentoDoDiaItem(
                a.ClienteNome,
                a.Servico?.Nome ?? "",
                a.DataHoraInicio.ToString("HH:mm"),
                a.Status.ToString()))
            .ToList();

        return new AgendamentosDashResponse(
            agendamentosHoje, confirmadosHoje, canceladosMes, taxaConclusao,
            taxaOcupacao, porStatus, porProfissional, agendaHojeResp);
    }

    private async Task<ContratosDashResponse> GetContratosAsync(
        DateOnly hoje, DateOnly em30Dias, CancellationToken ct)
    {
        var ativos = await db.Contratos
            .Where(c => c.Status == ContratoStatus.Ativo)
            .Include(c => c.Cliente)
            .ToListAsync(ct);

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

        var contratosVencendo = ativos
            .Where(c => c.DataFim.HasValue && c.DataFim.Value >= hoje && c.DataFim.Value <= em30Dias)
            .Select(c => new ContratoVencendoItem(
                c.Titulo,
                c.Cliente?.Nome ?? "",
                c.Valor,
                c.DataFim!.Value,
                (c.DataFim!.Value.ToDateTime(TimeOnly.MinValue) - DateTime.UtcNow).Days))
            .ToList();

        return new ContratosDashResponse(ativos.Count, Math.Round(mrr, 2), vencendoEm30, contratosVencendo);
    }

    private async Task<CobrancasDashResponse> GetCobrancasAsync(DateOnly hoje, CancellationToken ct)
    {
        var pendentes = await db.Cobrancas
            .Where(c => c.Status == CobrancaStatus.Pendente)
            .Include(c => c.Cliente)
            .ToListAsync(ct);

        var totalReceber = pendentes.Sum(c => c.Valor);
        var vencidas = pendentes.Where(c => c.DataVencimento < hoje).ToList();

        var cobrancasVencidas = vencidas
            .Select(c => new CobrancaVencidaItem(
                c.Referencia,
                c.Cliente?.Nome ?? "",
                c.Valor,
                c.DataVencimento,
                (int)(hoje.ToDateTime(TimeOnly.MinValue) - c.DataVencimento.ToDateTime(TimeOnly.MinValue)).TotalDays))
            .ToList();

        var aging = new List<AgingFaixa>
        {
            new("1-7 dias",
                cobrancasVencidas.Count(v => v.DiasAtraso >= 1 && v.DiasAtraso <= 7),
                cobrancasVencidas.Where(v => v.DiasAtraso >= 1 && v.DiasAtraso <= 7).Sum(v => v.Valor)),
            new("8-30 dias",
                cobrancasVencidas.Count(v => v.DiasAtraso >= 8 && v.DiasAtraso <= 30),
                cobrancasVencidas.Where(v => v.DiasAtraso >= 8 && v.DiasAtraso <= 30).Sum(v => v.Valor)),
            new("31-60 dias",
                cobrancasVencidas.Count(v => v.DiasAtraso >= 31 && v.DiasAtraso <= 60),
                cobrancasVencidas.Where(v => v.DiasAtraso >= 31 && v.DiasAtraso <= 60).Sum(v => v.Valor)),
            new("+60 dias",
                cobrancasVencidas.Count(v => v.DiasAtraso > 60),
                cobrancasVencidas.Where(v => v.DiasAtraso > 60).Sum(v => v.Valor)),
        };

        return new CobrancasDashResponse(totalReceber, vencidas.Count, vencidas.Sum(c => c.Valor), cobrancasVencidas, aging);
    }

    private async Task<OrcamentosDashResponse> GetOrcamentosAsync(DateTime inicioMes, CancellationToken ct)
    {
        var abertosStatus = new[] { OrcamentoStatus.Enviado, OrcamentoStatus.Aprovado };

        var abertos = await db.Orcamentos
            .Where(o => abertosStatus.Contains(o.Status))
            .Include(o => o.Itens)
            .ToListAsync(ct);

        var valorPipeline = abertos.Sum(o => o.Itens.Sum(i => i.ValorUnitario * i.Quantidade));

        var orcamentosMes = await db.Orcamentos
            .Where(o => o.CriadoEm >= inicioMes && o.Status != OrcamentoStatus.Rascunho && o.Status != OrcamentoStatus.Cancelado)
            .CountAsync(ct);

        var convertidosMes = await db.Orcamentos
            .Where(o => o.CriadoEm >= inicioMes && o.Status == OrcamentoStatus.Convertido)
            .CountAsync(ct);

        var taxaConversao = orcamentosMes > 0
            ? Math.Round((decimal)convertidosMes / orcamentosMes * 100m, 1) : 0m;

        var todos = await db.Orcamentos
            .Where(o => o.Status != OrcamentoStatus.Rascunho)
            .Include(o => o.Itens)
            .ToListAsync(ct);

        var porStatus = todos
            .GroupBy(o => o.Status.ToString())
            .Select(g => new OrcamentoStatusItem(
                g.Key,
                g.Count(),
                g.Sum(o => o.Itens.Sum(i => i.ValorUnitario * i.Quantidade))))
            .ToList();

        return new OrcamentosDashResponse(abertos.Count, taxaConversao, Math.Round(valorPipeline, 2), porStatus);
    }

    private async Task<AssinaturasDashResponse> GetAssinaturasAsync(DateTime inicioMes, CancellationToken ct)
    {
        var ativas = await db.AssinaturasCliente
            .Where(a => a.Status == AssinaturaStatus.Ativa)
            .Include(a => a.Plano)
            .ToListAsync(ct);

        var mrr = ativas.Sum(a => a.Plano is null ? 0m : a.Plano.Periodicidade switch
        {
            Periodicidade.Mensal => a.Plano.Preco,
            Periodicidade.Trimestral => a.Plano.Preco / 3m,
            Periodicidade.Semestral => a.Plano.Preco / 6m,
            Periodicidade.Anual => a.Plano.Preco / 12m,
            _ => 0m,
        });

        var canceladasMes = await db.AssinaturasCliente
            .Where(a => (a.Status == AssinaturaStatus.Cancelada || a.Status == AssinaturaStatus.Expirada)
                && a.CriadoEm >= inicioMes)
            .CountAsync(ct);

        var novasMes = await db.AssinaturasCliente
            .Where(a => a.Status == AssinaturaStatus.Ativa && a.CriadoEm >= inicioMes)
            .CountAsync(ct);

        // Evolucao12Meses
        var ptBr = CultureInfo.GetCultureInfo("pt-BR");
        var hoje = DateTime.UtcNow.Date;
        var todasAssinaturas = await db.AssinaturasCliente.ToListAsync(ct);
        var evolucao = new List<EvolucaoAssinaturaMes>();

        for (int i = 11; i >= 0; i--)
        {
            var inicioM = new DateTime(hoje.Year, hoje.Month, 1).AddMonths(-i);
            var ultimoDiaMes = inicioM.AddMonths(1).AddDays(-1);

            var ativasNoMes = todasAssinaturas.Count(a =>
                a.Status == AssinaturaStatus.Ativa && a.CriadoEm.Date <= ultimoDiaMes);

            var novasNoMes = todasAssinaturas.Count(a =>
                a.Status == AssinaturaStatus.Ativa
                && a.CriadoEm >= inicioM && a.CriadoEm <= ultimoDiaMes);

            var canceladasNoMes = todasAssinaturas.Count(a =>
                (a.Status == AssinaturaStatus.Cancelada || a.Status == AssinaturaStatus.Expirada)
                && a.CriadoEm >= inicioM);

            evolucao.Add(new EvolucaoAssinaturaMes(
                inicioM.ToString("MMM/yy", ptBr),
                ativasNoMes,
                novasNoMes,
                canceladasNoMes));
        }

        return new AssinaturasDashResponse(ativas.Count, Math.Round(mrr, 2), canceladasMes, novasMes, evolucao);
    }
}
