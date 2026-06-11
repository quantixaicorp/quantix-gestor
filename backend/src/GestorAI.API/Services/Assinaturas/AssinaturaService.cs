using GestorAI.API.Domain.Entities;
using GestorAI.API.Domain.Enums;
using GestorAI.API.DTOs.Assinaturas;
using GestorAI.API.DTOs.Cobrancas;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Services.Cobrancas;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Assinaturas;

public class AssinaturaService(
    AppDbContext db,
    TenantContext tenantContext,
    CobrancaService cobrancaService)
{
    public async Task<List<AssinaturaListItem>> ListAsync(Guid? planoId, string? status, CancellationToken ct)
    {
        var query = db.AssinaturasCliente
            .Include(a => a.Cliente)
            .Include(a => a.Plano)
            .AsQueryable();

        if (planoId.HasValue)
            query = query.Where(a => a.PlanoAssinaturaId == planoId.Value);

        if (status != null && Enum.TryParse<AssinaturaStatus>(status, out var s))
            query = query.Where(a => a.Status == s);

        return await query
            .OrderByDescending(a => a.CriadoEm)
            .Select(a => new AssinaturaListItem(
                a.Id, a.Cliente!.Nome, a.Plano!.Nome,
                a.Status.ToString(), a.DataRenovacao, a.CicloAtual))
            .ToListAsync(ct);
    }

    public async Task<AssinaturaResponse> GetAsync(Guid id, CancellationToken ct)
    {
        var a = await db.AssinaturasCliente
            .Include(x => x.Cliente)
            .Include(x => x.Plano)
            .FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new AppException("Assinatura não encontrada.", 404);

        return new AssinaturaResponse(
            a.Id, a.Cliente!.Nome, a.Cliente.Whatsapp,
            a.PlanoAssinaturaId, a.Plano!.Nome, a.Plano.Preco,
            a.Status.ToString(), a.DataInicio, a.DataRenovacao,
            a.CicloAtual, a.ContratoId);
    }

    public async Task<AssinarResponse> AssinarAsync(
        Guid empresaId, Guid planoId, AssinarRequest req, CancellationToken ct)
    {
        tenantContext.EmpresaId = empresaId;

        var (assinaturaId, contratoId, cobrancaId) = await AssinarSemAsaasAsync(empresaId, planoId, req, ct);

        var asaasResult = await cobrancaService.EnviarAsaasAsync(
            cobrancaId, new EnviarAsaasRequest("PIX"), ct);

        var cobranca = await db.Cobrancas.FindAsync([cobrancaId], ct);
        return new AssinarResponse(
            assinaturaId, contratoId, cobrancaId,
            asaasResult.PixQrCode, asaasResult.BoletoUrl,
            cobranca!.Valor,
            cobranca.DataVencimento);
    }

    public async Task<(Guid AssinaturaId, Guid ContratoId, Guid CobrancaId)> AssinarSemAsaasAsync(
        Guid empresaId, Guid planoId, AssinarRequest req, CancellationToken ct)
    {
        tenantContext.EmpresaId = empresaId;

        var plano = await db.PlanosAssinatura.Include(p => p.Itens)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == planoId && p.EmpresaId == empresaId && p.Ativo, ct)
            ?? throw new AppException("Plano não encontrado.", 404);

        var cliente = await db.Clientes.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.EmpresaId == empresaId && c.Whatsapp == req.Whatsapp, ct);
        if (cliente is null)
        {
            cliente = new Cliente { EmpresaId = empresaId, Nome = req.Nome, Whatsapp = req.Whatsapp, Email = req.Email };
            db.Clientes.Add(cliente);
            await db.SaveChangesAsync(ct);
        }

        var numero = (await db.Contratos.IgnoreQueryFilters()
            .Where(c => c.EmpresaId == empresaId)
            .MaxAsync(c => (int?)c.Numero, ct) ?? 0) + 1;

        var objeto = string.Join(", ", plano.Itens.Select(i =>
            i.QuantidadePorCiclo == 0 ? $"{i.Descricao} (ilimitado)" : $"{i.QuantidadePorCiclo}x {i.Descricao}"));

        var contrato = new Contrato
        {
            EmpresaId = empresaId,
            Numero = numero,
            ClienteId = cliente.Id,
            Titulo = $"Assinatura — {plano.Nome}",
            Objeto = objeto,
            TipoCobranca = TipoCobranca.Recorrente,
            Valor = plano.Preco,
            DataInicio = DateOnly.FromDateTime(DateTime.UtcNow),
            Periodicidade = plano.Periodicidade,
            DiaVencimento = DateTime.UtcNow.Day,
            Status = ContratoStatus.Ativo,
        };
        contrato.Itens.Add(new ContratoItem
        {
            Descricao = plano.Nome,
            Quantidade = 1,
            ValorUnitario = plano.Preco,
        });
        db.Contratos.Add(contrato);
        await db.SaveChangesAsync(ct);

        var hoje = DateOnly.FromDateTime(DateTime.UtcNow);
        var assinatura = new AssinaturaCliente
        {
            EmpresaId = empresaId,
            ClienteId = cliente.Id,
            PlanoAssinaturaId = plano.Id,
            ContratoId = contrato.Id,
            DataInicio = hoje,
            DataRenovacao = hoje.AddMonths(1),
        };
        db.AssinaturasCliente.Add(assinatura);

        contrato.AssinaturaClienteId = assinatura.Id;

        var cobranca = new Cobranca
        {
            EmpresaId = empresaId,
            ClienteId = cliente.Id,
            ContratoId = contrato.Id,
            Referencia = $"Assinatura {plano.Nome} — {hoje:MMMM/yyyy}",
            Valor = plano.Preco,
            DataVencimento = hoje.AddDays(3),
        };
        db.Cobrancas.Add(cobranca);
        await db.SaveChangesAsync(ct);

        return (assinatura.Id, contrato.Id, cobranca.Id);
    }

    public async Task CancelarAsync(Guid id, CancellationToken ct)
    {
        var assinatura = await db.AssinaturasCliente
            .FirstOrDefaultAsync(a => a.Id == id, ct)
            ?? throw new AppException("Assinatura não encontrada.", 404);

        assinatura.Status = AssinaturaStatus.Cancelada;

        var contrato = await db.Contratos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == assinatura.ContratoId, ct);
        if (contrato != null && contrato.Status == ContratoStatus.Ativo)
            contrato.Status = ContratoStatus.Encerrado;

        var cobrancasPendentes = await db.Cobrancas.IgnoreQueryFilters()
            .Where(c => c.ContratoId == assinatura.ContratoId && c.Status == CobrancaStatus.Pendente)
            .ToListAsync(ct);
        foreach (var cob in cobrancasPendentes)
            cob.Status = CobrancaStatus.Cancelado;

        await db.SaveChangesAsync(ct);
    }
}
