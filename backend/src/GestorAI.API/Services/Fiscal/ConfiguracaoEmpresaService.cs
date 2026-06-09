using GestorAI.API.Domain.Entities;
using GestorAI.API.DTOs.Fiscal;
using GestorAI.API.DTOs.PublicBooking;
using GestorAI.API.Infrastructure.Data;
using GestorAI.API.Shared.Exceptions;
using GestorAI.API.Shared.MultiTenancy;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace GestorAI.API.Services.Fiscal;

public class ConfiguracaoEmpresaService(AppDbContext db, TenantContext tenantContext, IWebHostEnvironment env)
{
    public async Task<ConfiguracaoEmpresaResponse> ObterAsync(CancellationToken ct)
    {
        var config = await db.ConfiguracoesEmpresa
            .FirstOrDefaultAsync(ct);

        if (config is null)
        {
            config = new ConfiguracaoEmpresa
            {
                Id = Guid.NewGuid(),
                EmpresaId = tenantContext.EmpresaId,
                Ambiente = 2,
            };
            db.ConfiguracoesEmpresa.Add(config);
            await db.SaveChangesAsync(ct);
        }

        return ToResponse(config);
    }

    public async Task<ConfiguracaoEmpresaResponse> AtualizarAsync(
        AtualizarConfiguracaoEmpresaRequest req, CancellationToken ct)
    {
        var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct);

        if (config is null)
        {
            config = new ConfiguracaoEmpresa
            {
                Id = Guid.NewGuid(),
                EmpresaId = tenantContext.EmpresaId,
                Ambiente = 2,
            };
            db.ConfiguracoesEmpresa.Add(config);
        }

        if (req.RazaoSocial is not null) config.RazaoSocial = req.RazaoSocial;
        if (req.NomeFantasia is not null) config.NomeFantasia = req.NomeFantasia;
        if (req.Cnpj is not null) config.Cnpj = req.Cnpj;
        if (req.InscricaoEstadual is not null) config.InscricaoEstadual = req.InscricaoEstadual;
        if (req.InscricaoMunicipal is not null) config.InscricaoMunicipal = req.InscricaoMunicipal;
        if (req.Logradouro is not null) config.Logradouro = req.Logradouro;
        if (req.Numero is not null) config.Numero = req.Numero;
        if (req.Complemento is not null) config.Complemento = req.Complemento;
        if (req.Bairro is not null) config.Bairro = req.Bairro;
        if (req.CodigoMunicipio is not null) config.CodigoMunicipio = req.CodigoMunicipio;
        if (req.Municipio is not null) config.Municipio = req.Municipio;
        if (req.Uf is not null) config.Uf = req.Uf;
        if (req.Cep is not null) config.Cep = req.Cep;
        if (req.RegimeTributario is not null) config.RegimeTributario = req.RegimeTributario;
        if (req.CscId is not null) config.CscId = req.CscId;
        if (req.CscToken is not null) config.CscToken = req.CscToken;
        if (req.Ambiente is not null) config.Ambiente = req.Ambiente;
        if (req.SerieNfe is not null) config.SerieNfe = req.SerieNfe;
        if (req.SerieNfce is not null) config.SerieNfce = req.SerieNfce;
        if (req.FocusNfeToken is not null) config.FocusNfeToken = req.FocusNfeToken;

