using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Fiscal;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Fiscal;

public class NotaFiscalService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<NotaFiscalResponse>> ListAsync(CancellationToken ct) =>
        await db.Invoices
            .Include(n => n.Items)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => ToResponse(n))
            .ToListAsync(ct);

    public async Task<NotaFiscalResponse> EmitirAsync(EmitirNotaFiscalRequest req, CancellationToken ct)
    {
        var venda = await db.Sales
            .Include(v => v.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(v => v.Id == req.SaleId, ct)
            ?? throw new AppException("Sale não encontrada", 404);

        var modelo = req.Type.ToLower() switch
        {
            "nfce" => ModeloNF.NFCe,
            "nfe" => ModeloNF.NFe,
            _ => throw new AppException("Type inválido. Use 'nfe' ou 'nfce'", 400)
        };

        var nota = new Invoice
        {
            Id = Guid.NewGuid(),
            CompanyId = tenantContext.CompanyId,
            SaleId = req.SaleId,
            Model = modelo,
            Status = StatusNF.Processando,
            CreatedAt = DateTime.UtcNow,
        };

        foreach (var item in venda.Items)
        {
            nota.Items.Add(new InvoiceItem
            {
                Id = Guid.NewGuid(),
                CompanyId = tenantContext.CompanyId,
                InvoiceId = nota.Id,
                ProductName = item.Product?.Name ?? string.Empty,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Total = item.Total,
            });
        }

        db.Invoices.Add(nota);
        await db.SaveChangesAsync(ct);

        return await ConsultarAsync(nota.Id, ct);
    }

    public async Task<NotaFiscalResponse> CancelarAsync(Guid id, CancelarNotaFiscalRequest req, CancellationToken ct)
    {
        var nota = await db.Invoices
            .Include(n => n.Items)
            .FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new AppException("Nota fiscal não encontrada", 404);

        if (nota.Status != StatusNF.Autorizada && nota.Status != StatusNF.Processando)
            throw new AppException("Apenas notas com status Autorizada ou Processando podem ser canceladas", 400);

        nota.Status = StatusNF.Cancelada;
        nota.CanceledAt = DateTime.UtcNow;
        nota.ErrorMessage = req.Reason;

        await db.SaveChangesAsync(ct);
        return ToResponse(nota);
    }

    public async Task<NotaFiscalResponse> ConsultarAsync(Guid id, CancellationToken ct)
    {
        var nota = await db.Invoices
            .Include(n => n.Items)
            .FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new AppException("Nota fiscal não encontrada", 404);

        return ToResponse(nota);
    }

    private static NotaFiscalResponse ToResponse(Invoice n) => new(
        n.Id,
        n.SaleId,
        n.Model.ToString(),
        n.Number,
        n.Series,
        n.Status.ToString(),
        n.AccessKey,
        n.Protocol,
        n.XmlUrl,
        n.PdfUrl,
        n.ErrorMessage,
        n.AuthorizedAt,
        n.CanceledAt,
        n.CreatedAt,
        n.Items.Select(i => new NotaFiscalItemResponse(
            i.Id,
            i.ProductName,
            i.Ncm,
            i.Cfop,
            i.Quantity,
            i.UnitPrice,
            i.Total)).ToArray());
}
