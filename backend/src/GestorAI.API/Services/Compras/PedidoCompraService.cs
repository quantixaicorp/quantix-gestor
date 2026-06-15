using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Compras;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Compras;

public class PedidoCompraService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<PedidoCompraResponse>> ListAsync(
        string? status, Guid? fornecedorId, CancellationToken ct)
    {
        var query = db.PedidosCompra
            .Include(p => p.Fornecedor)
            .Include(p => p.Itens)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusPedidoCompra>(status, out var s))
            query = query.Where(p => p.Status == s);
        if (fornecedorId.HasValue)
            query = query.Where(p => p.FornecedorId == fornecedorId.Value);

        var list = await query.OrderByDescending(p => p.Numero).ToListAsync(ct);
        return list.Select(ToResponse).ToList();
    }

    public async Task<PedidoCompraResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var p = await db.PedidosCompra
            .Include(x => x.Fornecedor)
            .Include(x => x.Itens)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Pedido de compra não encontrado.", 404);
        return ToResponse(p);
    }

    public async Task<PedidoCompraResponse> CreateAsync(CreatePedidoCompraRequest req, CancellationToken ct)
    {
        var numero = await NextNumeroAsync(ct);

        var pedido = new PedidoCompra
        {
            EmpresaId = tenantContext.EmpresaId,
            Numero = numero,
            Data = req.Data,
            FornecedorId = req.FornecedorId,
            Observacoes = req.Observacoes,
            Status = StatusPedidoCompra.Rascunho,
        };

        foreach (var itemReq in req.Itens)
        {
            pedido.Itens.Add(new ItemPedidoCompra
            {
                EmpresaId = tenantContext.EmpresaId,
                PedidoCompraId = pedido.Id,
                ProdutoId = itemReq.ProdutoId,
                Descricao = itemReq.Descricao,
                Quantidade = itemReq.Quantidade,
                ValorEstimado = itemReq.ValorEstimado,
            });
        }

        pedido.ValorEstimado = pedido.Itens.Sum(i => i.Quantidade * i.ValorEstimado);
        db.PedidosCompra.Add(pedido);
        await db.SaveChangesAsync(ct);

        return await GetAsync(pedido.Id, ct);
    }

    public async Task<PedidoCompraResponse> UpdateAsync(Guid id, UpdatePedidoCompraRequest req, CancellationToken ct)
    {
        var pedido = await db.PedidosCompra
            .Include(p => p.Itens)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("Pedido de compra não encontrado.", 404);

        if (pedido.Status is StatusPedidoCompra.RecebidoTotalmente or StatusPedidoCompra.Cancelado)
            throw new AppException("Este pedido não pode ser editado.", 400);

        pedido.FornecedorId = req.FornecedorId;
        pedido.Data = req.Data;
        pedido.Observacoes = req.Observacoes;

        db.ItensPedidoCompra.RemoveRange(pedido.Itens);
        pedido.Itens.Clear();

        foreach (var itemReq in req.Itens)
        {
            pedido.Itens.Add(new ItemPedidoCompra
            {
                EmpresaId = tenantContext.EmpresaId,
                PedidoCompraId = pedido.Id,
                ProdutoId = itemReq.ProdutoId,
                Descricao = itemReq.Descricao,
                Quantidade = itemReq.Quantidade,
                ValorEstimado = itemReq.ValorEstimado,
            });
        }

        pedido.ValorEstimado = pedido.Itens.Sum(i => i.Quantidade * i.ValorEstimado);
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    public async Task<CompraResponse> ConverterAsync(Guid id, CancellationToken ct)
    {
        var pedido = await db.PedidosCompra
            .Include(p => p.Itens)
            .Include(p => p.Fornecedor)
            .FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new AppException("Pedido de compra não encontrado.", 404);

        if (pedido.Status is StatusPedidoCompra.Cancelado or StatusPedidoCompra.RecebidoTotalmente)
            throw new AppException("Pedido não pode ser convertido.", 400);

        var numeroCompra = await db.Compras
            .Where(c => c.EmpresaId == tenantContext.EmpresaId)
            .MaxAsync(c => (int?)c.Numero, ct) ?? 0;

        var compra = new Compra
        {
            EmpresaId = tenantContext.EmpresaId,
            Numero = numeroCompra + 1,
            Data = DateTime.UtcNow.Date,
            FornecedorId = pedido.FornecedorId,
            PedidoCompraId = pedido.Id,
            TipoCompra = "Mercadoria",
            CondicaoPagamento = "AVista",
            FormaPagamento = "PIX",
            Status = StatusCompra.Rascunho,
        };

        foreach (var item in pedido.Itens)
        {
            compra.Itens.Add(new ItemCompra
            {
                EmpresaId = tenantContext.EmpresaId,
                CompraId = compra.Id,
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao,
                DestinoCompra = DestinoCompra.EstoqueParaVenda,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorEstimado,
                ValorTotal = item.Quantidade * item.ValorEstimado,
            });
        }

        compra.ValorTotal = compra.Itens.Sum(i => i.ValorTotal);
        db.Compras.Add(compra);

        if (pedido.Status == StatusPedidoCompra.Rascunho || pedido.Status == StatusPedidoCompra.AguardandoAprovacao)
            pedido.Status = StatusPedidoCompra.Aprovado;

        await db.SaveChangesAsync(ct);

        var result = await db.Compras
            .Include(c => c.Fornecedor)
            .Include(c => c.Itens)
            .Include(c => c.Parcelamento)
            .FirstOrDefaultAsync(c => c.Id == compra.Id, ct);

        return CompraToResponse(result!);
    }

    public async Task<PedidoCompraResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var pedido = await db.PedidosCompra.FindAsync([id], ct)
            ?? throw new AppException("Pedido de compra não encontrado.", 404);

        if (pedido.Status == StatusPedidoCompra.Cancelado)
            throw new AppException("Pedido já está cancelado.", 400);

        pedido.Status = StatusPedidoCompra.Cancelado;
        await db.SaveChangesAsync(ct);

        return await GetAsync(id, ct);
    }

    private async Task<int> NextNumeroAsync(CancellationToken ct)
    {
        var max = await db.PedidosCompra
            .Where(p => p.EmpresaId == tenantContext.EmpresaId)
            .MaxAsync(p => (int?)p.Numero, ct) ?? 0;
        return max + 1;
    }

    private static PedidoCompraResponse ToResponse(PedidoCompra p) =>
        new(p.Id, p.Numero, p.Data, p.FornecedorId,
            p.Fornecedor?.Nome ?? "",
            p.Status.ToString(), p.ValorEstimado, p.Observacoes,
            p.CriadoEm,
            p.Itens.Select(i => new ItemPedidoResponse(
                i.Id, i.ProdutoId, i.Descricao, i.Quantidade, i.ValorEstimado)).ToList());

    private static CompraResponse CompraToResponse(Compra c)
    {
        var itens = c.Itens.Select(i => new ItemCompraResponse(
            i.Id, i.ProdutoId, i.Descricao, i.DestinoCompra.ToString(),
            i.Quantidade, i.ValorUnitario, i.Desconto, i.FreteRateado,
            i.Impostos, i.ValorTotal, i.CategoriaFinanceira, i.CentroCusto)).ToList();

        return new CompraResponse(
            c.Id, c.Numero, c.Data, c.FornecedorId,
            c.Fornecedor?.Nome ?? "",
            c.PedidoCompraId, c.TipoCompra, c.NumeroNota,
            c.CondicaoPagamento, c.FormaPagamento,
            c.Status.ToString(), c.ValorTotal, c.Observacoes,
            c.CriadaEm, itens, null);
    }
}
