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
        if (!Enum.TryParse<FormaPagamento>(req.PaymentMethod, out var formaPagamento))
            throw new AppException("Forma de pagamento inválida.");

        var produtoIds = req.Items.Select(i => i.ProductId).Distinct().ToList();
        var produtos = await db.Products
            .Where(p => produtoIds.Contains(p.Id))
            .ToListAsync(ct);

        foreach (var item in req.Items)
        {
            var produto = produtos.FirstOrDefault(p => p.Id == item.ProductId)
                ?? throw new AppException($"Product {item.ProductId} não encontrado.", 404);
            if (produto.Type == TipoProduto.Produto && produto.CurrentStock < item.Quantity)
                throw new AppException(
                    $"Estoque insuficiente para '{produto.Name}'. " +
                    $"Disponível: {produto.CurrentStock}, solicitado: {item.Quantity}.");
        }

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try
        {
            try { tx = await db.Database.BeginTransactionAsync(ct); } catch { /* InMemory doesn't support transactions */ }

            var itensEntidade = req.Items.Select(item =>
            {
                var produto = produtos.First(p => p.Id == item.ProductId);
                var total = produto.SalePrice * item.Quantity - item.Discount;
                return new SaleItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = produto.SalePrice,
                    Discount = item.Discount,
                    Total = total,
                };
            }).ToList();

            var subtotal = itensEntidade.Sum(i => i.UnitPrice * i.Quantity);
            if (req.Discount > subtotal)
                throw new AppException("Discount não pode exceder o subtotal.");

            var total = subtotal - req.Discount;

            var dataHora = req.SaleDate.HasValue
                ? DateTime.SpecifyKind(req.SaleDate.Value, DateTimeKind.Unspecified)
                : DateTime.UtcNow;

            var venda = new Sale
            {
                CompanyId = tenantContext.CompanyId,
                CustomerId = req.CustomerId,
                SaleDate = dataHora,
                Status = StatusVenda.Concluida,
                Subtotal = subtotal,
                Discount = req.Discount,
                Total = total,
                PaymentMethod = formaPagamento,
                Installments = req.Installments,
                Notes = req.Notes,
                ProfessionalId   = req.ProfessionalId,
                ServiceOrderNotes     = req.ServiceOrderNotes,
            };
            db.Sales.Add(venda);

            foreach (var item in itensEntidade)
            {
                item.SaleId = venda.Id;
                db.SaleItems.Add(item);
            }

            foreach (var item in req.Items)
            {
                var produto = produtos.First(p => p.Id == item.ProductId);
                produto.CurrentStock -= item.Quantity;
                produto.UpdatedAt = DateTime.UtcNow;

                db.StockMovements.Add(new StockMovement
                {
                    CompanyId = tenantContext.CompanyId,
                    ProductId = item.ProductId,
                    Type = TipoMovimentacao.Saida,
                    Quantity = item.Quantity,
                    Source = OrigemMovimentacao.Venda,
                    ReferenceId = venda.Id,
                });
            }

            var nomeCliente = req.CustomerId.HasValue
                ? (await db.Customers.FindAsync([req.CustomerId.Value], ct))?.Name ?? "Customer"
                : "Sale balcão";

            string? nomeProfissional = null;
            if (req.ProfessionalId.HasValue)
                nomeProfissional = (await db.Professionals.FindAsync([req.ProfessionalId.Value], ct))?.Name;
            venda.ProfessionalName = nomeProfissional;

            db.Transactions.Add(new Transaction
            {
                CompanyId = tenantContext.CompanyId,
                Type = TipoLancamento.Receita,
                Description = $"Sale — {nomeCliente}",
                Amount = total,
                DueDate = venda.SaleDate,
                PaymentDate = venda.SaleDate,
                Status = StatusLancamento.Pago,
                Category = "Sale",
                SaleId = venda.Id,
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
        var venda = await db.Sales.Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new AppException("Sale não encontrada.", 404);

        if (venda.Status == StatusVenda.Cancelada)
            throw new AppException("Sale já está cancelada.");

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try
        {
            try { tx = await db.Database.BeginTransactionAsync(ct); } catch { /* InMemory doesn't support transactions */ }

            foreach (var item in venda.Items)
            {
                var produto = await db.Products.FindAsync([item.ProductId], ct);
                if (produto is not null)
                {
                    produto.CurrentStock += item.Quantity;
                    produto.UpdatedAt = DateTime.UtcNow;
                }

                db.StockMovements.Add(new StockMovement
                {
                    CompanyId = tenantContext.CompanyId,
                    ProductId = item.ProductId,
                    Type = TipoMovimentacao.Entrada,
                    Quantity = item.Quantity,
                    Source = OrigemMovimentacao.Venda,
                    ReferenceId = venda.Id,
                    Notes = "Estorno por cancelamento",
                });
            }

            var lancamento = await db.Transactions
                .FirstOrDefaultAsync(l => l.SaleId == venda.Id, ct);
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
        if (!Enum.TryParse<FormaPagamento>(req.PaymentMethod, out var formaPagamento))
            throw new AppException("Forma de pagamento inválida.");

        var venda = await db.Sales
            .Include(v => v.Items)
            .Include(v => v.Customer)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new AppException("Sale não encontrada.", 404);

        if (venda.Status != StatusVenda.Aberta)
            throw new AppException("Apenas vendas abertas podem ser fechadas.");

        var produtoIds = venda.Items.Select(i => i.ProductId).Distinct().ToList();
        var produtos = await db.Products.Where(p => produtoIds.Contains(p.Id)).ToListAsync(ct);

        foreach (var item in venda.Items)
        {
            var produto = produtos.FirstOrDefault(p => p.Id == item.ProductId)
                ?? throw new AppException($"Product não encontrado.", 404);
            if (produto.Type == TipoProduto.Produto && produto.CurrentStock < item.Quantity)
                throw new AppException(
                    $"Estoque insuficiente para '{produto.Name}'. " +
                    $"Disponível: {produto.CurrentStock}, solicitado: {item.Quantity}.");
        }

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        try
        {
            foreach (var item in venda.Items)
            {
                var produto = produtos.First(p => p.Id == item.ProductId);
                produto.CurrentStock -= item.Quantity;
                produto.UpdatedAt = DateTime.UtcNow;

                db.StockMovements.Add(new StockMovement
                {
                    CompanyId = tenantContext.CompanyId,
                    ProductId = item.ProductId,
                    Type = TipoMovimentacao.Saida,
                    Quantity = item.Quantity,
                    Source = OrigemMovimentacao.Venda,
                    ReferenceId = venda.Id,
                });
            }

            var nomeCliente = venda.Customer?.Name ?? "Sale balcão";
            db.Transactions.Add(new Transaction
            {
                CompanyId = tenantContext.CompanyId,
                Type = TipoLancamento.Receita,
                Description = $"Sale — {nomeCliente}",
                Amount = venda.Total,
                DueDate = DateTime.UtcNow,
                PaymentDate = DateTime.UtcNow,
                Status = StatusLancamento.Pago,
                Category = "Sale",
                SaleId = venda.Id,
            });

            venda.Status = StatusVenda.Concluida;
            venda.PaymentMethod = formaPagamento;
            venda.Installments = req.Installments;
            if (req.Notes is not null) venda.Notes = req.Notes;

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
        var query = db.Sales.Include(v => v.Customer).AsQueryable();

        if (de.HasValue) query = query.Where(v => v.SaleDate >= de.Value);
        if (ate.HasValue) query = query.Where(v => v.SaleDate <= ate.Value.AddDays(1));
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<StatusVenda>(status, out var s))
            query = query.Where(v => v.Status == s);

        return await query
            .OrderByDescending(v => v.SaleDate)
            .Select(v => new VendaListItem(
                v.Id, v.CustomerId, v.Customer != null ? v.Customer.Name : null,
                v.SaleDate, v.Status.ToString(),
                v.Total, v.PaymentMethod.ToString(),
                v.ProfessionalName))
            .ToListAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var venda = await db.Sales
            .Include(v => v.Transaction)
            .Include(v => v.Items)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new AppException("Sale não encontrada.", 404);

        foreach (var item in venda.Items)
        {
            var produto = await db.Products.FindAsync([item.ProductId], ct);
            if (produto is not null)
                produto.CurrentStock += item.Quantity;
        }

        if (venda.Transaction is not null)
            db.Transactions.Remove(venda.Transaction);

        db.SaleItems.RemoveRange(venda.Items);
        db.Sales.Remove(venda);
        await db.SaveChangesAsync(ct);
    }

    public async Task<VendaResponse> UpdateAsync(Guid id, UpdateVendaRequest req, CancellationToken ct)
    {
        var venda = await db.Sales
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new AppException("Sale não encontrada.", 404);

        if (venda.Status != StatusVenda.Concluida)
            throw new AppException("Apenas vendas concluídas podem ser editadas.", 400);

        if (!Enum.TryParse<FormaPagamento>(req.PaymentMethod, out var forma))
            throw new AppException($"FormaPagamento inválida: {req.PaymentMethod}.", 400);

        venda.CustomerId = req.CustomerId;
        venda.PaymentMethod = forma;
        venda.SaleDate = req.SaleDate;

        var lancamento = await db.Transactions
            .FirstOrDefaultAsync(l => l.SaleId == id, ct);
        if (lancamento is not null)
        {
            var nomeCliente = req.CustomerId.HasValue
                ? (await db.Customers.FindAsync([req.CustomerId.Value], ct))?.Name ?? "Customer"
                : "Sale balcão";
            lancamento.Description = $"Sale — {nomeCliente}";
            lancamento.DueDate = req.SaleDate;
            lancamento.PaymentDate = req.SaleDate;
        }

        await db.SaveChangesAsync(ct);
        return await GetAsync(id, ct);
    }

    public async Task<VendaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var venda = await db.Sales
            .Include(v => v.Customer)
            .Include(v => v.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(v => v.Id == id, ct)
            ?? throw new AppException("Sale não encontrada.", 404);

        return new VendaResponse(
            venda.Id,
            venda.CustomerId,
            venda.Customer?.Name,
            venda.SaleDate,
            venda.Status.ToString(),
            venda.Subtotal,
            venda.Discount,
            venda.Total,
            venda.PaymentMethod.ToString(),
            venda.Installments,
            venda.Notes,
            venda.Items.Select(i => new ItemVendaResponse(
                i.ProductId, i.Product?.Name ?? "",
                i.Quantity, i.UnitPrice,
                i.Discount, i.Total)).ToList(),
            venda.ProfessionalName,
            venda.ServiceOrderNotes);
    }
}
