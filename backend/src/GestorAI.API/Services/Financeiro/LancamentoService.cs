using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Financeiro;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Financeiro;

public class LancamentoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<LancamentoResponse>> ListAsync(
        string? tipo, string? status, DateTime? vencimentoAte, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var query = db.Lancamentos.AsQueryable();

        if (!string.IsNullOrEmpty(tipo) && Enum.TryParse<TipoLancamento>(tipo, out var t))
            query = query.Where(l => l.Tipo == t);
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusLancamento>(status, out var s))
            query = query.Where(l => l.Status == s);
        if (vencimentoAte.HasValue)
            query = query.Where(l => l.DataVencimento <= vencimentoAte.Value);

        return await query
            .OrderBy(l => l.DataVencimento)
            .Select(l => ToResponse(l, hoje))
            .ToListAsync(ct);
    }

    public async Task<LancamentoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var l = await db.Lancamentos.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);
        return ToResponse(l, hoje);
    }

    public async Task<LancamentoResponse> CreateAsync(CreateLancamentoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoLancamento>(req.Tipo, out var tipo))
            throw new AppException("Tipo inválido.");

        var lancamento = new Lancamento
        {
            EmpresaId = tenantContext.EmpresaId,
            Tipo = tipo,
            Descricao = req.Descricao,
            Valor = req.Valor,
            DataVencimento = req.DataVencimento,
            Status = tipo == TipoLancamento.Receita ? StatusLancamento.Pago : StatusLancamento.Pendente,
            DataPagamento = tipo == TipoLancamento.Receita ? req.DataVencimento : null,
            Categoria = req.Categoria,
            Observacao = req.Observacao,
        };

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        db.Lancamentos.Add(lancamento);
        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return ToResponse(lancamento, DateTime.UtcNow.Date);
    }

    public async Task<LancamentoResponse> PagarAsync(
        Guid id, PagarLancamentoRequest req, CancellationToken ct)
    {
        var lancamento = await db.Lancamentos.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (lancamento.Status == StatusLancamento.Pago)
            throw new AppException("Lançamento já está pago.");
        if (lancamento.Status == StatusLancamento.Cancelado)
            throw new AppException("Lançamento cancelado não pode ser pago.");

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        lancamento.Status = StatusLancamento.Pago;
        lancamento.DataPagamento = req.DataPagamento;

        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return ToResponse(lancamento, DateTime.UtcNow.Date);
    }

    public async Task<LancamentoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var lancamento = await db.Lancamentos.FindAsync([id], ct)
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
        var lancamento = await db.Lancamentos.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (lancamento.VendaId.HasValue)
            throw new AppException("Lançamentos gerados por vendas não podem ser excluídos diretamente.", 400);

        db.Lancamentos.Remove(lancamento);
        await db.SaveChangesAsync(ct);
    }

    public async Task<FluxoCaixaResponse> GetFluxoCaixaAsync(
        DateTime de, DateTime ate, CancellationToken ct)
    {
        var lancamentos = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pago
                && l.DataPagamento.HasValue
                && l.DataPagamento.Value.Date >= de.Date
                && l.DataPagamento.Value.Date <= ate.Date)
            .ToListAsync(ct);

        var agrupados = lancamentos
            .GroupBy(l => l.DataPagamento!.Value.Date)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var r = g.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor);
                var d = g.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor);
                return new FluxoCaixaItemResponse(g.Key, r, d, r - d);
            })
            .ToList();

        var totalR = lancamentos.Where(l => l.Tipo == TipoLancamento.Receita).Sum(l => l.Valor);
        var totalD = lancamentos.Where(l => l.Tipo == TipoLancamento.Despesa).Sum(l => l.Valor);

        return new FluxoCaixaResponse(totalR, totalD, totalR - totalD, agrupados);
    }

    public async Task<LancamentoResumo> GetResumoAsync(CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalReceitas = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pago
                     && l.Tipo == TipoLancamento.Receita
                     && l.DataPagamento.HasValue
                     && l.DataPagamento.Value >= inicioMes)
            .SumAsync(l => (decimal?)l.Valor, ct) ?? 0m;

        var totalDespesas = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pago
                     && l.Tipo == TipoLancamento.Despesa
                     && l.DataPagamento.HasValue
                     && l.DataPagamento.Value >= inicioMes)
            .SumAsync(l => (decimal?)l.Valor, ct) ?? 0m;

        var totalPendente = await db.Lancamentos
            .Where(l => l.Status == StatusLancamento.Pendente
                     && l.DataVencimento >= hoje)
            .SumAsync(l => (decimal?)l.Valor, ct) ?? 0m;

        return new LancamentoResumo(totalReceitas, totalDespesas, totalReceitas - totalDespesas, totalPendente);
    }

    public async Task<LancamentoResponse> UpdateAsync(Guid id, UpdateLancamentoRequest req, CancellationToken ct)
    {
        var l = await db.Lancamentos.FindAsync([id], ct)
            ?? throw new AppException("Lançamento não encontrado.", 404);

        if (l.Status != StatusLancamento.Pendente)
            throw new AppException("Apenas lançamentos pendentes podem ser editados.", 400);

        if (l.VendaId.HasValue)
            throw new AppException("Lançamentos gerados por vendas não podem ser editados.", 400);

        if (!Enum.TryParse<TipoLancamento>(req.Tipo, out var tipo))
            throw new AppException($"Tipo inválido: {req.Tipo}.", 400);

        l.Tipo = tipo;
        l.Descricao = req.Descricao;
        l.Valor = req.Valor;
        l.DataVencimento = req.DataVencimento;
        l.Categoria = req.Categoria;
        l.Observacao = req.Observacao;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    private static LancamentoResponse ToResponse(Lancamento l, DateTime hoje) => new(
        l.Id, l.Tipo.ToString(), l.Descricao, l.Valor,
        l.DataVencimento, l.DataPagamento, l.Status.ToString(),
        l.Categoria, l.VendaId, l.Observacao,
        l.Status == StatusLancamento.Pendente && l.DataVencimento.Date < hoje);
}
