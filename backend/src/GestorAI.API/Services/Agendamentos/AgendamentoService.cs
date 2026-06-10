using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Agendamentos;

public class AgendamentoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<AgendamentoListItem>> ListAsync(DateOnly data, CancellationToken ct)
    {
        var inicio = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = inicio.AddDays(1);

        return await db.Agendamentos
            .Include(a => a.Profissional)
            .Include(a => a.Servico)
            .Where(a => a.DataHoraInicio >= inicio && a.DataHoraInicio < fim)
            .OrderBy(a => a.DataHoraInicio)
            .Select(a => new AgendamentoListItem(
                a.Id,
                a.ProfissionalId,
                a.Profissional!.Nome,
                a.ClienteNome,
                a.Servico!.Nome,
                a.DataHoraInicio,
                a.DataHoraFim,
                a.Status))
            .ToListAsync(ct);
    }

    public async Task<List<AgendamentoListItem>> ListSemanaAsync(
        DateOnly de, DateOnly ate, Guid? profissionalId, CancellationToken ct)
    {
        var inicio = de.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fim = ate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);

        var query = db.Agendamentos
            .Include(a => a.Profissional)
            .Include(a => a.Servico)
            .Where(a => a.DataHoraInicio >= inicio && a.DataHoraInicio < fim);

        if (profissionalId.HasValue)
            query = query.Where(a => a.ProfissionalId == profissionalId.Value);

        return await query
            .OrderBy(a => a.DataHoraInicio)
            .Select(a => new AgendamentoListItem(
                a.Id,
                a.ProfissionalId,
                a.Profissional!.Nome,
                a.ClienteNome,
                a.Servico!.Nome,
                a.DataHoraInicio,
                a.DataHoraFim,
                a.Status))
            .ToListAsync(ct);
    }

    public async Task<AgendamentoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var a = await db.Agendamentos
            .Include(a => a.Profissional)
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Agendamento não encontrado.", 404);
        return ToResponse(a);
    }

    public async Task<AgendamentoResponse> CriarAsync(CriarAgendamentoRequest req, CancellationToken ct)
    {
        _ = await db.Profissionais.FirstOrDefaultAsync(p => p.Id == req.ProfissionalId, ct)
            ?? throw new AppException("Profissional não encontrado.", 404);

        var servico = await db.Produtos
            .FirstOrDefaultAsync(p => p.Id == req.ServicoId && p.DuracaoMinutos != null, ct)
            ?? throw new AppException("Serviço não encontrado ou sem duração configurada.", 404);

        var dataHoraFim = req.DataHoraInicio.AddMinutes(servico.DuracaoMinutos!.Value);

        var diaSemana = (int)req.DataHoraInicio.DayOfWeek;
        var horaInicio = req.DataHoraInicio.TimeOfDay;
        var horaFim = dataHoraFim.TimeOfDay;

        var dataAgendamento = DateOnly.FromDateTime(req.DataHoraInicio);
        var dentroDoHorario = await db.DisponibilidadeSemanais
            .AnyAsync(d => d.ProfissionalId == req.ProfissionalId
                && d.DiaSemana == diaSemana
                && d.DataInicio <= dataAgendamento && d.DataFim >= dataAgendamento
                && d.HoraInicio <= horaInicio
                && d.HoraFim >= horaFim, ct);

        if (!dentroDoHorario)
            throw new AppException("Horário fora da disponibilidade do profissional.", 400);

        var bloqueado = await db.BloqueiosAgenda
            .AnyAsync(b => b.DataInicio < dataHoraFim && b.DataFim > req.DataHoraInicio
                && (b.ProfissionalId == null || b.ProfissionalId == req.ProfissionalId), ct);

        if (bloqueado)
            throw new AppException("Horário bloqueado para o profissional.", 400);

        var conflito = await db.Agendamentos
            .AnyAsync(a => a.ProfissionalId == req.ProfissionalId
                && a.Status != AgendamentoStatus.Cancelado
                && a.DataHoraInicio < dataHoraFim && a.DataHoraFim > req.DataHoraInicio, ct);

        if (conflito)
            throw new AppException("Conflito de horário com outro agendamento.", 400);

        var agendamento = new Agendamento
        {
            EmpresaId = tenantContext.EmpresaId,
            ProfissionalId = req.ProfissionalId,
            ClienteNome = req.ClienteNome,
            ClienteTelefone = req.ClienteTelefone,
            ClienteId = req.ClienteId,
            ServicoId = req.ServicoId,
            DataHoraInicio = req.DataHoraInicio,
            DataHoraFim = dataHoraFim,
            Status = AgendamentoStatus.Agendado,
            Observacao = req.Observacao,
        };

        db.Agendamentos.Add(agendamento);
        await db.SaveChangesAsync(ct);
        return await GetAsync(agendamento.Id, ct);
    }

    public async Task<AgendamentoResponse> AtualizarAsync(Guid id, AtualizarAgendamentoRequest req, CancellationToken ct)
    {
        var a = await db.Agendamentos
            .Include(a => a.Profissional)
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Agendamento não encontrado.", 404);

        if (a.Status == AgendamentoStatus.Concluido || a.Status == AgendamentoStatus.Cancelado)
            throw new AppException("Agendamentos concluídos ou cancelados não podem ser editados.", 400);

        a.Observacao = req.Observacao;
        await db.SaveChangesAsync(ct);
        return ToResponse(a);
    }

    public async Task<AgendamentoResponse> ConfirmarAsync(Guid id, CancellationToken ct)
    {
        var a = await FindAsync(id, ct);
        if (a.Status != AgendamentoStatus.Agendado && a.Status != AgendamentoStatus.AguardandoConfirmacao)
            throw new AppException("Apenas agendamentos nos status Agendado ou AguardandoConfirmacao podem ser confirmados.", 400);
        a.Status = AgendamentoStatus.Confirmado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<ConcluirResponse> ConcluirAsync(Guid id, CancellationToken ct)
    {
        var a = await db.Agendamentos
            .Include(a => a.Servico)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Agendamento não encontrado.", 404);

        if (a.Status != AgendamentoStatus.Agendado && a.Status != AgendamentoStatus.Confirmado)
            throw new AppException("Apenas agendamentos ativos podem ser concluídos.", 400);

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        var preco = a.Servico!.PrecoVenda;

        var venda = new Venda
        {
            EmpresaId = tenantContext.EmpresaId,
            ClienteId = a.ClienteId,
            Status = StatusVenda.Aberta,
            Subtotal = preco,
            Desconto = 0,
            Total = preco,
            FormaPagamento = FormaPagamento.Outro,
            Observacao = $"Gerado do agendamento de {a.ClienteNome}",
        };
        db.Vendas.Add(venda);

        db.ItensVenda.Add(new ItemVenda
        {
            VendaId = venda.Id,
            ProdutoId = a.ServicoId,
            Quantidade = 1,
            PrecoUnitario = preco,
            Desconto = 0,
            Total = preco,
        });

        a.Status = AgendamentoStatus.Concluido;
        a.VendaId = venda.Id;

        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return new ConcluirResponse(venda.Id);
    }

    public async Task<AgendamentoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var a = await FindAsync(id, ct);
        if (a.Status == AgendamentoStatus.Concluido || a.Status == AgendamentoStatus.Cancelado)
            throw new AppException("Agendamento já está concluído ou cancelado.", 400);
        a.Status = AgendamentoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<AgendamentoResponse> CancelarPublicoAsync(Guid id, CancellationToken ct)
    {
        var a = await FindAsync(id, ct);
        if (a.Status == AgendamentoStatus.Cancelado)
            throw new AppException("Agendamento já está cancelado.", 400);
        if (a.Status == AgendamentoStatus.Concluido)
            throw new AppException("Agendamento já concluído não pode ser cancelado.", 400);

        var config = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.EmpresaId == a.EmpresaId, ct);

        if (config?.HorasLimiteCancelamento is int horas)
        {
            var horasAte = (a.DataHoraInicio - DateTime.UtcNow).TotalHours;
            if (horasAte < horas)
                throw new AppException(
                    $"Cancelamentos devem ser feitos com pelo menos {horas} hora(s) de antecedência.", 400);
        }

        a.Status = AgendamentoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<AgendamentoResponse> RecusarAsync(Guid id, CancellationToken ct)
    {
        var a = await FindAsync(id, ct);
        if (a.Status != AgendamentoStatus.AguardandoConfirmacao)
            throw new AppException("Apenas agendamentos AguardandoConfirmacao podem ser recusados.", 400);
        a.Status = AgendamentoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<List<AgendamentoListItem>> PendentesConfirmacaoAsync(CancellationToken ct) =>
        await db.Agendamentos
            .Include(a => a.Profissional)
            .Include(a => a.Servico)
            .Where(a => a.Status == AgendamentoStatus.AguardandoConfirmacao)
            .OrderBy(a => a.DataHoraInicio)
            .Select(a => new AgendamentoListItem(
                a.Id,
                a.ProfissionalId,
                a.Profissional!.Nome,
                a.ClienteNome,
                a.Servico!.Nome,
                a.DataHoraInicio,
                a.DataHoraFim,
                a.Status))
            .ToListAsync(ct);

    public async Task<List<DateTime>> SlotsAsync(
        Guid profissionalId, DateOnly data, Guid servicoId, CancellationToken ct)
    {
        var diaSemana = (int)data.DayOfWeek;

        var faixas = await db.DisponibilidadeSemanais
            .Where(d => d.ProfissionalId == profissionalId
                && d.DiaSemana == diaSemana
                && d.DataInicio <= data && d.DataFim >= data)
            .ToListAsync(ct);

        if (faixas.Count == 0) return [];

        var servico = await db.Produtos
            .FirstOrDefaultAsync(p => p.Id == servicoId && p.DuracaoMinutos != null, ct)
            ?? throw new AppException("Serviço não encontrado.", 404);

        var duracao = TimeSpan.FromMinutes(servico.DuracaoMinutos!.Value);
        var inicioDia = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var fimDia = inicioDia.AddDays(1);

        var bloqueios = await db.BloqueiosAgenda
            .Where(b => b.DataInicio < fimDia && b.DataFim > inicioDia
                && (b.ProfissionalId == null || b.ProfissionalId == profissionalId))
            .ToListAsync(ct);

        var ocupados = await db.Agendamentos
            .Where(a => a.ProfissionalId == profissionalId
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

    private async Task<Agendamento> FindAsync(Guid id, CancellationToken ct) =>
        await db.Agendamentos.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Agendamento não encontrado.", 404);

    private static AgendamentoResponse ToResponse(Agendamento a) => new(
        a.Id,
        a.Profissional?.Nome ?? "",
        a.ClienteNome,
        a.ClienteTelefone,
        a.ClienteId,
        a.Servico?.Nome ?? "",
        a.Servico?.DuracaoMinutos ?? 0,
        a.DataHoraInicio,
        a.DataHoraFim,
        a.Status,
        a.Observacao,
        a.VendaId,
        a.CriadoEm);
}
