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
        var query = db.Parcelamentos
            .Include(p => p.Parcelas)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusParcelamento>(status, out var s))
            query = query.Where(p => p.Status == s);
        if (compraId.HasValue)
            query = query.Where(p => p.CompraId == compraId.Value);

        var list = await query.OrderByDescending(p => p.CompraId).ToListAsync(ct);
        return list.Select(ToDetalhe).ToList();
    }

    public async Task<ParcelamentoDetalheResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.Parcelamentos
            .Include(x => x.Parcelas)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Parcelamento não encontrado.", 404);
        return ToDetalhe(p);
    }

    public async Task<Parcelamento> CriarAsync(
        Guid compraId,
        string descricao,
        decimal valorTotal,
        List<(DateTime DataVencimento, decimal Valor)> vencimentos,
        string categoriaDefault,
        CancellationToken ct)
    {
        var parcelamento = new Parcelamento
        {
            EmpresaId = tenantContext.EmpresaId,
            CompraId = compraId,
            Descricao = descricao,
            ValorTotal = valorTotal,
            QtdParcelas = vencimentos.Count,
            Status = StatusParcelamento.EmAberto,
        };
        db.Parcelamentos.Add(parcelamento);

        for (var i = 0; i < vencimentos.Count; i++)
        {
            var (dataVenc, valor) = vencimentos[i];
            db.Lancamentos.Add(new Lancamento
            {
                EmpresaId = tenantContext.EmpresaId,
                Tipo = TipoLancamento.Despesa,
                Descricao = $"{descricao} - Parcela {i + 1}/{vencimentos.Count}",
                Valor = valor,
                DataVencimento = dataVenc,
                Status = StatusLancamento.Pendente,
                Categoria = categoriaDefault,
                ParcelamentoId = parcelamento.Id,
                NumeroParcela = i + 1,
            });
        }

        return parcelamento;
    }

    public async Task RecalcularStatusAsync(Guid parcelamentoId, CancellationToken ct)
    {
        var parcelamento = await db.Parcelamentos
            .Include(p => p.Parcelas)
            .FirstOrDefaultAsync(p => p.Id == parcelamentoId, ct);

        if (parcelamento is null || parcelamento.Status == StatusParcelamento.Cancelado)
            return;

        var parcelas = parcelamento.Parcelas
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
        var parcelamento = await db.Parcelamentos
            .Include(p => p.Parcelas)
            .FirstOrDefaultAsync(p => p.Id == parcelamentoId, ct);

        if (parcelamento is null) return;

        foreach (var parcela in parcelamento.Parcelas.Where(l => l.Status == StatusLancamento.Pendente))
            parcela.Status = StatusLancamento.Cancelado;

        parcelamento.Status = StatusParcelamento.Cancelado;
    }

    private static ParcelamentoDetalheResponse ToDetalhe(Parcelamento p)
    {
        var hoje = DateTime.UtcNow.Date;
        var parcelas = p.Parcelas
            .OrderBy(l => l.NumeroParcela)
            .Select(l => new ParcelaResponse(
                l.Id,
                l.NumeroParcela ?? 0,
                l.Valor,
                l.DataVencimento,
                l.DataPagamento,
                l.Status.ToString(),
                l.Status == StatusLancamento.Pendente && l.DataVencimento.Date < hoje))
            .ToList();

        return new ParcelamentoDetalheResponse(
            p.Id, p.CompraId, p.Descricao, p.ValorTotal,
            p.QtdParcelas, p.Status.ToString(), parcelas);
    }
}
