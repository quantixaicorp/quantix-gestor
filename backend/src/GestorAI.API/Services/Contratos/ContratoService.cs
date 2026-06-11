using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Contratos;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Shared;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Contratos;

public class ContratoService(AppDbContext db, TenantContext tenantContext)
{
    public async Task<List<ContratoListItem>> ListAsync(string? status, CancellationToken ct)
    {
        var query = db.Contratos.Include(c => c.Cliente).AsQueryable();
        if (status != null && Enum.TryParse<ContratoStatus>(status, out var s))
            query = query.Where(c => c.Status == s);
        return await query
            .OrderByDescending(c => c.CriadoEm)
            .Select(c => ToListItem(c))
            .ToListAsync(ct);
    }

    public async Task<ContratoResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Contratos
            .Include(c => c.Cliente)
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contrato não encontrado.", 404);
        return ToResponse(c);
    }

    public async Task<ContratoResponse> CreateAsync(CreateContratoRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<TipoCobranca>(req.TipoCobranca, out var tipo))
            throw new AppException($"TipoCobranca inválido: {req.TipoCobranca}.", 400);
        if (!Enum.TryParse<Periodicidade>(req.Periodicidade, out var periodicidade))
            throw new AppException($"Periodicidade inválida: {req.Periodicidade}.", 400);
        if (req.DiaVencimento < 1 || req.DiaVencimento > 28)
            throw new AppException("DiaVencimento deve ser entre 1 e 28.", 400);

        _ = await db.Clientes.FirstOrDefaultAsync(c => c.Id == req.ClienteId, ct)
            ?? throw new AppException("Cliente não encontrado.", 404);

        var numero = (await db.Contratos.MaxAsync(c => (int?)c.Numero, ct) ?? 0) + 1;

        var contrato = new Contrato
        {
            EmpresaId = tenantContext.EmpresaId,
            Numero = numero,
            ClienteId = req.ClienteId,
            Titulo = req.Titulo,
            Objeto = req.Objeto,
            TipoCobranca = tipo,
            Valor = req.Valor,
            DataInicio = req.DataInicio,
            DataFim = req.DataFim,
            Periodicidade = periodicidade,
            DiaVencimento = req.DiaVencimento,
            Observacao = req.Observacao,
        };

        foreach (var item in req.Itens)
            contrato.Itens.Add(new ContratoItem
            {
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
            });

        db.Contratos.Add(contrato);
        await db.SaveChangesAsync(ct);
        return await GetAsync(contrato.Id, ct);
    }

    public async Task<ContratoResponse> AtivarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status != ContratoStatus.Rascunho)
            throw new AppException("Apenas rascunhos podem ser ativados.", 400);
        if (!c.Itens.Any())
            throw new AppException("Contrato precisa ter pelo menos um item.", 400);
        if (c.TipoCobranca == TipoCobranca.ParceladoPrazoFixo && c.DataFim == null)
            throw new AppException("Contratos parcelados requerem DataFim.", 400);
        c.Status = ContratoStatus.Ativo;
        await db.SaveChangesAsync(ct);
        return ToResponse(c);
    }

    public async Task<ContratoResponse> EncerrarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status != ContratoStatus.Ativo)
            throw new AppException("Apenas contratos ativos podem ser encerrados.", 400);
        c.Status = ContratoStatus.Encerrado;
        await db.SaveChangesAsync(ct);
        return ToResponse(c);
    }

    public async Task<ContratoResponse> CancelarAsync(Guid id, CancellationToken ct)
    {
        var c = await FindAsync(id, ct);
        if (c.Status == ContratoStatus.Encerrado || c.Status == ContratoStatus.Cancelado)
            throw new AppException("Contrato já está encerrado ou cancelado.", 400);
        c.Status = ContratoStatus.Cancelado;
        await db.SaveChangesAsync(ct);
        return ToResponse(c);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var c = await db.Contratos
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contrato não encontrado.", 404);

        if (c.Status != ContratoStatus.Rascunho && c.Status != ContratoStatus.Cancelado)
            throw new AppException("Apenas contratos em rascunho ou cancelados podem ser excluídos.", 400);

        var temCobrancaPaga = await db.Cobrancas
            .AnyAsync(cb => cb.ContratoId == id && cb.Status == CobrancaStatus.Pago, ct);
        if (temCobrancaPaga)
            throw new AppException("Não é possível excluir um contrato com cobranças pagas.", 400);

        db.Contratos.Remove(c);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<CobrancaListItem>> GerarCobrancasAsync(
        Guid id, GerarCobrancasRequest req, CancellationToken ct)
    {
        var contrato = await db.Contratos
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contrato não encontrado.", 404);

        if (contrato.Status != ContratoStatus.Ativo)
            throw new AppException("Apenas contratos ativos podem gerar cobranças.", 400);

        var vencimentos = CalcularVencimentos(contrato, req.De, req.Ate);
        var existentes = await db.Cobrancas
            .Where(c => c.ContratoId == id)
            .Select(c => c.DataVencimento)
            .ToListAsync(ct);

        var allVencimentos = CalcularVencimentos(contrato,
            contrato.DataInicio,
            contrato.DataFim ?? req.Ate.AddYears(10));
        var totalParcelas = allVencimentos.Count;

        var novas = new List<Cobranca>();
        foreach (var venc in vencimentos.Where(v => !existentes.Contains(v)))
        {
            var parcela = allVencimentos.IndexOf(venc) + 1;
            var referencia = contrato.TipoCobranca == TipoCobranca.ParceladoPrazoFixo
                ? $"Parcela {parcela}/{totalParcelas} — {contrato.Titulo}"
                : GerarReferenciaRecorrente(contrato, venc);

            decimal valorCobranca;
            if (contrato.TipoCobranca == TipoCobranca.ParceladoPrazoFixo)
            {
                var eachValue = Math.Round(contrato.Valor / totalParcelas, 2);
                valorCobranca = parcela == totalParcelas
                    ? contrato.Valor - eachValue * (totalParcelas - 1)
                    : eachValue;
            }
            else
            {
                valorCobranca = contrato.Valor;
            }

            var cobranca = new Cobranca
            {
                EmpresaId = tenantContext.EmpresaId,
                ClienteId = contrato.ClienteId,
                ContratoId = contrato.Id,
                Referencia = referencia,
                Valor = valorCobranca,
                DataVencimento = venc,
            };
            novas.Add(cobranca);
            db.Cobrancas.Add(cobranca);
        }

        await db.SaveChangesAsync(ct);

        return novas.Select(c => new CobrancaListItem(
            c.Id, contrato.Cliente!.Nome, c.ContratoId,
            contrato.Titulo, c.Referencia, c.Valor,
            c.DataVencimento, c.Status.ToString())).ToList();
    }

    public async Task<string> GetPdfHtmlAsync(Guid id, string apiBase, CancellationToken ct)
    {
        var c = await db.Contratos
            .Include(c => c.Cliente)
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contrato não encontrado.", 404);

        var cfg = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.EmpresaId == tenantContext.EmpresaId, ct);

        var total = c.Itens.Sum(i => i.Quantidade * i.ValorUnitario);
        var linhas = string.Join("", c.Itens.Select(i =>
            $"<tr><td>{i.Descricao}</td><td>{i.Quantidade:N2}</td>" +
            $"<td>R$ {i.ValorUnitario:N2}</td><td>R$ {i.Quantidade * i.ValorUnitario:N2}</td></tr>"));

        var corpo = $$"""
            <h1>CONTRATO {{c.Numero:D3}} — {{c.Titulo}}</h1>
            <div class="meta">
              Cliente: {{c.Cliente?.Nome}}<br>
              Início: {{c.DataInicio:dd/MM/yyyy}}{{(c.DataFim.HasValue ? $" | Término: {c.DataFim:dd/MM/yyyy}" : "")}}<br>
              Tipo: {{c.TipoCobranca}} | Periodicidade: {{c.Periodicidade}} | Valor: R$ {{c.Valor:N2}}
            </div>
            <h2 style="font-size:14px">Objeto</h2>
            <div class="objeto">{{c.Objeto}}</div>
            <h2 style="font-size:14px">Itens</h2>
            <table>
              <thead><tr><th>Descrição</th><th>Qtd</th><th>Unit.</th><th>Total</th></tr></thead>
              <tbody>{{linhas}}</tbody>
            </table>
            <div class="total">Total: R$ {{total:N2}}</div>
            <div class="assinatura">
              <div>Contratante<br>{{c.Cliente?.Nome}}</div>
              <div>Contratada<br>{{cfg?.NomeFantasia ?? cfg?.RazaoSocial ?? ""}}</div>
            </div>
            """;

        return HtmlDocumentoBase.WrapDocument($"CONTRATO {c.Numero:D3}", corpo, cfg, apiBase);
    }

    private static List<DateOnly> CalcularVencimentos(Contrato contrato, DateOnly de, DateOnly ate)
    {
        var dia = contrato.DiaVencimento;
        var daysInStartMonth = DateTime.DaysInMonth(de.Year, de.Month);
        var primeiro = new DateOnly(de.Year, de.Month, Math.Min(dia, daysInStartMonth));
        if (primeiro < de)
            primeiro = AvançarPeriodo(primeiro, contrato.Periodicidade, dia);

        var vencimentos = new List<DateOnly>();
        var cursor = primeiro;
        var limite = contrato.DataFim.HasValue && contrato.DataFim.Value < ate
            ? contrato.DataFim.Value
            : ate;

        while (cursor <= limite)
        {
            vencimentos.Add(cursor);
            cursor = AvançarPeriodo(cursor, contrato.Periodicidade, dia);
        }

        return vencimentos;
    }

    private static DateOnly AvançarPeriodo(DateOnly data, Periodicidade periodicidade, int dia)
    {
        var meses = periodicidade switch
        {
            Periodicidade.Mensal => 1,
            Periodicidade.Trimestral => 3,
            Periodicidade.Semestral => 6,
            Periodicidade.Anual => 12,
            _ => 1
        };
        var next = data.AddMonths(meses);
        return new DateOnly(next.Year, next.Month, Math.Min(dia, DateTime.DaysInMonth(next.Year, next.Month)));
    }

    private static string GerarReferenciaRecorrente(Contrato contrato, DateOnly vencimento)
    {
        var cultura = new System.Globalization.CultureInfo("pt-BR");
        var periodo = contrato.Periodicidade switch
        {
            Periodicidade.Mensal => $"Mensalidade {vencimento.ToString("MMM/yyyy", cultura)}",
            Periodicidade.Trimestral => $"Trimestral {vencimento.ToString("MMM/yyyy", cultura)}",
            Periodicidade.Semestral => $"Semestral {vencimento.ToString("MMM/yyyy", cultura)}",
            Periodicidade.Anual => $"Anuidade {vencimento.Year}",
            _ => vencimento.ToString("MMM/yyyy", cultura)
        };
        return $"{periodo} — {contrato.Titulo}";
    }

    public async Task<ContratoResponse> RenovarAsync(Guid id, CancellationToken ct)
    {
        var original = await FindAsync(id, ct);
        if (original.Status != ContratoStatus.Ativo)
            throw new AppException("Apenas contratos ativos podem ser renovados.", 400);

        var novaDataInicio = original.DataFim.HasValue
            ? original.DataFim.Value.AddDays(1)
            : DateOnly.FromDateTime(DateTime.UtcNow);

        var numero = (await db.Contratos.MaxAsync(c => (int?)c.Numero, ct) ?? 0) + 1;

        var novo = new Contrato
        {
            EmpresaId = tenantContext.EmpresaId,
            Numero = numero,
            ClienteId = original.ClienteId,
            Titulo = original.Titulo,
            Objeto = original.Objeto,
            TipoCobranca = original.TipoCobranca,
            Valor = original.Valor,
            DataInicio = novaDataInicio,
            DataFim = null,
            Periodicidade = original.Periodicidade,
            DiaVencimento = original.DiaVencimento,
            Observacao = original.Observacao,
            Status = ContratoStatus.Rascunho,
        };

        foreach (var item in original.Itens)
            novo.Itens.Add(new ContratoItem
            {
                Descricao = item.Descricao,
                Quantidade = item.Quantidade,
                ValorUnitario = item.ValorUnitario,
            });

        db.Contratos.Add(novo);
        await db.SaveChangesAsync(ct);
        return await GetAsync(novo.Id, ct);
    }

    public async Task<List<ContratoVencendoItem>> ListVencendoAsync(int dias, CancellationToken ct)
    {
        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var limite = hoje.AddDays(dias);

        return await db.Contratos
            .Include(c => c.Cliente)
            .Where(c => c.Status == ContratoStatus.Ativo
                     && c.DataFim.HasValue
                     && c.DataFim.Value >= hoje
                     && c.DataFim.Value <= limite)
            .Select(c => new ContratoVencendoItem(
                c.Id, c.Numero, c.Cliente!.Nome, c.Titulo, c.DataFim!.Value, c.Valor))
            .ToListAsync(ct);
    }

    private async Task<Contrato> FindAsync(Guid id, CancellationToken ct)
    {
        return await db.Contratos
            .Include(c => c.Cliente)
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new AppException("Contrato não encontrado.", 404);
    }

    private static ContratoListItem ToListItem(Contrato c) => new(
        c.Id, c.Numero, c.Cliente?.Nome ?? "", c.Titulo,
        c.TipoCobranca.ToString(), c.Valor, c.Status.ToString(),
        c.DataInicio, c.DataFim);

    private static ContratoResponse ToResponse(Contrato c) => new(
        c.Id, c.Numero, c.Cliente?.Nome ?? "", c.Cliente?.Whatsapp ?? "",
        c.Titulo, c.Objeto, c.TipoCobranca.ToString(), c.Valor,
        c.DataInicio, c.DataFim, c.Periodicidade.ToString(),
        c.DiaVencimento, c.Status.ToString(), c.Observacao, c.CriadoEm,
        c.Itens.Select(i => new ContratoItemResponse(i.Id, i.Descricao, i.Quantidade, i.ValorUnitario)).ToList(),
        c.Itens.Sum(i => i.Quantidade * i.ValorUnitario),
        c.ClickSignStatus,
        c.ClickSignViewerUrl);

    public async Task<EnviarAssinaturaResponse> EnviarAssinaturaAsync(
        Guid id, EnviarAssinaturaRequest req, ClickSignService clickSignService, CancellationToken ct)
    {
        var contrato = await FindAsync(id, ct);
        if (contrato.Status != ContratoStatus.Ativo)
            throw new AppException("Apenas contratos ativos podem ser enviados para assinatura.", 400);

        var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct)
            ?? throw new AppException("Configuração da empresa não encontrada.", 404);

        if (string.IsNullOrWhiteSpace(config.ClickSignApiKey))
            throw new AppException("API Key do ClickSign não configurada. Acesse Configurações → Integrações.", 400);

        var html = await GetPdfHtmlAsync(id, "", ct);
        var pdfBytes = await GerarPdfAsync(html);

        var nomeArquivo = $"contrato-{contrato.Numero:D3}.pdf";
        var docResult = await clickSignService.CriarDocumentoAsync(
            config.ClickSignApiKey, config.ClickSignSandbox, nomeArquivo, pdfBytes, ct);

        await clickSignService.AdicionarSignatarioAsync(
            config.ClickSignApiKey, config.ClickSignSandbox,
            docResult.DocKey, contrato.Cliente!.Nome, req.EmailSignatario, ct);

        contrato.ClickSignDocKey = docResult.DocKey;
        contrato.ClickSignStatus = "Pendente";
        contrato.ClickSignViewerUrl = docResult.ViewerUrl;
        await db.SaveChangesAsync(ct);

        return new EnviarAssinaturaResponse(docResult.DocKey, docResult.ViewerUrl, "Pendente");
    }

    private static async Task<byte[]> GerarPdfAsync(string html)
    {
        using var browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new PuppeteerSharp.LaunchOptions
        {
            Headless = true,
            Args = ["--no-sandbox", "--disable-setuid-sandbox"],
        });
        using var page = await browser.NewPageAsync();
        await page.SetContentAsync(html);
        return await page.PdfDataAsync(new PuppeteerSharp.PdfOptions
        {
            Format = PuppeteerSharp.Media.PaperFormat.A4,
            PrintBackground = true,
        });
    }
}
