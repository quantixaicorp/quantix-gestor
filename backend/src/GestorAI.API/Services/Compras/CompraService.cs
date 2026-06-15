using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Compras;

public class CompraService(
    AppDbContext db,
    TenantContext tenantContext,
    ParcelamentoService parcelamentoService)
{
    public async Task<List<CompraResponse>> ListAsync(
        string? status, Guid? fornecedorId, DateTime? de, DateTime? ate, CancellationToken ct)
    {
        var query = db.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.Itens)
            .Include(c => c.Parcelamento)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusCompra>(status, out var s))
            query = query.Where(c => c.Status == s);
        if (fornecedorId.HasValue)
            query = query.Where(c => c.FornecedorId == fornecedorId.Value);
        if (de.HasValue)
            query = query.Where(c => c.Data >= de.Value);
        if (ate.HasValue)
            query = query.Where(c => c.Data <= ate.Value);

        var list = await query.OrderByDescending(c => c.Numero).ToListAsync(ct);
        return list.Select(ToResponse).ToList();
    }

    public async Task<CompraResumoResponse> GetResumoAsync(CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);

        var comprasMes = await db.Compras
            .Where(c => c.Status == StatusCompra.Confirmada && c.Data >= inicioMes)
            .ToListAsync(ct);

        var totalContasPagar = await db.Parcelamentos
            .Include(p => p.Parcelas)
            .Where(p => p.CompraId != null && p.Status != StatusParcelamento.Cancelado)
            .SelectMany(p => p.Parcelas)
            .Where(l => l.Status == StatusLancamento.Pendente)
            .SumAsync(l => (decimal?)l.Valor, ct) ?? 0m;

        return new CompraResumoResponse(
            comprasMes.Sum(c => c.ValorTotal),
            comprasMes.Count,
            totalContasPagar);
    }

    public async Task<CompraResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Compras
            .Include(x => x.Fornecedor)
            .Include(x => x.Itens)
            .Include(x => x.Parcelamento).ThenInclude(p => p!.Parcelas)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Compra não encontrada.", 404);
        return ToResponse(c);
    }

    public async Task<CompraResponse> CreateAsync(CreateCompraRequest req, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var numero = await NextNumeroCompraAsync(ct);
        var compra = new Compra
        {
            EmpresaId = tenantContext.EmpresaId,
            Numero = numero,
            Data = req.Data,
            FornecedorId = req.FornecedorId,
            PedidoCompraId = req.PedidoCompraId,
            TipoCompra = req.TipoCompra,
            NumeroNota = req.NumeroNota,
            CondicaoPagamento = req.CondicaoPagamento,
            FormaPagamento = req.FormaPagamento,
            Status = StatusCompra.Rascunho,
            Observacoes = req.Observacoes,
        };

        foreach (var itemReq in req.Itens)
        {
            var item = BuildItem(itemReq);
            item.EmpresaId = tenantContext.EmpresaId;
            item.CompraId = compra.Id;
            compra.Itens.Add(item);
        }

        compra.ValorTotal = compra.Itens.Sum(i => i.ValorTotal);
        db.Compras.Add(compra);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetAsync(compra.Id, ct);
    }

    public async Task<CompraResponse> UpdateAsync(Guid id, UpdateCompraRequest req, CancellationToken ct)
    {
        var compra = await db.Compras
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Compra não encontrada.", 404);

        if (compra.Status != StatusCompra.Rascunho)
            throw new AppException("Apenas rascunhos podem ser editados.", 400);

        compra.FornecedorId = req.FornecedorId;
        compra.Data = req.Data;
        compra.NumeroNota = req.NumeroNota;
        compra.TipoCompra = req.TipoCompra;
        compra.PedidoCompraId = req.PedidoCompraId;
        compra.Observacoes = req.Observacoes;
        compra.CondicaoPagamento = req.CondicaoPagamento;
        compra.FormaPagamento = req.FormaPagamento;

        db.ItensCompra.RemoveRange(compra.Itens);
        compra.Itens.Clear();

        foreach (var itemReq in req.Itens)
        {
            var item = BuildItem(itemReq);
            item.EmpresaId = tenantContext.EmpresaId;
            item.CompraId = compra.Id;
            compra.Itens.Add(item);
        }

        compra.ValorTotal = compra.Itens.Sum(i => i.ValorTotal);
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<CompraResponse> ConfirmarAsync(Guid id, CancellationToken ct)
    {
        var compra = await db.Compras
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Compra não encontrada.", 404);

        if (compra.Status != StatusCompra.Rascunho)
            throw new AppException("Apenas rascunhos podem ser confirmados.", 400);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Atualizar estoque para itens com destino EstoqueParaVenda
        foreach (var item in compra.Itens.Where(i => i.DestinoCompra == DestinoCompra.EstoqueParaVenda && i.ProdutoId.HasValue))
        {
            var produto = await db.Produtos.FindAsync([item.ProdutoId!.Value], ct)
                ?? throw new AppException($"Produto {item.ProdutoId} não encontrado.", 404);

            var novoEstoque = produto.EstoqueAtual + item.Quantidade;
            if (novoEstoque > 0)
                produto.CustoMedio = (produto.EstoqueAtual * produto.CustoMedio + item.Quantidade * item.ValorUnitario) / novoEstoque;

            produto.EstoqueAtual = novoEstoque;
            produto.AtualizadoEm = DateTime.UtcNow;

            db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
            {
                EmpresaId = tenantContext.EmpresaId,
                ProdutoId = item.ProdutoId!.Value,
                Tipo = TipoMovimentacao.Entrada,
                Quantidade = item.Quantidade,
                Origem = OrigemMovimentacao.Compra,
                Observacao = $"Compra #{compra.Numero}",
            });
        }

        // Gerar parcelamento
        var categoriaDefault = compra.Itens.Select(i => i.CategoriaFinanceira)
            .FirstOrDefault(c => !string.IsNullOrEmpty(c)) ?? "Compras";

        var vencimentos = VencimentoCalculator.Calcular(
            compra.CondicaoPagamento,
            compra.Data,
            compra.ValorTotal,
            null,
            null);

        var descricao = $"Compra #{compra.Numero}";
        var parcelamento = await parcelamentoService.CriarAsync(
            compra.Id, descricao, compra.ValorTotal, vencimentos, categoriaDefault, ct);

        compra.Status = StatusCompra.Confirmada;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<CompraResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var compra = await db.Compras
            .Include(c => c.Itens)
            .Include(c => c.Parcelamento)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Compra não encontrada.", 404);

        if (compra.Status == StatusCompra.Cancelada)
            throw new AppException("Compra já está cancelada.", 400);

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        if (compra.Status == StatusCompra.Confirmada)
        {
            // Reverter estoque
            foreach (var item in compra.Itens.Where(i => i.DestinoCompra == DestinoCompra.EstoqueParaVenda && i.ProdutoId.HasValue))
            {
                var produto = await db.Produtos.FindAsync([item.ProdutoId!.Value], ct);
                if (produto is null) continue;

                produto.EstoqueAtual -= item.Quantidade;
                produto.AtualizadoEm = DateTime.UtcNow;

                db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    EmpresaId = tenantContext.EmpresaId,
                    ProdutoId = item.ProdutoId!.Value,
                    Tipo = TipoMovimentacao.Saida,
                    Quantidade = item.Quantidade,
                    Origem = OrigemMovimentacao.Manual,
                    Observacao = $"Cancelamento Compra #{compra.Numero}",
                });
            }

            // Cancelar parcelas
            if (compra.Parcelamento is not null)
                await parcelamentoService.CancelarParcelasAsync(compra.Parcelamento.Id, ct);
        }

        compra.Status = StatusCompra.Cancelada;
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var compra = await db.Compras.FindAsync([id], ct)
            ?? throw new AppException("Compra não encontrada.", 404);

        if (compra.Status != StatusCompra.Rascunho)
            throw new AppException("Apenas rascunhos podem ser excluídos.", 400);

        db.Compras.Remove(compra);
        await db.SaveChangesAsync(ct);
    }

    private async Task<int> NextNumeroCompraAsync(CancellationToken ct)
    {
        var max = await db.Compras
            .Where(c => c.EmpresaId == tenantContext.EmpresaId)
            .MaxAsync(c => (int?)c.Numero, ct) ?? 0;
        return max + 1;
    }

    private static ItemCompra BuildItem(ItemCompraRequest req)
    {
        if (!Enum.TryParse<DestinoCompra>(req.DestinoCompra, out var destino))
            throw new AppException($"DestinoCompra inválido: {req.DestinoCompra}", 400);

        var total = req.Quantidade * req.ValorUnitario - req.Desconto + req.FreteRateado + req.Impostos;

        return new ItemCompra
        {
            Id = Guid.NewGuid(),
            ProdutoId = req.ProdutoId,
            Descricao = req.Descricao,
            DestinoCompra = destino,
            Quantidade = req.Quantidade,
            ValorUnitario = req.ValorUnitario,
            Desconto = req.Desconto,
            FreteRateado = req.FreteRateado,
            Impostos = req.Impostos,
            ValorTotal = total,
            CategoriaFinanceira = req.CategoriaFinanceira,
            CentroCusto = req.CentroCusto,
        };
    }

    private static CompraResponse ToResponse(Compra c)
    {
        var itens = c.Itens.Select(i => new ItemCompraResponse(
            i.Id, i.ProdutoId, i.Descricao, i.DestinoCompra.ToString(),
            i.Quantidade, i.ValorUnitario, i.Desconto, i.FreteRateado,
            i.Impostos, i.ValorTotal, i.CategoriaFinanceira, i.CentroCusto)).ToList();

        ParcelamentoResumoResponse? parcelamentoResumo = c.Parcelamento is null ? null :
            new(c.Parcelamento.Id, c.Parcelamento.Descricao, c.Parcelamento.ValorTotal,
                c.Parcelamento.QtdParcelas, c.Parcelamento.Status.ToString());

        return new CompraResponse(
            c.Id, c.Numero, c.Data, c.FornecedorId,
            c.Fornecedor?.Nome ?? "",
            c.PedidoCompraId, c.TipoCompra, c.NumeroNota,
            c.CondicaoPagamento, c.FormaPagamento,
            c.Status.ToString(), c.ValorTotal, c.Observacoes,
            c.CriadaEm, itens, parcelamentoResumo);
    }
}
