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
        var inicio = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var fim = inicio.AddDays(1);

        return await db.Appointments
            .Include(a => a.Professional)
            .Include(a => a.Service)
            .Where(a => a.StartAt >= inicio && a.StartAt < fim)
            .OrderBy(a => a.StartAt)
            .Select(a => new AgendamentoListItem(
                a.Id,
                a.ProfessionalId,
                a.Professional!.Name,
                a.CustomerName,
                a.Service!.Name,
                a.StartAt,
                a.EndAt,
                a.Status))
            .ToListAsync(ct);
    }

    public async Task<List<AgendamentoListItem>> ListSemanaAsync(
        DateOnly de, DateOnly ate, Guid? profissionalId, CancellationToken ct)
    {
        var inicio = de.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var fim = ate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified).AddDays(1);

        var query = db.Appointments
            .Include(a => a.Professional)
            .Include(a => a.Service)
            .Where(a => a.StartAt >= inicio && a.StartAt < fim);

        if (profissionalId.HasValue)
            query = query.Where(a => a.ProfessionalId == profissionalId.Value);

        return await query
            .OrderBy(a => a.StartAt)
            .Select(a => new AgendamentoListItem(
                a.Id,
                a.ProfessionalId,
                a.Professional!.Name,
                a.CustomerName,
                a.Service!.Name,
                a.StartAt,
                a.EndAt,
                a.Status))
            .ToListAsync(ct);
    }

    public async Task<AgendamentoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var a = await db.Appointments
            .Include(a => a.Professional)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Appointment não encontrado.", 404);
        return ToResponse(a);
    }

    public async Task<AgendamentoResponse> CriarAsync(CriarAgendamentoRequest req, CancellationToken ct)
    {
        _ = await db.Professionals.FirstOrDefaultAsync(p => p.Id == req.ProfessionalId, ct)
            ?? throw new AppException("Professional não encontrado.", 404);

        var servico = await db.Products
            .FirstOrDefaultAsync(p => p.Id == req.ServiceId && p.DurationMinutes != null, ct)
            ?? throw new AppException("Serviço não encontrado ou sem duração configurada.", 404);

        var dataHoraFim = req.StartAt.AddMinutes(servico.DurationMinutes!.Value);

        var diaSemana = (int)req.StartAt.DayOfWeek;
        var horaInicio = req.StartAt.TimeOfDay;
        var horaFim = dataHoraFim.TimeOfDay;

        var dataAgendamento = DateOnly.FromDateTime(req.StartAt);
        var dentroDoHorario = await db.WeeklyAvailabilities
            .AnyAsync(d => d.ProfessionalId == req.ProfessionalId
                && d.WeekDay == diaSemana
                && d.StartDate <= dataAgendamento && d.EndDate >= dataAgendamento
                && d.StartTime <= horaInicio
                && d.EndTime >= horaFim, ct);

        if (!dentroDoHorario)
            throw new AppException("Horário fora da disponibilidade do profissional.", 400);

        var bloqueado = await db.ScheduleBlocks
            .AnyAsync(b => b.StartDate < dataHoraFim && b.EndDate > req.StartAt
                && (b.ProfessionalId == null || b.ProfessionalId == req.ProfessionalId), ct);

        if (bloqueado)
            throw new AppException("Horário bloqueado para o profissional.", 400);

        var conflito = await db.Appointments
            .AnyAsync(a => a.ProfessionalId == req.ProfessionalId
                && a.Status != AgendamentoStatus.Cancelado
                && a.StartAt < dataHoraFim && a.EndAt > req.StartAt, ct);

        if (conflito)
            throw new AppException("Conflito de horário com outro agendamento.", 400);

        var agendamento = new Appointment
        {
            CompanyId = tenantContext.CompanyId,
            ProfessionalId = req.ProfessionalId,
            CustomerName = req.CustomerName,
            CustomerPhone = req.CustomerPhone,
            CustomerId = req.CustomerId,
            ServiceId = req.ServiceId,
            StartAt = req.StartAt,
            EndAt = dataHoraFim,
            Status = AgendamentoStatus.Agendado,
            Notes = req.Notes,
        };

        db.Appointments.Add(agendamento);
        await db.SaveChangesAsync(ct);
        return await GetAsync(agendamento.Id, ct);
    }

    public async Task<AgendamentoResponse> AtualizarAsync(Guid id, AtualizarAgendamentoRequest req, CancellationToken ct)
    {
        var a = await db.Appointments
            .Include(a => a.Professional)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Appointment não encontrado.", 404);

        if (a.Status == AgendamentoStatus.Concluido || a.Status == AgendamentoStatus.Cancelado)
            throw new AppException("Appointments concluídos ou cancelados não podem ser editados.", 400);

        a.Notes = req.Notes;
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
        var a = await db.Appointments
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Appointment não encontrado.", 404);

        if (a.Status != AgendamentoStatus.Agendado && a.Status != AgendamentoStatus.Confirmado)
            throw new AppException("Apenas agendamentos ativos podem ser concluídos.", 400);

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        var preco = a.Service!.SalePrice;

        var profissional = await db.Professionals.FindAsync([a.ProfessionalId], ct);

        var venda = new Sale
        {
            CompanyId = tenantContext.CompanyId,
            CustomerId = a.CustomerId,
            Status = StatusVenda.Aberta,
            Subtotal = preco,
            Discount = 0,
            Total = preco,
            PaymentMethod = FormaPagamento.Outro,
            Notes = $"Gerado do agendamento de {a.CustomerName}",
            ProfessionalId   = a.ProfessionalId,
            ProfessionalName = profissional?.Name,
            ServiceOrderNotes     = $"Appointment de {a.CustomerName}",
        };
        db.Sales.Add(venda);

        db.SaleItems.Add(new SaleItem
        {
            SaleId = venda.Id,
            ProductId = a.ServiceId,
            Quantity = 1,
            UnitPrice = preco,
            Discount = 0,
            Total = preco,
        });

        a.Status = AgendamentoStatus.Concluido;
        a.SaleId = venda.Id;

        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return new ConcluirResponse(venda.Id);
    }

    public async Task<AgendamentoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var a = await FindAsync(id, ct);
        if (a.Status == AgendamentoStatus.Concluido || a.Status == AgendamentoStatus.Cancelado)
            throw new AppException("Appointment já está concluído ou cancelado.", 400);
        a.Status = AgendamentoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<AgendamentoResponse> CancelarPublicoAsync(Guid id, CancellationToken ct)
    {
        var a = await FindAsync(id, ct);
        if (a.Status == AgendamentoStatus.Cancelado)
            throw new AppException("Appointment já está cancelado.", 400);
        if (a.Status == AgendamentoStatus.Concluido)
            throw new AppException("Appointment já concluído não pode ser cancelado.", 400);

        var config = await db.CompanySettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CompanyId == a.CompanyId, ct);

        if (config?.CancellationLimitHours is int horas)
        {
            var horasAte = (a.StartAt - DateTime.UtcNow).TotalHours;
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
        await db.Appointments
            .Include(a => a.Professional)
            .Include(a => a.Service)
            .Where(a => a.Status == AgendamentoStatus.AguardandoConfirmacao)
            .OrderBy(a => a.StartAt)
            .Select(a => new AgendamentoListItem(
                a.Id,
                a.ProfessionalId,
                a.Professional!.Name,
                a.CustomerName,
                a.Service!.Name,
                a.StartAt,
                a.EndAt,
                a.Status))
            .ToListAsync(ct);

    public async Task<List<DateTime>> SlotsAsync(
        Guid profissionalId, DateOnly data, Guid servicoId, CancellationToken ct)
    {
        var diaSemana = (int)data.DayOfWeek;

        var faixas = await db.WeeklyAvailabilities
            .Where(d => d.ProfessionalId == profissionalId
                && d.WeekDay == diaSemana
                && d.StartDate <= data && d.EndDate >= data)
            .ToListAsync(ct);

        if (faixas.Count == 0) return [];

        var servico = await db.Products
            .FirstOrDefaultAsync(p => p.Id == servicoId && p.DurationMinutes != null, ct)
            ?? throw new AppException("Serviço não encontrado.", 404);

        var duracao = TimeSpan.FromMinutes(servico.DurationMinutes!.Value);
        var inicioDia = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var fimDia = inicioDia.AddDays(1);

        var bloqueios = await db.ScheduleBlocks
            .Where(b => b.StartDate < fimDia && b.EndDate > inicioDia
                && (b.ProfessionalId == null || b.ProfessionalId == profissionalId))
            .ToListAsync(ct);

        var ocupados = await db.Appointments
            .Where(a => a.ProfessionalId == profissionalId
                && a.StartAt >= inicioDia && a.StartAt < fimDia
                && a.Status != AgendamentoStatus.Cancelado)
            .ToListAsync(ct);

        var slots = new List<DateTime>();
        var incremento = TimeSpan.FromMinutes(30);

        foreach (var faixa in faixas)
        {
            var cursor = data.ToDateTime(TimeOnly.FromTimeSpan(faixa.StartTime), DateTimeKind.Unspecified);
            var limite = data.ToDateTime(TimeOnly.FromTimeSpan(faixa.EndTime), DateTimeKind.Unspecified) - duracao;

            while (cursor <= limite)
            {
                var fim = cursor + duracao;
                var bloqueado = bloqueios.Any(b => b.StartDate < fim && b.EndDate > cursor);
                var ocupado = ocupados.Any(a => a.StartAt < fim && a.EndAt > cursor);

                if (!bloqueado && !ocupado)
                    slots.Add(cursor);

                cursor += incremento;
            }
        }

        return [.. slots.OrderBy(s => s)];
    }

    private async Task<Appointment> FindAsync(Guid id, CancellationToken ct) =>
        await db.Appointments.FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Appointment não encontrado.", 404);

    private static AgendamentoResponse ToResponse(Appointment a) => new(
        a.Id,
        a.Professional?.Name ?? "",
        a.CustomerName,
        a.CustomerPhone,
        a.CustomerId,
        a.Service?.Name ?? "",
        a.Service?.DurationMinutes ?? 0,
        a.StartAt,
        a.EndAt,
        a.Status,
        a.Notes,
        a.SaleId,
        a.CreatedAt);
}
