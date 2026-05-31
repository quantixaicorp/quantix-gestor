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
        await db.NotasFiscais
            .Include(n => n.Itens)
            .OrderByDescending(n => n.CriadaEm)
            .Select(n => ToResponse(n))
            .ToListAsync(ct);

    public async Task<NotaFiscalResponse> EmitirAsync(EmitirNotaFiscalRequest req, CancellationToken ct)
    {
        var venda = await db.Vendas
            .Include(v => v.Itens)
                .ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(v => v.Id == req.VendaId, ct)
            ?? throw new AppException("Venda não encontrada", 404);

        var modelo = req.Tipo.ToLower() switch
        {
            "nfce" => ModeloNF.NFCe,
            "nfe" => ModeloNF.NFe,
            _ => throw new AppException("Tipo inválido. Use 'nfe' ou 'nfce'", 400)
        };

        var nota = new NotaFiscal
        {
            Id = Guid.NewGuid(),
            EmpresaId = tenantContext.EmpresaId,
            VendaId = req.VendaId,
            Modelo = modelo,
            Status = StatusNF.Processando,
            CriadaEm = DateTime.UtcNow,
        };

        foreach (var item in venda.Itens)
        {
            nota.Itens.Add(new NotaFiscalItem
            {
                Id = Guid.NewGuid(),
                EmpresaId = tenantContext.EmpresaId,
                NotaFiscalId = nota.Id,
                NomeProduto = item.Produto?.Nome ?? string.Empty,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.PrecoUnitario,
                Total = item.Total,
            });
        }

        db.NotasFiscais.Add(nota);
        await db.SaveChangesAsync(ct);

        return await ConsultarAsync(nota.Id, ct);
    }

    public async Task<NotaFiscalResponse> CancelarAsync(Guid id, CancelarNotaFiscalRequest req, CancellationToken ct)
    {
        var nota = await db.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new AppException("Nota fiscal não encontrada", 404);

        if (nota.Status != StatusNF.Autorizada && nota.Status != StatusNF.Processando)
            throw new AppException("Apenas notas com status Autorizada ou Processando podem ser canceladas", 400);

        nota.Status = StatusNF.Cancelada;
        nota.CanceladaEm = DateTime.UtcNow;
        nota.MensagemErro = req.Motivo;

        await db.SaveChangesAsync(ct);
        return ToResponse(nota);
    }

    public async Task<NotaFiscalResponse> ConsultarAsync(Guid id, CancellationToken ct)
    {
        var nota = await db.NotasFiscais
            .Include(n => n.Itens)
            .FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new AppException("Nota fiscal não encontrada", 404);

        return ToResponse(nota);
    }

    private static NotaFiscalResponse ToResponse(NotaFiscal n) => new(
        n.Id,
        n.VendaId,
        n.Modelo.ToString(),
        n.Numero,
        n.Serie,
        n.Status.ToString(),
        n.ChaveAcesso,
        n.Protocolo,
        n.XmlUrl,
        n.PdfUrl,
        n.MensagemErro,
        n.AutorizadaEm,
        n.CanceladaEm,
        n.CriadaEm,
        n.Itens.Select(i => new NotaFiscalItemResponse(
            i.Id,
            i.NomeProduto,
            i.Ncm,
            i.Cfop,
            i.Quantidade,
            i.PrecoUnitario,
            i.Total)).ToArray());
}
