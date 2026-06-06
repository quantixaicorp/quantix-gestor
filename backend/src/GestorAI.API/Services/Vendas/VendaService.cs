using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Vendas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Vendas;

public class VendaService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<VendaResponse> CreateAsync(CreateVendaRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<FormaPagamento>(req.FormaPagamento, out var formaPagamento))
            throw new AppException("Forma de pagamento inválida.");

        var produtoIds = req.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await db.Produtos
            .Where(p => produtoIds.Contains(p.Id))
            .ToListAsync(ct);

        foreach (var item in req.Itens)
        {
            var produto = produtos.FirstOrDefault(p => p.Id == item.ProdutoId)
                ?? throw new AppException($"Produto {item.ProdutoId} não encontrado.", 404);
            if (produto.Tipo == TipoProduto.Produto && produto.EstoqueAtual < item.Quantidade)
                throw new AppException(
                    $"Estoque insuficiente para '{produto.Nome}'. " +
                    $"Disponível: {produto.EstoqueAtual}, solicitado: {item.Quantidade}.");
        }

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try
        {
            try { tx = await db.Database.BeginTransactionAsync(ct); } catch { /* InMemory doesn't support transactions */ }

            var itensEntidade = req.Itens.Select(item =>
            {
                var produto = produtos.First(p => p.Id == item.ProdutoId);
                var total = produto.PrecoVenda * item.Quantidade - item.Desconto;
                return new ItemVenda
                {
                    ProdutoId = item.ProdutoId,
                    Quantidade = item.Quantidade,
                    PrecoUnitario = produto.PrecoVenda,
                    Desconto = item.Desconto,
                    Total = total,
                };
            }).ToList();

            var subtotal = itensEntidade.Sum(i => i.PrecoUnitario * i.Quantidade);
            if (req.Desconto > subtotal)
                throw new AppException("Desconto não pode exceder o subtotal.");

            var total = subtotal - req.Desconto;

            var dataHora = req.DataHora.HasValue
                ? DateTime.SpecifyKind(req.DataHora.Value, DateTimeKind.Utc)
                : DateTime.UtcNow;

            var venda = new Venda
            {
                EmpresaId = tenantContext.EmpresaId,
                ClienteId = req.ClienteId,
                DataHora = dataHora,
                Status = StatusVenda.Concluida,
                Subtotal = subtotal,
                Desconto = req.Desconto,
                Total = total,
                FormaPagamento = formaPagamento,
                Parcelas = req.Parcelas,
                Observacao = req.Observacao,
            };
            db.Vendas.Add(venda);

            foreach (var item in itensEntidade)
            {
                item.VendaId = venda.Id;
                db.ItensVenda.Add(item);
            }

            foreach (var item in req.Itens)
            {
                var produto = produtos.First(p => p.Id == item.ProdutoId);
                produto.EstoqueAtual -= item.Quantidade;
                produto.AtualizadoEm = DateTime.UtcNow;

                db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    EmpresaId = tenantContext.EmpresaId,
                    ProdutoId = item.ProdutoId,
                    Tipo = TipoMovimentacao.Saida,
                    Quantidade = item.Quantidade,
                    Origem = OrigemMovimentacao.Venda,
                    ReferenciaId = venda.Id,
                });
            }

            var nomeCliente = req.ClienteId.HasValue
                ? (await db.Clientes.FindAsync([req.ClienteId.Value], ct))?.Nome ?? "Cliente"
                : "Venda balcão";

            db.Lancamentos.Add(new Lancamento
            {
                EmpresaId = tenantContext.EmpresaId,
                Tipo = TipoLancamento.Receita,
                Descricao = $"Venda — {nomeCliente}",
                Valor = total,
                DataVencimento = venda.DataHora,
                DataPagamento = venda.DataHora,
                Status = StatusLancamento.Pago,
                Categoria = "Venda",
                VendaId = venda.Id,
            });

            await db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);

            return await GetAsync(venda.Id, ct);
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<VendaResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var venda = await db.Vendas.Include(v => v.Itens)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new AppException("Venda não encontrada.", 404);

        if (venda.Status == StatusVenda.Cancelada)
            throw new AppException("Venda já está cancelada.");

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try
        {
            try { tx = await db.Database.BeginTransactionAsync(ct); } catch { /* InMemory doesn't support transactions */ }

            foreach (var item in venda.Itens)
            {
                var produto = await db.Produtos.FindAsync([item.ProdutoId], ct);
                if (produto is not null)
                {
                    produto.EstoqueAtual += item.Quantidade;
                    produto.AtualizadoEm = DateTime.UtcNow;
                }

                db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    EmpresaId = tenantContext.EmpresaId,
                    ProdutoId = item.ProdutoId,
                    Tipo = TipoMovimentacao.Entrada,
                    Quantidade = item.Quantidade,
                    Origem = OrigemMovimentacao.Venda,
                    ReferenciaId = venda.Id,
                    Observacao = "Estorno por cancelamento",
                });
            }

            var lancamento = await db.Lancamentos
                .FirstOrDefaultAsync(l => l.VendaId == venda.Id, ct);
            if (lancamento is not null)
                lancamento.Status = StatusLancamento.Cancelado;

            venda.Status = StatusVenda.Cancelada;

            await db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);

            return await GetAsync(venda.Id, ct);
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<VendaResponse> FecharAsync(Guid id, FecharVendaRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<FormaPagamento>(req.FormaPagamento, out var formaPagamento))
            throw new AppException("Forma de pagamento inválida.");

        var venda = await db.Vendas
            .Include(v => v.Itens)
            .Include(v => v.Cliente)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new AppException("Venda não encontrada.", 404);

        if (venda.Status != StatusVenda.Aberta)
            throw new AppException("Apenas vendas abertas podem ser fechadas.");

        var produtoIds = venda.Itens.Select(i => i.ProdutoId).Distinct().ToList();
        var produtos = await db.Produtos.Where(p => produtoIds.Contains(p.Id)).ToListAsync(ct);

        foreach (var item in venda.Itens)
        {
            var produto = produtos.FirstOrDefault(p => p.Id == item.ProdutoId)
                ?? throw new AppException($"Produto não encontrado.", 404);
            if (produto.Tipo == TipoProduto.Produto && produto.EstoqueAtual < item.Quantidade)
                throw new AppException(
                    $"Estoque insuficiente para '{produto.Nome}'. " +
                    $"Disponível: {produto.EstoqueAtual}, solicitado: {item.Quantidade}.");
        }

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        try
        {
            foreach (var item in venda.Itens)
            {
                var produto = produtos.First(p => p.Id == item.ProdutoId);
                produto.EstoqueAtual -= item.Quantidade;
                produto.AtualizadoEm = DateTime.UtcNow;

                db.MovimentacoesEstoque.Add(new MovimentacaoEstoque
                {
                    EmpresaId = tenantContext.EmpresaId,
                    ProdutoId = item.ProdutoId,
                    Tipo = TipoMovimentacao.Saida,
                    Quantidade = item.Quantidade,
                    Origem = OrigemMovimentacao.Venda,
                    ReferenciaId = venda.Id,
                });
            }

            var nomeCliente = venda.Cliente?.Nome ?? "Venda balcão";
            db.Lancamentos.Add(new Lancamento
            {
                EmpresaId = tenantContext.EmpresaId,
                Tipo = TipoLancamento.Receita,
                Descricao = $"Venda — {nomeCliente}",
                Valor = venda.Total,
                DataVencimento = DateTime.UtcNow,
                DataPagamento = DateTime.UtcNow,
                Status = StatusLancamento.Pago,
                Categoria = "Venda",
                VendaId = venda.Id,
            });

            venda.Status = StatusVenda.Concluida;
            venda.FormaPagamento = formaPagamento;
            venda.Parcelas = req.Parcelas;
            if (req.Observacao is not null) venda.Observacao = req.Observacao;

            await db.SaveChangesAsync(ct);
            if (tx is not null) await tx.CommitAsync(ct);

            return await GetAsync(venda.Id, ct);
        }
        catch
        {
            if (tx is not null) await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<List<VendaListItem>> ListAsync(
        DateTime? de, DateTime? ate, string? status, CancellationToken ct)
    {
        var query = db.Vendas.Include(v => v.Cliente).AsQueryable();

        if (de.HasValue) query = query.Where(v => v.DataHora >= de.Value);
        if (ate.HasValue) query = query.Where(v => v.DataHora <= ate.Value.AddDays(1));
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusVenda>(status, out var s))
            query = query.Where(v => v.Status == s);

        return await query
            .OrderByDescending(v => v.DataHora)
            .Select(v => new VendaListItem(
                v.Id, v.Cliente != null ? v.Cliente.Nome : null,
                v.DataHora, v.Status.ToString(),
                v.Total, v.FormaPagamento.ToString()))
            .ToListAsync(ct);
    }

    public async Task<VendaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var venda = await db.Vendas
            .Include(v => v.Cliente)
            .Include(v => v.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new AppException("Venda não encontrada.", 404);

        return new VendaResponse(
            venda.Id,
            venda.Cliente?.Nome,
            venda.DataHora,
            venda.Status.ToString(),
            venda.Subtotal,
            venda.Desconto,
            venda.Total,
            venda.FormaPagamento.ToString(),
            venda.Parcelas,
            venda.Observacao,
            venda.Itens.Select(i => new ItemVendaResponse(
                i.ProdutoId, i.Produto?.Nome ?? "",
                i.Quantidade, i.PrecoUnitario,
                i.Desconto, i.Total)).ToList());
    }
}
