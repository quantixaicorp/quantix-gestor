// backend/src/GestorAI.API/Services/Orcamentos/OrcamentoService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.DTOs.Orcamentos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Shared;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Orcamentos;

public class OrcamentoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<OrcamentoListItem>> ListAsync(string? status, CancellationToken ct)
    {
        var orcamentos = await db.Quotes
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        await ExpireIfNeededAsync(orcamentos, ct);

        return orcamentos
            .Where(o => status == null || o.Status.ToString() == status)
            .Select(o => ToListItem(o))
            .ToList();
    }

    public async Task<OrcamentoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Quotes
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        await ExpireIfNeededAsync([o], ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> CreateAsync(CreateOrcamentoRequest req, CancellationToken ct)
    {
        var numero = (await db.Quotes.MaxAsync(o => (int?)o.Number, ct) ?? 0) + 1;

        var orcamento = new Quote
        {
            CompanyId = tenantContext.CompanyId,
            CustomerId = req.CustomerId,
            Number = numero,
            Title = req.Title,
            ExpirationDate = req.ExpirationDate,
            Status = OrcamentoStatus.Rascunho,
            Notes = req.Notes,
        };

        foreach (var item in req.Items)
        {
            if (!Enum.TryParse<OrcamentoItemTipo>(item.Type, out var tipo))
                throw new AppException($"Type de item inválido: {item.Type}.");

            if (tipo == OrcamentoItemTipo.Produto && item.ProductId == null)
                throw new AppException($"Item '{item.Description}' do tipo Product requer ProductId.");

            orcamento.Items.Add(new QuoteItem
            {
                Type = tipo,
                ProductId = item.ProductId,
                Description = item.Description,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
            });
        }

        db.Quotes.Add(orcamento);
        await db.SaveChangesAsync(ct);

        return await GetAsync(orcamento.Id, ct);
    }

    public async Task<OrcamentoResponse> EnviarAsync(Guid id, CancellationToken ct)
    {
        var o = await FindAsync(id, ct);
        if (o.Status != OrcamentoStatus.Rascunho)
            throw new AppException("Apenas rascunhos podem ser enviados.");
        o.Status = OrcamentoStatus.Enviado;
        o.PublicToken = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> AprovarAsync(Guid id, CancellationToken ct)
    {
        var o = await FindAsync(id, ct);
        if (o.ExpirationDate.Date < DateTime.UtcNow.Date)
            throw new AppException("Orçamento expirado não pode ser aprovado.", 400);
        if (o.Status != OrcamentoStatus.Enviado)
            throw new AppException("Apenas orçamentos enviados podem ser aprovados.");
        o.Status = OrcamentoStatus.Aprovado;
        await db.SaveChangesAsync(ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> RejeitarAsync(Guid id, CancellationToken ct)
    {
        var o = await FindAsync(id, ct);
        if (o.Status != OrcamentoStatus.Enviado)
            throw new AppException("Apenas orçamentos enviados podem ser rejeitados.");
        o.Status = OrcamentoStatus.Rejeitado;
        await db.SaveChangesAsync(ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var o = await FindAsync(id, ct);
        if (o.Status != OrcamentoStatus.Rascunho)
            throw new AppException("Apenas rascunhos podem ser cancelados.");
        o.Status = OrcamentoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> ConvertAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Quotes
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        if (o.Status != OrcamentoStatus.Aprovado)
            throw new AppException("Apenas orçamentos aprovados podem ser convertidos.", 400);

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        var itensProduto = o.Items
            .Where(i => i.Type == OrcamentoItemTipo.Produto)
            .ToList();

        var subtotal = o.Items.Sum(i => i.Quantity * i.UnitPrice);

        var venda = new Sale
        {
            CompanyId = tenantContext.CompanyId,
            CustomerId = o.CustomerId,
            Status = StatusVenda.Concluida,
            Subtotal = subtotal,
            Discount = 0,
            Total = subtotal,
            PaymentMethod = FormaPagamento.Outro,
            Notes = $"Gerado do Orçamento ORC-{o.Number:D3}",
        };
        db.Sales.Add(venda);

        foreach (var item in itensProduto)
        {
            db.SaleItems.Add(new SaleItem
            {
                SaleId = venda.Id,
                ProductId = item.ProductId!.Value,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Discount = 0,
                Total = item.Quantity * item.UnitPrice,
            });

            var produto = await db.Products.FindAsync([item.ProductId!.Value], ct);
            if (produto is not null)
            {
                produto.CurrentStock -= item.Quantity;
                produto.UpdatedAt = DateTime.UtcNow;

                db.StockMovements.Add(new StockMovement
                {
                    CompanyId = tenantContext.CompanyId,
                    ProductId = item.ProductId!.Value,
                    Type = TipoMovimentacao.Saida,
                    Quantity = item.Quantity,
                    Source = OrigemMovimentacao.Venda,
                    ReferenceId = venda.Id,
                });
            }
        }

        var nomeCliente = o.CustomerId.HasValue
            ? (await db.Customers.FindAsync([o.CustomerId.Value], ct))?.Name ?? "Customer"
            : "Sale balcão";

        db.Transactions.Add(new Transaction
        {
            CompanyId = tenantContext.CompanyId,
            Type = TipoLancamento.Receita,
            Description = $"Sale — {nomeCliente} (ORC-{o.Number:D3})",
            Amount = subtotal,
            DueDate = DateTime.UtcNow,
            PaymentDate = DateTime.UtcNow,
            Status = StatusLancamento.Pago,
            Category = "Sale",
            SaleId = venda.Id,
        });

        o.SaleId = venda.Id;
        o.Status = OrcamentoStatus.Convertido;

        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return ToResponse(o);
    }

    public async Task<string> GetPdfHtmlAsync(Guid id, string apiBase, CancellationToken ct)
    {
        var o = await db.Quotes
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        await ExpireIfNeededAsync([o], ct);

        var cfg = await db.CompanySettings
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CompanyId == tenantContext.CompanyId, ct);

        var total = o.Items.Sum(i => i.Quantity * i.UnitPrice);
        var linhas = string.Join("", o.Items.Select(i =>
            $"<tr><td>{i.Description}</td><td>{i.Quantity:N2}</td>" +
            $"<td>R$ {i.UnitPrice:N2}</td><td>R$ {i.Quantity * i.UnitPrice:N2}</td></tr>"));
        var clienteHtml = o.Customer != null ? $"Customer: {o.Customer.Name}<br>" : "";
        var obsHtml = o.Notes != null ? $"<div class='obs'>Obs: {o.Notes}</div>" : "";

        var corpo = $$"""
            <h1>ORC-{{o.Number:D3}} — {{o.Title}}</h1>
            <div class="meta">
              {{clienteHtml}}
              Válido até: {{o.ExpirationDate:dd/MM/yyyy}} | Status: {{o.Status}}
            </div>
            <table>
              <thead><tr><th>Descrição</th><th>Qtd</th><th>Unit.</th><th>Total</th></tr></thead>
              <tbody>{{linhas}}</tbody>
            </table>
            <div class="total">Total: R$ {{total:N2}}</div>
            {{obsHtml}}
            """;

        return HtmlDocumentoBase.WrapDocument($"ORC-{o.Number:D3}", corpo, cfg, apiBase);
    }

    public async Task<OrcamentoPublicoResponse> GetPublicoAsync(Guid token, CancellationToken ct)
    {
        var o = await db.Quotes
            .IgnoreQueryFilters()
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PublicToken == token, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);
        await ExpireIfNeededAsync([o], ct);
        return ToPublicoResponse(o);
    }

    public async Task<OrcamentoPublicoResponse> AprovarPublicoAsync(Guid token, CancellationToken ct)
    {
        var o = await db.Quotes
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PublicToken == token, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);
        if (o.ExpirationDate.Date < DateTime.UtcNow.Date)
            throw new AppException("Orçamento expirado.", 400);
        if (o.Status != OrcamentoStatus.Enviado)
            throw new AppException("Orçamento não está disponível para aprovação.", 400);
        o.Status = OrcamentoStatus.Aprovado;
        await db.SaveChangesAsync(ct);
        return ToPublicoResponse(o);
    }

    public async Task<OrcamentoPublicoResponse> RejeitarPublicoAsync(Guid token, CancellationToken ct)
    {
        var o = await db.Quotes
            .IgnoreQueryFilters()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PublicToken == token, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);
        if (o.Status != OrcamentoStatus.Enviado)
            throw new AppException("Orçamento não está disponível para rejeição.", 400);
        o.Status = OrcamentoStatus.Rejeitado;
        await db.SaveChangesAsync(ct);
        return ToPublicoResponse(o);
    }

    public async Task<CobrancaResponse> GerarCobrancaAsync(
        Guid id, DateOnly dataVencimento, CancellationToken ct)
    {
        var orc = await db.Quotes
            .Include(o => o.Items)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        if (orc.Status != OrcamentoStatus.Aprovado && orc.Status != OrcamentoStatus.Enviado)
            throw new AppException("Apenas orçamentos Aprovados ou Enviados podem gerar cobrança.", 400);

        if (orc.CustomerId is null)
            throw new AppException("Orçamento sem cliente vinculado.", 400);

        var total = orc.Items.Sum(i => i.Quantity * i.UnitPrice);
        var referencia = $"Orçamento ORC-{orc.Number:D3} — {orc.Title}";

        var cobranca = new Charge
        {
            CompanyId = tenantContext.CompanyId,
            CustomerId = orc.CustomerId.Value,
            Reference = referencia,
            Amount = total,
            DueDate = dataVencimento,
        };
        db.Charges.Add(cobranca);
        await db.SaveChangesAsync(ct);

        var created = await db.Charges
            .Include(c => c.Customer)
            .FirstAsync(c => c.Id == cobranca.Id, ct);

        return new CobrancaResponse(
            created.Id, created.Customer!.Name, created.Customer.WhatsApp ?? "",
            null, null,
            created.Reference, created.Amount, created.DueDate,
            null, created.Status.ToString(), null, null, created.CreatedAt);
    }

    private async Task<Quote> FindAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Quotes
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);
        return o;
    }

    private async Task ExpireIfNeededAsync(List<Quote> orcamentos, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var expirar = orcamentos
            .Where(o => o.ExpirationDate.Date < hoje
                && (o.Status == OrcamentoStatus.Enviado || o.Status == OrcamentoStatus.Aprovado))
            .ToList();
        if (expirar.Count == 0) return;
        foreach (var o in expirar) o.Status = OrcamentoStatus.Expirado;
        await db.SaveChangesAsync(ct);
    }

    private static OrcamentoListItem ToListItem(Quote o) => new(
        o.Id, o.Number, o.Title, o.Customer?.Name,
        o.ExpirationDate, o.Status.ToString(),
        o.Items.Sum(i => i.Quantity * i.UnitPrice));

    private static OrcamentoPublicoResponse ToPublicoResponse(Quote o) => new(
        o.Title,
        o.Customer?.Name,
        o.ExpirationDate,
        o.Status.ToString(),
        o.Notes,
        o.Items.Select(i => new OrcamentoItemPublicoResponse(
            i.Description, i.Quantity, i.UnitPrice,
            i.Quantity * i.UnitPrice)).ToList(),
        o.Items.Sum(i => i.Quantity * i.UnitPrice));

    private static OrcamentoResponse ToResponse(Quote o) => new(
        o.Id, o.Number, o.Title, o.CustomerId,
        o.Customer?.Name, o.Customer?.WhatsApp,
        o.ExpirationDate, o.Status.ToString(),
        o.Notes, o.SaleId, o.PublicToken, o.CreatedAt,
        o.Items.Select(i => new OrcamentoItemResponse(
            i.Id, i.Type.ToString(), i.ProductId,
            i.Description, i.Quantity, i.UnitPrice)).ToList(),
        o.Items.Sum(i => i.Quantity * i.UnitPrice));
}
