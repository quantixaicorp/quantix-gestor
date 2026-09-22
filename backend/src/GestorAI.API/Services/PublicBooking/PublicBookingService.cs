using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.PublicBooking;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Asaas;
using GestorAI.API.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.PublicBooking;

public class PublicBookingService(AppDbContext db, AsaasService asaasService)
{
    public async Task<Guid> ResolveEmpresaAsync(string slug, CancellationToken ct)
    {
        var config = await db.CompanySettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Slug == slug, ct)
            ?? throw new AppException("Empresa não encontrada.", 404);
        return config.CompanyId;
    }

    public async Task<PublicEmpresaInfo> GetInfoAsync(Guid empresaId, CancellationToken ct)
    {
        var config = await db.CompanySettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CompanyId == empresaId, ct)
            ?? throw new AppException("Empresa não encontrada.", 404);
        return new PublicEmpresaInfo(
            config.NomeFantasia ?? config.RazaoSocial ?? "Empresa",
            config.LogoUrl,
            config.PrimaryColor,
            config.PublicDescription);
    }

    public async Task<List<PublicServicoResponse>> GetServicosAsync(Guid empresaId, CancellationToken ct) =>
        await db.Products
            .IgnoreQueryFilters()
            .Where(p => p.CompanyId == empresaId && p.Type == TipoProduto.Servico && p.IsActive && p.DurationMinutes != null)
            .OrderBy(p => p.Name)
            .Select(p => new PublicServicoResponse(p.Id, p.Name, p.SalePrice, p.DurationMinutes))
            .ToListAsync(ct);

    public async Task<List<PublicProfissionalResponse>> GetProfissionaisAsync(Guid empresaId, CancellationToken ct) =>
        await db.Professionals
            .IgnoreQueryFilters()
            .Where(p => p.CompanyId == empresaId && p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new PublicProfissionalResponse(p.Id, p.Name))
            .ToListAsync(ct);

    public async Task<List<int>> GetDiasComDisponibilidadeAsync(Guid empresaId, Guid profissionalId, CancellationToken ct)
    {
        var profissional = await db.Professionals
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == profissionalId && p.CompanyId == empresaId, ct)
            ?? throw new AppException("Professional não encontrado.", 404);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.WeeklyAvailabilities
            .IgnoreQueryFilters()
            .Where(d => d.ProfessionalId == profissionalId
                && d.StartDate > DateOnly.MinValue
                && d.EndDate >= hoje)
            .Select(d => d.WeekDay)
            .Distinct()
            .ToListAsync(ct);
    }

    public async Task<List<DateTime>> GetSlotsAsync(
        Guid empresaId, Guid profissionalId, Guid servicoId, DateOnly data, CancellationToken ct)
    {
        var diaSemana = (int)data.DayOfWeek;

        var faixas = await db.WeeklyAvailabilities
            .IgnoreQueryFilters()
            .Where(d => d.ProfessionalId == profissionalId
                && d.WeekDay == diaSemana
                && d.StartDate <= data && d.EndDate >= data)
            .ToListAsync(ct);

        if (faixas.Count == 0) return [];

        var servico = await db.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == servicoId && p.CompanyId == empresaId && p.DurationMinutes != null, ct)
            ?? throw new AppException("Serviço não encontrado.", 404);

        var duracao = TimeSpan.FromMinutes(servico.DurationMinutes!.Value);
        var inicioDia = data.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var fimDia = inicioDia.AddDays(1);

        var bloqueios = await db.ScheduleBlocks
            .IgnoreQueryFilters()
            .Where(b => b.CompanyId == empresaId
                && b.StartDate < fimDia && b.EndDate > inicioDia
                && (b.ProfessionalId == null || b.ProfessionalId == profissionalId))
            .ToListAsync(ct);

        var ocupados = await db.Appointments
            .IgnoreQueryFilters()
            .Where(a => a.CompanyId == empresaId
                && a.ProfessionalId == profissionalId
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

    public async Task<PublicAgendamentoConfirmado> CriarAgendamentoAsync(
        Guid empresaId, PublicCriarAgendamentoRequest req, CancellationToken ct)
    {
        _ = await db.Professionals
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == req.ProfessionalId && p.CompanyId == empresaId, ct)
            ?? throw new AppException("Professional não encontrado.", 404);

        var servico = await db.Products
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == req.ServiceId && p.CompanyId == empresaId && p.DurationMinutes != null, ct)
            ?? throw new AppException("Serviço não encontrado.", 404);

        var dataHoraFim = req.StartAt.AddMinutes(servico.DurationMinutes!.Value);

        var conflito = await db.Appointments
            .IgnoreQueryFilters()
            .AnyAsync(a => a.CompanyId == empresaId
                && a.ProfessionalId == req.ProfessionalId
                && a.Status != AgendamentoStatus.Cancelado
                && a.StartAt < dataHoraFim && a.EndAt > req.StartAt, ct);

        if (conflito)
            throw new AppException("Horário não está mais disponível.", 409);

        var config = await db.CompanySettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CompanyId == empresaId, ct);

        var statusInicial = (config?.AutoApprove ?? true)
            ? AgendamentoStatus.Agendado
            : AgendamentoStatus.AguardandoConfirmacao;

        var agendamento = new Appointment
        {
            CompanyId = empresaId,
            ProfessionalId = req.ProfessionalId,
            CustomerName = req.CustomerName,
            CustomerPhone = req.CustomerPhone,
            ServiceId = req.ServiceId,
            StartAt = req.StartAt,
            EndAt = dataHoraFim,
            Status = statusInicial,
        };

        db.Appointments.Add(agendamento);
        await db.SaveChangesAsync(ct);

        // Criar cobrança de sinal PIX se configurado — falha silenciosa para não bloquear o agendamento
        string? sinalPixQrCode = null;
        decimal? sinalValor = null;
        if (config?.DepositAmount > 0 && !string.IsNullOrWhiteSpace(config.AsaasApiKey))
        {
            try
            {
                var customerId = await asaasService.GetOrCreateCustomerAsync(
                    config.AsaasApiKey, config.AsaasSandbox,
                    req.CustomerName, null, ct);

                var vencimento = DateOnly.FromDateTime(req.StartAt);
                var result = await asaasService.CreatePaymentAsync(
                    config.AsaasApiKey, config.AsaasSandbox,
                    customerId, config.DepositAmount.Value,
                    vencimento,
                    $"Sinal para agendamento em {req.StartAt:dd/MM/yyyy HH:mm}",
                    "PIX", ct);

                agendamento.DepositAsaasId = result.Id;
                agendamento.DepositPixQrCode = result.PixQrCode?.Payload;
                await db.SaveChangesAsync(ct);

                sinalPixQrCode = agendamento.DepositPixQrCode;
                sinalValor = config.DepositAmount;
            }
            catch
            {
                // Sinal é opcional — não bloquear o agendamento se Asaas falhar
            }
        }

        var profissionalNome = (await db.Professionals.IgnoreQueryFilters()
            .FirstAsync(p => p.Id == req.ProfessionalId, ct)).Name;

        return new PublicAgendamentoConfirmado(
            agendamento.Id,
            servico.Name,
            profissionalNome,
            agendamento.StartAt,
            agendamento.EndAt,
            sinalPixQrCode,
            sinalValor);
    }
}
