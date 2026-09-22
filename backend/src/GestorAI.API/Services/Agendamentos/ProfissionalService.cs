using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Agendamentos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Agendamentos;

public class ProfissionalService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ProfissionalResponse>> ListAsync(CancellationToken ct) =>
        await db.Professionals
            .OrderBy(p => p.Name)
            .Select(p => new ProfissionalResponse(p.Id, p.Name, p.Phone, p.IsActive))
            .ToListAsync(ct);

    public async Task<ProfissionalResponse> CreateAsync(CriarProfissionalRequest req, CancellationToken ct)
    {
        var p = new Professional
        {
            CompanyId = tenantContext.CompanyId,
            Name = req.Name,
            Phone = req.Phone,
        };
        db.Professionals.Add(p);
        await db.SaveChangesAsync(ct);
        return new ProfissionalResponse(p.Id, p.Name, p.Phone, p.IsActive);
    }

    public async Task<ProfissionalResponse> UpdateAsync(Guid id, AtualizarProfissionalRequest req, CancellationToken ct)
    {
        var p = await db.Professionals.FindAsync([id], ct)
            ?? throw new AppException("Professional não encontrado.", 404);
        p.Name = req.Name;
        p.Phone = req.Phone;
        p.IsActive = req.IsActive;
        await db.SaveChangesAsync(ct);
        return new ProfissionalResponse(p.Id, p.Name, p.Phone, p.IsActive);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Professionals.FindAsync([id], ct)
            ?? throw new AppException("Professional não encontrado.", 404);
        var temAgendamentos = await db.Appointments
            .AnyAsync(a => a.ProfessionalId == id && a.Status != Domain.Enums.AgendamentoStatus.Cancelado, ct);
        if (temAgendamentos)
            throw new AppException("Professional possui agendamentos ativos e não pode ser excluído.", 400);
        db.Professionals.Remove(p);
        await db.SaveChangesAsync(ct);
    }

    public async Task<DisponibilidadePeriodoResponse?> GetDisponibilidadeAsync(
        Guid id, DateOnly dataInicio, DateOnly dataFim, CancellationToken ct)
    {
        _ = await db.Professionals.FindAsync([id], ct)
            ?? throw new AppException("Professional não encontrado.", 404);

        var faixas = await db.WeeklyAvailabilities
            .Where(d => d.ProfessionalId == id && d.StartDate == dataInicio && d.EndDate == dataFim)
            .OrderBy(d => d.WeekDay).ThenBy(d => d.StartTime)
            .Select(d => new DisponibilidadeItem(
                d.WeekDay,
                $"{d.StartTime.Hours:D2}:{d.StartTime.Minutes:D2}",
                $"{d.EndTime.Hours:D2}:{d.EndTime.Minutes:D2}"))
            .ToListAsync(ct);

        return new DisponibilidadePeriodoResponse(dataInicio, dataFim, faixas);
    }

    public async Task<List<DisponibilidadePeriodoResponse>> ListPeriodosAsync(Guid id, CancellationToken ct)
    {
        _ = await db.Professionals.FindAsync([id], ct)
            ?? throw new AppException("Professional não encontrado.", 404);

        var all = await db.WeeklyAvailabilities
            .Where(d => d.ProfessionalId == id && d.StartDate > DateOnly.MinValue)
            .OrderBy(d => d.StartDate).ThenBy(d => d.WeekDay).ThenBy(d => d.StartTime)
            .ToListAsync(ct);

        return all
            .GroupBy(d => (d.StartDate, d.EndDate))
            .Select(g => new DisponibilidadePeriodoResponse(
                g.Key.StartDate,
                g.Key.EndDate,
                g.Select(d => new DisponibilidadeItem(
                    d.WeekDay,
                    $"{d.StartTime.Hours:D2}:{d.StartTime.Minutes:D2}",
                    $"{d.EndTime.Hours:D2}:{d.EndTime.Minutes:D2}"))
                 .ToList()))
            .ToList();
    }

    public async Task SalvarDisponibilidadeAsync(Guid id, SalvarDisponibilidadeRequest req, CancellationToken ct)
    {
        _ = await db.Professionals.FindAsync([id], ct)
            ?? throw new AppException("Professional não encontrado.", 404);

        if (req.EndDate < req.StartDate)
            throw new AppException("EndDate deve ser igual ou posterior a StartDate.");

        // Remove apenas as faixas do período exato sendo salvo
        var existentes = await db.WeeklyAvailabilities
            .Where(d => d.ProfessionalId == id && d.StartDate == req.StartDate && d.EndDate == req.EndDate)
            .ToListAsync(ct);
        db.WeeklyAvailabilities.RemoveRange(existentes);

        foreach (var faixa in req.Faixas)
        {
            if (!TimeSpan.TryParseExact(faixa.StartTime, @"hh\:mm", null, out var inicio))
                throw new AppException($"StartTime inválida: {faixa.StartTime}");
            if (!TimeSpan.TryParseExact(faixa.EndTime, @"hh\:mm", null, out var fim))
                throw new AppException($"EndTime inválida: {faixa.EndTime}");
            if (fim <= inicio)
                throw new AppException("EndTime deve ser posterior a StartTime.");

            db.WeeklyAvailabilities.Add(new WeeklyAvailability
            {
                ProfessionalId = id,
                WeekDay = faixa.WeekDay,
                StartTime = inicio,
                EndTime = fim,
                StartDate = req.StartDate,
                EndDate = req.EndDate,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task ExcluirPeriodoAsync(Guid id, DateOnly dataInicio, DateOnly dataFim, CancellationToken ct)
    {
        var existentes = await db.WeeklyAvailabilities
            .Where(d => d.ProfessionalId == id && d.StartDate == dataInicio && d.EndDate == dataFim)
            .ToListAsync(ct);
        db.WeeklyAvailabilities.RemoveRange(existentes);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<BloqueioResponse>> ListBloqueiosAsync(DateTime de, DateTime ate, CancellationToken ct) =>
        await db.ScheduleBlocks
            .Include(b => b.Professional)
            .Where(b => b.StartDate < ate && b.EndDate > de)
            .OrderBy(b => b.StartDate)
            .Select(b => new BloqueioResponse(
                b.Id, b.ProfessionalId, b.Professional != null ? b.Professional.Name : null,
                b.StartDate, b.EndDate, b.Reason))
            .ToListAsync(ct);

    public async Task<BloqueioResponse> CriarBloqueioAsync(CriarBloqueioRequest req, CancellationToken ct)
    {
        if (req.EndDate <= req.StartDate)
            throw new AppException("EndDate deve ser posterior a StartDate.");

        var b = new ScheduleBlock
        {
            CompanyId = tenantContext.CompanyId,
            ProfessionalId = req.ProfessionalId,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            Reason = req.Reason,
        };
        db.ScheduleBlocks.Add(b);
        await db.SaveChangesAsync(ct);

        string? nomeProfissional = null;
        if (req.ProfessionalId.HasValue)
        {
            var prof = await db.Professionals.FindAsync([req.ProfessionalId.Value], ct);
            nomeProfissional = prof?.Name;
        }
        return new BloqueioResponse(b.Id, b.ProfessionalId, nomeProfissional, b.StartDate, b.EndDate, b.Reason);
    }

    public async Task DeleteBloqueioAsync(Guid id, CancellationToken ct)
    {
        var b = await db.ScheduleBlocks.FindAsync([id], ct)
            ?? throw new AppException("Bloqueio não encontrado.", 404);
        db.ScheduleBlocks.Remove(b);
        await db.SaveChangesAsync(ct);
    }
}
