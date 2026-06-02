using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.PublicBooking;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.PublicBooking;

public class PublicBookingService(AppDbContext db)
{
    public async Task<Guid> ResolveEmpresaAsync(string slug, CancellationToken ct)
    {
        var config = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Slug == slug, ct)
            ?? throw new AppException("Empresa não encontrada.", 404);
        return config.EmpresaId;
    }

    public async Task<PublicEmpresaInfo> GetInfoAsync(Guid empresaId, CancellationToken ct)
    {
        var config = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId, ct)
            ?? throw new AppException("Empresa não encontrada.", 404);
        return new PublicEmpresaInfo(
            config.NomeFantasia ?? config.RazaoSocial ?? "Empresa",
            config.LogoUrl,
            config.CorPrimaria,
            config.DescricaoPublica);
    }

    public async Task<List<PublicServicoResponse>> GetServicosAsync(Guid empresaId, CancellationToken ct) =>
        await db.Produtos
            .IgnoreQueryFilters()
            .Where(p => p.EmpresaId == empresaId && p.Tipo == TipoProduto.Servico && p.Ativo && p.DuracaoMinutos != null)
            .OrderBy(p => p.Nome)
            .Select(p => new PublicServicoResponse(p.Id, p.Nome, p.PrecoVenda, p.DuracaoMinutos))
            .ToListAsync(ct);

    public async Task<List<PublicProfissionalResponse>> GetProfissionaisAsync(Guid empresaId, CancellationToken ct) =>
        await db.Profissionais
            .IgnoreQueryFilters()
            .Where(p => p.EmpresaId == empresaId && p.Ativo)
            .OrderBy(p => p.Nome)
            .Select(p => new PublicProfissionalResponse(p.Id, p.Nome))
            .ToListAsync(ct);

    public async Task<List<int>> GetDiasComDisponibilidadeAsync(Guid empresaId, Guid profissionalId, CancellationToken ct)
    {
        var profissional = await db.Profissionais
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == profissionalId && p.EmpresaId == empresaId, ct)
            ?? throw new AppException("Profissional não encontrado.", 404);

        return await db.DisponibilidadeSemanais
            .IgnoreQueryFilters()
            .Where(d => d.ProfissionalId == profissionalId)
            .Select(d => d.DiaSemana)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<List<DateTime>> GetSlotsAsync(
        Guid empresaId, Guid profissionalId, Guid servicoId, DateOnly data, CancellationToken ct)
    {
        var diaSemana = (int)data.DayOfWeek;

        var faixas = await db.DisponibilidadeSemanais
            .IgnoreQueryFilters()
            .Where(d => d.ProfissionalId == profissionalId && d.DiaSemana == diaSemana)
            .ToListAsync(ct);

        if (faixas.Count == 0) return [];

        var servico = await db.Produtos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == servicoId && p.EmpresaId == empresaId && p.DuracaoMinutos != null, ct)
            ?? throw new AppException("Serviço não encontrado.", 404);

        var duracao = TimeSpan.FromMinutes(servico.DuracaoMinutos!.Value);
        var inicioDia = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimDia = inicioDia.AddDays(1);

        var bloqueios = await db.BloqueiosAgenda
            .IgnoreQueryFilters()
            .Where(b => b.EmpresaId == empresaId
                && b.DataInicio < fimDia && b.DataFim > inicioDia
                && (b.ProfissionalId == null || b.ProfissionalId == profissionalId))
            .ToListAsync(ct);

        var ocupados = await db.Agendamentos
            .IgnoreQueryFilters()
            .Where(a => a.EmpresaId == empresaId
                && a.ProfissionalId == profissionalId
                && a.DataHoraInicio >= inicioDia && a.DataHoraInicio < fimDia
                && a.Status != AgendamentoStatus.Cancelado)
            .ToListAsync(ct);

        var slots = new List<DateTime>();
        var incremento = TimeSpan.FromMinutes(30);

        foreach (var faixa in faixas)
        {
            var cursor = data.ToDateTime(TimeOnly.FromTimeSpan(faixa.HoraInicio), DateTimeKind.Utc);
            var limite = data.ToDateTime(TimeOnly.FromTimeSpan(faixa.HoraFim), DateTimeKind.Utc) - duracao;

            while (cursor <= limite)
            {
                var fim = cursor + duracao;
                var bloqueado = bloqueios.Any(b => b.DataInicio < fim && b.DataFim > cursor);
                var ocupado = ocupados.Any(a => a.DataHoraInicio < fim && a.DataHoraFim > cursor);

                if (!bloqueado && !ocupado)
                    slots.Add(cursor);

                cursor += incremento;
            }
        }

        return [.. slots.OrderBy(s => s)];
    }

    public async Task<PublicAgendamentoConfirmado> CriarAgendamentoAsync(
        Guid empresaId, PublicCriarAgendamentoRequest req, CancellationToken ct)
    {
        _ = await db.Profissionais
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == req.ProfissionalId && p.EmpresaId == empresaId, ct)
            ?? throw new AppException("Profissional não encontrado.", 404);

        var servico = await db.Produtos
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == req.ServicoId && p.EmpresaId == empresaId && p.DuracaoMinutos != null, ct)
            ?? throw new AppException("Serviço não encontrado.", 404);

        var dataHoraFim = req.DataHoraInicio.AddMinutes(servico.DuracaoMinutos!.Value);

        var conflito = await db.Agendamentos
            .IgnoreQueryFilters()
            .AnyAsync(a => a.EmpresaId == empresaId
                && a.ProfissionalId == req.ProfissionalId
                && a.Status != AgendamentoStatus.Cancelado
                && a.DataHoraInicio < dataHoraFim && a.DataHoraFim > req.DataHoraInicio, ct);

        if (conflito)
            throw new AppException("Horário não está mais disponível.", 409);

        var agendamento = new Agendamento
        {
            EmpresaId = empresaId,
            ProfissionalId = req.ProfissionalId,
            ClienteNome = req.ClienteNome,
            ClienteTelefone = req.ClienteTelefone,
            ServicoId = req.ServicoId,
            DataHoraInicio = req.DataHoraInicio,
            DataHoraFim = dataHoraFim,
            Status = AgendamentoStatus.AguardandoConfirmacao,
        };

        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync(ct);

        return new PublicAgendamentoConfirmado(
            agendamento.Id,
            servico.Nome,
            (await db.Profissionais.IgnoreQueryFilters()
                .FirstAsync(p => p.Id == req.ProfissionalId, ct)).Nome,
            agendamento.DataHoraInicio,
            agendamento.DataHoraFim);
    }
}
