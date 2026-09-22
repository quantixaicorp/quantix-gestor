using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Compras;

public class ParcelamentoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ParcelamentoDetalheResponse>> ListAsync(
        string? status, Guid? compraId, CancellationToken ct)
    {
        var query = db.InstallmentPlans
            .Include(p => p.Installments)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusParcelamento>(status, out var s))
            query = query.Where(p => p.Status == s);
        if (compraId.HasValue)
            query = query.Where(p => p.PurchaseId == compraId.Value);

        var list = await query.OrderByDescending(p => p.PurchaseId).ToListAsync(ct);
        return list.Select(ToDetalhe).ToList();
    }

    public async Task<ParcelamentoDetalheResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.InstallmentPlans
            .Include(x => x.Installments)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("InstallmentPlan não encontrado.", 404);
        return ToDetalhe(p);
    }

    public async Task<InstallmentPlan> CriarAsync(
        Guid compraId,
        string descricao,
        decimal valorTotal,
        List<(DateTime DueDate, decimal Amount)> vencimentos,
        string categoriaDefault,
        CancellationToken ct)
    {
        var parcelamento = new InstallmentPlan
        {
            CompanyId = tenantContext.CompanyId,
            PurchaseId = compraId,
            Description = descricao,
            TotalAmount = valorTotal,
            InstallmentCount = vencimentos.Count,
            Status = StatusParcelamento.EmAberto,
        };
        db.InstallmentPlans.Add(parcelamento);

        for (var i = 0; i < vencimentos.Count; i++)
        {
            var (dataVenc, valor) = vencimentos[i];
            db.Transactions.Add(new Transaction
            {
                CompanyId = tenantContext.CompanyId,
                Type = TipoLancamento.Despesa,
                Description = $"{descricao} - Parcela {i + 1}/{vencimentos.Count}",
                Amount = valor,
                DueDate = dataVenc,
                Status = StatusLancamento.Pendente,
                Category = categoriaDefault,
                InstallmentPlanId = parcelamento.Id,
                InstallmentNumber = i + 1,
            });
        }

        return parcelamento;
    }

    public async Task RecalcularStatusAsync(Guid parcelamentoId, CancellationToken ct)
    {
        var parcelamento = await db.InstallmentPlans
            .Include(p => p.Installments)
            .FirstOrDefaultAsync(p => p.Id == parcelamentoId, ct);

        if (parcelamento is null || parcelamento.Status == StatusParcelamento.Cancelado)
            return;

        var parcelas = parcelamento.Installments
            .Where(l => l.Status != StatusLancamento.Cancelado)
            .ToList();

        if (!parcelas.Any())
        {
            parcelamento.Status = StatusParcelamento.Cancelado;
        }
        else if (parcelas.All(l => l.Status == StatusLancamento.Pago))
        {
            parcelamento.Status = StatusParcelamento.PagoTotal;
        }
        else if (parcelas.Any(l => l.Status == StatusLancamento.Pago))
        {
            parcelamento.Status = StatusParcelamento.PagoParcialmente;
        }
        else
        {
            parcelamento.Status = StatusParcelamento.EmAberto;
        }
    }

    public async Task CancelarParcelasAsync(Guid parcelamentoId, CancellationToken ct)
    {
        var parcelamento = await db.InstallmentPlans
            .Include(p => p.Installments)
            .FirstOrDefaultAsync(p => p.Id == parcelamentoId, ct);

        if (parcelamento is null) return;

        foreach (var parcela in parcelamento.Installments.Where(l => l.Status == StatusLancamento.Pendente))
            parcela.Status = StatusLancamento.Cancelado;

        parcelamento.Status = StatusParcelamento.Cancelado;
    }

    private static ParcelamentoDetalheResponse ToDetalhe(InstallmentPlan p)
    {
        var hoje = DateTime.UtcNow.Date;
        var parcelas = p.Installments
            .OrderBy(l => l.InstallmentNumber)
            .Select(l => new ParcelaResponse(
                l.Id,
                l.InstallmentNumber ?? 0,
                l.Amount,
                l.DueDate,
                l.PaymentDate,
                l.Status.ToString(),
                l.Status == StatusLancamento.Pendente && l.DueDate.Date < hoje))
            .ToList();

        var categoria = p.Installments.FirstOrDefault()?.Category ?? "";

        return new ParcelamentoDetalheResponse(
            p.Id, p.PurchaseId, p.Description, p.TotalAmount,
            p.InstallmentCount, p.Status.ToString(), categoria, parcelas);
    }
}
