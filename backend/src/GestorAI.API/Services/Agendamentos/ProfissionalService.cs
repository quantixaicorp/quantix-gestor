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
        await db.Profissionais
            .OrderBy(p => p.Nome)
            .Select(p => new ProfissionalResponse(p.Id, p.Nome, p.Telefone, p.Ativo))
            .ToListAsync(ct);

    public async Task<ProfissionalResponse> CreateAsync(CriarProfissionalRequest req, CancellationToken ct)
    {
        var p = new Profissional
        {
            EmpresaId = tenantContext.EmpresaId,
            Nome = req.Nome,
            Telefone = req.Telefone,
        };
        db.Profissionais.Add(p);
        await db.SaveChangesAsync(ct);
        return new ProfissionalResponse(p.Id, p.Nome, p.Telefone, p.Ativo);
    }

    public async Task<ProfissionalResponse> UpdateAsync(Guid id, AtualizarProfissionalRequest req, CancellationToken ct)
    {
        var p = await db.Profissionais.FindAsync([id], ct)
            ?? throw new AppException("Profissional não encontrado.", 404);
        p.Nome = req.Nome;
        p.Telefone = req.Telefone;
        p.Ativo = req.Ativo;
        await db.SaveChangesAsync(ct);
        return new ProfissionalResponse(p.Id, p.Nome, p.Telefone, p.Ativo);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Profissionais.FindAsync([id], ct)
            ?? throw new AppException("Profissional não encontrado.", 404);
        var temAgendamentos = await db.Agendamentos
            .AnyAsync(a => a.ProfissionalId == id && a.Status != Domain.Enums.AgendamentoStatus.Cancelado, ct);
        if (temAgendamentos)
            throw new AppException("Profissional possui agendamentos ativos e não pode ser excluído.", 400);
        db.Profissionais.Remove(p);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<DisponibilidadeItem>> GetDisponibilidadeAsync(Guid id, CancellationToken ct)
    {
        _ = await db.Profissionais.FindAsync([id], ct)
            ?? throw new AppException("Profissional não encontrado.", 404);
        return await db.DisponibilidadeSemanais
            .Where(d => d.ProfissionalId == id)
            .OrderBy(d => d.DiaSemana).ThenBy(d => d.HoraInicio)
            .Select(d => new DisponibilidadeItem(
                d.DiaSemana,
                $"{d.HoraInicio.Hours:D2}:{d.HoraInicio.Minutes:D2}",
                $"{d.HoraFim.Hours:D2}:{d.HoraFim.Minutes:D2}"))
            .ToListAsync(ct);
    }

    public async Task SalvarDisponibilidadeAsync(Guid id, SalvarDisponibilidadeRequest req, CancellationToken ct)
    {
        _ = await db.Profissionais.FindAsync([id], ct)
            ?? throw new AppException("Profissional não encontrado.", 404);

        var existentes = await db.DisponibilidadeSemanais
            .Where(d => d.ProfissionalId == id)
            .ToListAsync(ct);
        db.DisponibilidadeSemanais.RemoveRange(existentes);

        foreach (var faixa in req.Faixas)
        {
            if (!TimeSpan.TryParseExact(faixa.HoraInicio, @"hh\:mm", null, out var inicio))
                throw new AppException($"HoraInicio inválida: {faixa.HoraInicio}");
            if (!TimeSpan.TryParseExact(faixa.HoraFim, @"hh\:mm", null, out var fim))
                throw new AppException($"HoraFim inválida: {faixa.HoraFim}");
            if (fim <= inicio)
                throw new AppException("HoraFim deve ser posterior a HoraInicio.");

            db.DisponibilidadeSemanais.Add(new DisponibilidadeSemanal
            {
                ProfissionalId = id,
                DiaSemana = faixa.DiaSemana,
                HoraInicio = inicio,
                HoraFim = fim,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<List<BloqueioResponse>> ListBloqueiosAsync(DateTime de, DateTime ate, CancellationToken ct) =>
        await db.BloqueiosAgenda
            .Include(b => b.Profissional)
            .Where(b => b.DataInicio < ate && b.DataFim > de)
            .OrderBy(b => b.DataInicio)
            .Select(b => new BloqueioResponse(
                b.Id, b.ProfissionalId, b.Profissional != null ? b.Profissional.Nome : null,
                b.DataInicio, b.DataFim, b.Motivo))
            .ToListAsync(ct);

    public async Task<BloqueioResponse> CriarBloqueioAsync(CriarBloqueioRequest req, CancellationToken ct)
    {
        if (req.DataFim <= req.DataInicio)
            throw new AppException("DataFim deve ser posterior a DataInicio.");

        var b = new BloqueioAgenda
        {
            EmpresaId = tenantContext.EmpresaId,
            ProfissionalId = req.ProfissionalId,
            DataInicio = req.DataInicio,
            DataFim = req.DataFim,
            Motivo = req.Motivo,
        };
        db.BloqueiosAgenda.Add(b);
        await db.SaveChangesAsync(ct);

        string? nomeProfissional = null;
        if (req.ProfissionalId.HasValue)
        {
            var prof = await db.Profissionais.FindAsync([req.ProfissionalId.Value], ct);
            nomeProfissional = prof?.Nome;
        }
        return new BloqueioResponse(b.Id, b.ProfissionalId, nomeProfissional, b.DataInicio, b.DataFim, b.Motivo);
    }

    public async Task DeleteBloqueioAsync(Guid id, CancellationToken ct)
    {
        var b = await db.BloqueiosAgenda.FindAsync([id], ct)
            ?? throw new AppException("Bloqueio não encontrado.", 404);
        db.BloqueiosAgenda.Remove(b);
        await db.SaveChangesAsync(ct);
    }
}