        await db.SaveChangesAsync(ct);
        return ToResponse(config);
    }

    private static ConfiguracaoEmpresaResponse ToResponse(ConfiguracaoEmpresa c) => new(
        c.Id,
        c.RazaoSocial,
        c.NomeFantasia,
        c.Cnpj,
        c.InscricaoEstadual,
        c.InscricaoMunicipal,
        c.Logradouro,
        c.Numero,
        c.Complemento,
        c.Bairro,
        c.CodigoMunicipio,
        c.Municipio,
        c.Uf,
        c.Cep,
        c.RegimeTributario,
        c.Ambiente,
        c.SerieNfe,
        c.SerieNfce,
        c.FocusNfeToken is not null,
        c.Slug,
        c.LogoUrl,
        c.CorPrimaria,
        c.DescricaoPublica,
        c.AsaasApiKey,
        c.AsaasSandbox,
        c.EvolutionApiUrl,
        c.EvolutionApiKey is not null,
        c.EvolutionInstance,
        c.Lembrete3dAntes,
        c.Lembrete1dAntes,
        c.LembreteNoDia,
        c.Lembrete1dDepois,
        c.Lembrete3dDepois,
        c.Lembrete7dDepois);

    public async Task<ConfiguracaoEmpresaResponse> SalvarBrandingAsync(
        ConfigurarBrandingRequest req, CancellationToken ct)
    {
        var slugEmUso = await db.ConfiguracoesEmpresa
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Slug == req.Slug && c.EmpresaId != tenantContext.EmpresaId, ct);
        if (slugEmUso)
            throw new AppException("Este slug já está em uso.", 400);

        var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct)
            ?? new ConfiguracaoEmpresa { EmpresaId = tenantContext.EmpresaId };

        var isNew = config.Id == Guid.Empty;
        config.Slug = req.Slug;
        if (req.NomeExibicao is not null) config.NomeFantasia = req.NomeExibicao;
        config.CorPrimaria = req.CorPrimaria;
        config.DescricaoPublica = req.DescricaoPublica;

        if (isNew) db.ConfiguracoesEmpresa.Add(config);
        await db.SaveChangesAsync(ct);
        return await ObterAsync(ct);
    }

    public async Task SalvarIntegracoesAsync(SalvarIntegracoesRequest req, CancellationToken ct)
    {
        var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct)
            ?? new ConfiguracaoEmpresa { EmpresaId = tenantContext.EmpresaId };
        var isNew = config.Id == Guid.Empty;
        config.AsaasApiKey = req.AsaasApiKey;
        config.AsaasSandbox = req.AsaasSandbox;
        if (isNew) db.ConfiguracoesEmpresa.Add(config);
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarAutomacaoConfigAsync(SalvarAutomacaoConfigRequest req, CancellationToken ct)
    {
        var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct)
            ?? new ConfiguracaoEmpresa { EmpresaId = tenantContext.EmpresaId };
        var isNew = config.Id == Guid.Empty;
        config.EvolutionApiUrl = req.EvolutionApiUrl;
        if (req.EvolutionApiKey is not null) config.EvolutionApiKey = req.EvolutionApiKey;
        config.EvolutionInstance = req.EvolutionInstance;
        config.Lembrete3dAntes = req.Lembrete3dAntes;
        config.Lembrete1dAntes = req.Lembrete1dAntes;
        config.LembreteNoDia = req.LembreteNoDia;
        config.Lembrete1dDepois = req.Lembrete1dDepois;
        config.Lembrete3dDepois = req.Lembrete3dDepois;
        config.Lembrete7dDepois = req.Lembrete7dDepois;
        if (isNew) db.ConfiguracoesEmpresa.Add(config);
        await db.SaveChangesAsync(ct);
    }

    public async Task<string> UploadLogoAsync(IFormFile file, CancellationToken ct)
    {
        var extensoesPermitidas = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!extensoesPermitidas.Contains(ext))
            throw new AppException("Formato inválido. Use jpg, png ou webp.", 400);
        if (file.Length > 2 * 1024 * 1024)
            throw new AppException("Arquivo muito grande. Máximo 2MB.", 400);

        var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "uploads", "logos");
        Directory.CreateDirectory(dir);

        var fileName = $"{tenantContext.EmpresaId}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);

        var logoUrl = $"/uploads/logos/{fileName}";

        var config = await db.ConfiguracoesEmpresa.FirstOrDefaultAsync(ct)
            ?? new ConfiguracaoEmpresa { EmpresaId = tenantContext.EmpresaId };

        var isNew = config.Id == Guid.Empty;
        config.LogoUrl = logoUrl;
        if (isNew) db.ConfiguracoesEmpresa.Add(config);
        await db.SaveChangesAsync(ct);

        return logoUrl;
    }
}
