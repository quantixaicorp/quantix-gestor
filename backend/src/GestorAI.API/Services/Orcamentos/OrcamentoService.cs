// backend/src/GestorAI.API/Services/Orcamentos/OrcamentoService.cs
using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.DTOs.Orcamentos;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Orcamentos;

public class OrcamentoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<OrcamentoListItem>> ListAsync(string? status, CancellationToken ct)
    {
        var orcamentos = await db.Orcamentos
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .OrderByDescending(o => o.CriadoEm)
            .ToListAsync(ct);

        await ExpireIfNeededAsync(orcamentos, ct);

        return orcamentos
            .Where(o => status == null || o.Status.ToString() == status)
            .Select(o => ToListItem(o))
            .ToList();
    }

    public async Task<OrcamentoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        await ExpireIfNeededAsync([o], ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> CreateAsync(CreateOrcamentoRequest req, CancellationToken ct)
    {
        var numero = (await db.Orcamentos.MaxAsync(o => (int?)o.Numero, ct) ?? 0) + 1;

        var orcamento = new Orcamento
        {
            EmpresaId = tenantContext.EmpresaId,
            ClienteId = req.ClienteId,
            Numero = numero,
            Titulo = req.Titulo,
            DataValidade = req.DataValidade,
            Status = OrcamentoStatus.Rascunho,
            Observacao = req.Observacao,
        };

        foreach (var item in req.Itens)
        {
            if (!Enum.TryParse<OrcamentoItemTipo>(item.Tipo, out var tipo))
                throw new AppException($"Tipo de item inválido: {item.Tipo}.");

            if (tipo == OrcamentoItemTipo.Produto && item.ProdutoId == null)
                throw new AppException($"Item '{item.Descricao}' do tipo Produto requer ProdutoId.");

            orcamento.Itens.Add(new OrcamentoItem
            {
                Tipo = tipo,
                ProdutoId = item.ProdutoId,
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
            });
        }

        db.Orcamentos.Add(orcamento);
        await db.SaveChangesAsync(ct);

        return await GetAsync(orcamento.Id, ct);
    }

    public async Task<OrcamentoResponse> EnviarAsync(Guid id, CancellationToken ct)
    {
        var o = await FindAsync(id, ct);
        if (o.Status != OrcamentoStatus.Rascunho)
            throw new AppException("Apenas rascunhos podem ser enviados.");
        o.Status = OrcamentoStatus.Enviado;
        o.TokenPublico = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
        return ToResponse(o);
    }

    public async Task<OrcamentoResponse> AprovarAsync(Guid id, CancellationToken ct)
    {
        var o = await FindAsync(id, ct);
        if (o.DataValidade.Date < DateTime.UtcNow.Date)
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
        var o = await db.Orcamentos
            .Include(o => o.Itens)
            .Include(o => o.Cliente)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        if (o.Status != OrcamentoStatus.Aprovado)
            throw new AppException("Apenas orçamentos aprovados podem ser convertidos.", 400);

        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? tx = null;
        try { tx = await db.Database.BeginTransactionAsync(ct); } catch { }

        var itensProduto = o.Itens
            .Where(i => i.Tipo == OrcamentoItemTipo.Produto)
            .ToList();

        var subtotal = o.Itens.Sum(i => i.Quantidade * i.ValorUnitario);

        var venda = new Venda
        {
            EmpresaId = tenantContext.EmpresaId,
            ClienteId = o.ClienteId,
            Status = StatusVenda.Aberta,
            Subtotal = subtotal,
            Desconto = 0,
            Total = subtotal,
            FormaPagamento = FormaPagamento.Outro,
            Observacao = $"Gerado do Orçamento ORC-{o.Numero:D3}",
        };
        db.Vendas.Add(venda);

        foreach (var item in itensProduto)
        {
            db.ItensVenda.Add(new ItemVenda
            {
                VendaId = venda.Id,
                ProdutoId = item.ProdutoId!.Value,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.ValorUnitario,
                Desconto = 0,
                Total = item.Quantidade * item.ValorUnitario,
            });
        }

        o.VendaId = venda.Id;
        o.Status = OrcamentoStatus.Convertido;

        await db.SaveChangesAsync(ct);
        if (tx is not null) await tx.CommitAsync(ct);

        return ToResponse(o);
    }

    public async Task<string> GetPdfHtmlAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        await ExpireIfNeededAsync([o], ct);

        var total = o.Itens.Sum(i => i.Quantidade * i.ValorUnitario);
        var linhas = string.Join("", o.Itens.Select(i =>
            $"<tr><td>{i.Descricao}</td><td>{i.Quantidade:N2}</td>" +
            $"<td>R$ {i.ValorUnitario:N2}</td><td>R$ {i.Quantidade * i.ValorUnitario:N2}</td></tr>"));
        var clienteHtml = o.Cliente != null ? $"Cliente: {o.Cliente.Nome}<br>" : "";
        var obsHtml = o.Observacao != null ? $"<div class='obs'>Obs: {o.Observacao}</div>" : "";

        return $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head><meta charset="UTF-8">
            <style>
              body { font-family: sans-serif; padding: 32px; color: #111; }
              h1 { font-size: 22px; margin-bottom: 4px; }
              .meta { color: #555; font-size: 13px; margin-bottom: 24px; }
              table { width: 100%; border-collapse: collapse; margin-bottom: 16px; }
              th, td { border: 1px solid #ddd; padding: 8px; text-align: left; font-size: 13px; }
              th { background: #f5f5f5; }
              .total { text-align: right; font-weight: bold; font-size: 15px; margin-top: 8px; }
              .obs { margin-top: 16px; font-size: 13px; color: #555; }
            </style>
            </head>
            <body>
              <h1>ORC-{{o.Numero:D3}} — {{o.Titulo}}</h1>
              <div class="meta">
                {{clienteHtml}}
                Válido até: {{o.DataValidade:dd/MM/yyyy}} | Status: {{o.Status}}
              </div>
              <table>
                <thead><tr><th>Descrição</th><th>Qtd</th><th>Unit.</th><th>Total</th></tr></thead>
                <tbody>{{linhas}}</tbody>
              </table>
              <div class="total">Total: R$ {{total:N2}}</div>
              {{obsHtml}}
            </body>
            </html>
            """;
    }

    public async Task<OrcamentoPublicoResponse> GetPublicoAsync(Guid token, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .IgnoreQueryFilters()
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.TokenPublico == token, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);
        await ExpireIfNeededAsync([o], ct);
        return ToPublicoResponse(o);
    }

    public async Task<OrcamentoPublicoResponse> AprovarPublicoAsync(Guid token, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .IgnoreQueryFilters()
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.TokenPublico == token, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);
        if (o.DataValidade.Date < DateTime.UtcNow.Date)
            throw new AppException("Orçamento expirado.", 400);
        if (o.Status != OrcamentoStatus.Enviado)
            throw new AppException("Orçamento não está disponível para aprovação.", 400);
        o.Status = OrcamentoStatus.Aprovado;
        await db.SaveChangesAsync(ct);
        return ToPublicoResponse(o);
    }

    public async Task<OrcamentoPublicoResponse> RejeitarPublicoAsync(Guid token, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .IgnoreQueryFilters()
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.TokenPublico == token, ct)
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
        var orc = await db.Orcamentos
            .Include(o => o.Itens)
            .Include(o => o.Cliente)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);

        if (orc.Status != OrcamentoStatus.Aprovado && orc.Status != OrcamentoStatus.Enviado)
            throw new AppException("Apenas orçamentos Aprovados ou Enviados podem gerar cobrança.", 400);

        if (orc.ClienteId is null)
            throw new AppException("Orçamento sem cliente vinculado.", 400);

        var total = orc.Itens.Sum(i => i.Quantidade * i.ValorUnitario);
        var referencia = $"Orçamento ORC-{orc.Numero:D3} — {orc.Titulo}";

        var cobranca = new Cobranca
        {
            EmpresaId = tenantContext.EmpresaId,
            ClienteId = orc.ClienteId.Value,
            Referencia = referencia,
            Valor = total,
            DataVencimento = dataVencimento,
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync(ct);

        var created = await db.Cobrancas
            .Include(c => c.Cliente)
            .FirstAsync(c => c.Id == cobranca.Id, ct);

        return new CobrancaResponse(
            created.Id, created.Cliente!.Nome, created.Cliente.Whatsapp ?? "",
            null, null,
            created.Referencia, created.Valor, created.DataVencimento,
            null, created.Status.ToString(), null, null, created.CriadoEm);
    }

    private async Task<Orcamento> FindAsync(Guid id, CancellationToken ct)
    {
        var o = await db.Orcamentos
            .Include(o => o.Cliente)
            .Include(o => o.Itens)
            .FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new AppException("Orçamento não encontrado.", 404);
        return o;
    }

    private async Task ExpireIfNeededAsync(List<Orcamento> orcamentos, CancellationToken ct)
    {
        var hoje = DateTime.UtcNow.Date;
        var expirar = orcamentos
            .Where(o => o.DataValidade.Date < hoje
                && (o.Status == OrcamentoStatus.Enviado || o.Status == OrcamentoStatus.Aprovado))
            .ToList();
        if (expirar.Count == 0) return;
        foreach (var o in expirar) o.Status = OrcamentoStatus.Expirado;
        await db.SaveChangesAsync(ct);
    }

    private static OrcamentoListItem ToListItem(Orcamento o) => new(
        o.Id, o.Numero, o.Titulo, o.Cliente?.Nome,
        o.DataValidade, o.Status.ToString(),
        o.Itens.Sum(i => i.Quantidade * i.ValorUnitario));

    private static OrcamentoPublicoResponse ToPublicoResponse(Orcamento o) => new(
        o.Titulo,
        o.Cliente?.Nome,
        o.DataValidade,
        o.Status.ToString(),
        o.Observacao,
        o.Itens.Select(i => new OrcamentoItemPublicoResponse(
            i.Descricao, i.Quantidade, i.ValorUnitario,
            i.Quantidade * i.ValorUnitario)).ToList(),
        o.Itens.Sum(i => i.Quantidade * i.ValorUnitario));

    private static OrcamentoResponse ToResponse(Orcamento o) => new(
        o.Id, o.Numero, o.Titulo, o.ClienteId,
        o.Cliente?.Nome, o.Cliente?.Whatsapp,
        o.DataValidade, o.Status.ToString(),
        o.Observacao, o.VendaId, o.TokenPublico, o.CriadoEm,
        o.Itens.Select(i => new OrcamentoItemResponse(
            i.Id, i.Tipo.ToString(), i.ProdutoId,
            i.Descricao, i.Quantidade, i.ValorUnitario)).ToList(),
        o.Itens.Sum(i => i.Quantidade * i.ValorUnitario));
}
