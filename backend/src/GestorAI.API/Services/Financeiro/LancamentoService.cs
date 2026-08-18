using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Compras;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Financeiro;

public class LancamentoService(AppDbContext db, TenantContext tenantContext, ParcelamentoService parcelamentoService)
{
    public async Task<List<LancamentoResponse>> ListAsync(
        string? tipo, string? status, DateTime? vencimentoAte, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var query = db.Transactions.AsQueryable();

        if (!string.IsNullOrEmpty(tipo) && Enum.TryParse<TipoLancamento>(tipo, out var t))
            query = query.Where(l => l.Type == t);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusLancamento>(status, out var s))
            query = query.Where(l => l.Status == s);
        if (vencimentoAte.HasValue)
            query = query.Where(l => l.DueDate <= vencimentoAte.Value);

        return await query
            .OrderByDescending(l => l.DueDate)
            .Select(l => ToResponse(l, hoje))
            .ToListAsync(ct);
    }

    public async Task<LancamentoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var l = await db.Transactions.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);
        return ToResponse(l, hoje);
    }

    public async Task<LancamentoResponse> CreateAsync(CreateLancamentoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoLancamento>(req.Type, out var tipo))
            throw new AppException("Type inválido.");

        var lancamento = new Transaction
        {
            CompanyId = tenantContext.CompanyId,
            Type = tipo,
            Description = req.Description,
            Amount = req.Amount,
            DueDate = req.DueDate,
            Status = StatusLancamento.Pendente,
            PaymentDate = null,
            Category = req.Category,
            Notes = req.Notes,
        };

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        db.Transactions.Add(lancamento);
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return ToResponse(lancamento, DateTime.UtcNow.Date);
    }

    public async Task<Guid> CreateParceladoAsync(CreateParceladoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoLancamento>(req.Type, out var tipo))
            throw new AppException("Type inválido.");
        if (req.Installments.Count < 2)
            throw new AppException("InstallmentPlan deve ter ao menos 2 parcelas.");

        var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var n = req.Installments.Count;
            var valorTotal = req.Installments.Sum(p => p.Amount);
            var parcelamento = new InstallmentPlan
            {
                CompanyId = tenantContext.CompanyId,
                Description = req.Description,
                TotalAmount = valorTotal,
                InstallmentCount = n,
                Status = Domain.Enums.StatusParcelamento.EmAberto,
            };
            db.InstallmentPlans.Add(parcelamento);

            for (var i = 0; i < n; i++)
            {
                db.Transactions.Add(new Transaction
                {
                    CompanyId = tenantContext.CompanyId,
                    Type = tipo,
                    Description = $"{req.Description} - Parcela {i + 1}/{n}",
                    Amount = req.Installments[i].Amount,
                    DueDate = req.Installments[i].DueDate,
                    Status = StatusLancamento.Pendente,
                    Category = req.Category,
                    Notes = req.Notes,
                    InstallmentPlanId = parcelamento.Id,
                    InstallmentNumber = i + 1,
                });
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return parcelamento.Id;
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    public async Task<LancamentoResponse> PagarAsync(
        Guid id, PagarLancamentoRequest req, CancellationToken ct)
    {
        var lancamento = await db.Transactions.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (lancamento.Status == StatusLancamento.Pago)
            throw new AppException("Lançamento já está pago.");
        if (lancamento.Status == StatusLancamento.Cancelado)
            throw new AppException("Lançamento cancelado não pode ser pago.");

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        lancamento.Status = StatusLancamento.Pago;
        lancamento.PaymentDate = req.PaymentDate;

        await db.SaveChangesAsync(ct);

        if (lancamento.InstallmentPlanId.HasValue)
            await parcelamentoService.RecalcularStatusAsync(lancamento.InstallmentPlanId.Value, ct);

        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return ToResponse(lancamento, DateTime.UtcNow.Date);
    }

    public async Task<LancamentoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var lancamento = await db.Transactions.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (lancamento.Status == StatusLancamento.Pago)
            throw new AppException("Lançamento pago não pode ser cancelado.");

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        lancamento.Status = StatusLancamento.Cancelado;
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return ToResponse(lancamento, DateTime.UtcNow.Date);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var lancamento = await db.Transactions.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (lancamento.SaleId.HasValue)
            throw new AppException("Lançamentos gerados por vendas não podem ser excluídos diretamente.", 400);

        db.Transactions.Remove(lancamento);
        await db.SaveChangesAsync(ct);
    }

    public async Task<FluxoCaixaResponse> GetFluxoCaixaAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var lancamentos = await db.Transactions
            .Where(l => l.Status == StatusLancamento.Pago
                && l.PaymentDate.HasValue
                && l.PaymentDate.Value.Date >= de.Date
                && l.PaymentDate.Value.Date <= ate.Date)
            .ToListAsync(ct);

        var agrupados = lancamentos
            .GroupBy(l => l.PaymentDate!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var r = g.Where(l => l.Type == TipoLancamento.Receita).Sum(l => l.Amount);
                var d = g.Where(l => l.Type == TipoLancamento.Despesa).Sum(l => l.Amount);
                return new FluxoCaixaItemResponse(g.Key, r, d, r - d);
            })
            .ToList();

        var totalR = lancamentos.Where(l => l.Type == TipoLancamento.Receita).Sum(l => l.Amount);
        var totalD = lancamentos.Where(l => l.Type == TipoLancamento.Despesa).Sum(l => l.Amount);

        return new FluxoCaixaResponse(totalR, totalD, totalR - totalD, agrupados);
    }

    public async Task<LancamentoResumo> GetResumoAsync(CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var totalReceitas = await db.Transactions
            .Where(l => l.Status == StatusLancamento.Pago
                     && l.Type == TipoLancamento.Receita
                     && l.PaymentDate.HasValue
                     && l.PaymentDate.Value >= inicioMes)
            .SumAsync(l => (decimal?)l.Amount, ct) ?? 0m;

        var totalDespesas = await db.Transactions
            .Where(l => l.Status == StatusLancamento.Pago
                     && l.Type == TipoLancamento.Despesa
                     && l.PaymentDate.HasValue
                     && l.PaymentDate.Value >= inicioMes)
            .SumAsync(l => (decimal?)l.Amount, ct) ?? 0m;

        var totalPendente = await db.Transactions
            .Where(l => l.Status == StatusLancamento.Pendente
                     && l.DueDate >= hoje)
            .SumAsync(l => (decimal?)l.Amount, ct) ?? 0m;

        return new LancamentoResumo(totalReceitas, totalDespesas, totalReceitas - totalDespesas, totalPendente);
    }

    public async Task<LancamentoResponse> UpdateAsync(Guid id, UpdateLancamentoRequest req, CancellationToken ct)
    {
        var l = await db.Transactions.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (l.Status != StatusLancamento.Pendente)
            throw new AppException("Apenas lançamentos pendentes podem ser editados.", 400);

        if (l.SaleId.HasValue)
            throw new AppException("Lançamentos gerados por vendas não podem ser editados.", 400);

        if (!Enum.TryParse<TipoLancamento>(req.Type, out var tipo))
            throw new AppException($"Type inválido: {req.Type}.", 400);

        l.Type = tipo;
        l.Description = req.Description;
        l.Amount = req.Amount;
        l.DueDate = req.DueDate;
        l.Category = req.Category;
        l.Notes = req.Notes;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    private static LancamentoResponse ToResponse(Transaction l, DateTime hoje) => new(
        l.Id, l.Type.ToString(), l.Description, l.Amount,
        l.DueDate, l.PaymentDate, l.Status.ToString(),
        l.Category, l.SaleId, l.Notes,
        l.Status == StatusLancamento.Pendente && l.DueDate.Date < hoje,
        l.InstallmentPlanId, l.InstallmentNumber);
}
