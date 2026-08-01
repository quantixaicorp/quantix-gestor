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
        var config = await db.CompanySettings
            .FirstOrDefaultAsync(ct);

        if (config is null)
        {
            config = new CompanySettings
            {
                Id = Guid.NewGuid(),
                CompanyId = tenantContext.CompanyId,
                Ambiente = 2,
            };
            db.CompanySettings.Add(config);
            await db.SaveChangesAsync(ct);
        }

        return ToResponse(config);
    }

    public async Task<ConfiguracaoEmpresaResponse> AtualizarAsync(
        AtualizarConfiguracaoEmpresaRequest req, CancellationToken ct)
    {
        var config = await db.CompanySettings.FirstOrDefaultAsync(ct);

        if (config is null)
        {
            config = new CompanySettings
            {
                Id = Guid.NewGuid(),
                CompanyId = tenantContext.CompanyId,
                Ambiente = 2,
            };
            db.CompanySettings.Add(config);
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
        if (req.Phone is not null) config.Phone = req.Phone;
        if (req.Email is not null) config.Email = req.Email;
        if (req.TipoNegocio is not null) config.TipoNegocio = req.TipoNegocio;

        await db.SaveChangesAsync(ct);
        return ToResponse(config);
    }

    private static ConfiguracaoEmpresaResponse ToResponse(CompanySettings c) => new(
        c.Id,
        c.RazaoSocial,
        c.NomeFantasia,
        c.Cnpj,
        c.InscricaoEstadual,
        c.InscricaoMunicipal,
        c.Phone,
        c.Email,
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
        c.PrimaryColor,
        c.PublicDescription,
        c.AsaasApiKey,
        c.AsaasSandbox,
        c.ClickSignApiKey,
        c.ClickSignSandbox,
        c.EvolutionApiUrl,
        c.EvolutionApiKey is not null,
        c.EvolutionInstance,
        c.Reminder3DaysBefore,
        c.Reminder1DayBefore,
        c.ReminderOnDueDate,
        c.Reminder1DayAfter,
        c.Reminder3DaysAfter,
        c.Reminder7DaysAfter,
        c.CustomDomain,
        c.AutoApprove,
        c.DepositAmount,
        c.CancellationLimitHours,
        c.TipoNegocio);

    public async Task<ConfiguracaoEmpresaResponse> SalvarBrandingAsync(
        ConfigurarBrandingRequest req, CancellationToken ct)
    {
        var slugEmUso = await db.CompanySettings
            .IgnoreQueryFilters()
            .AnyAsync(c => c.Slug == req.Slug && c.CompanyId != tenantContext.CompanyId, ct);
        if (slugEmUso)
            throw new AppException("Este slug já está em uso.", 400);

        var config = await db.CompanySettings.FirstOrDefaultAsync(ct)
            ?? new CompanySettings { CompanyId = tenantContext.CompanyId };

        var isNew = config.Id == Guid.Empty;
        config.Slug = req.Slug;
        if (req.NomeExibicao is not null) config.NomeFantasia = req.NomeExibicao;
        config.PrimaryColor = req.PrimaryColor;
        config.PublicDescription = req.PublicDescription;

        if (isNew) db.CompanySettings.Add(config);
        await db.SaveChangesAsync(ct);
        return await ObterAsync(ct);
    }

    public async Task SalvarAgendamentoAsync(SalvarAgendamentoConfigRequest req, CancellationToken ct)
    {
        var existing = await db.CompanySettings.FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            existing = new CompanySettings { Id = Guid.NewGuid(), CompanyId = tenantContext.CompanyId };
            db.CompanySettings.Add(existing);
        }
        existing.AutoApprove = req.AutoApprove;
        existing.DepositAmount = req.DepositAmount;
        existing.CancellationLimitHours = req.CancellationLimitHours;
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarIntegracoesAsync(SalvarIntegracoesRequest req, CancellationToken ct)
    {
        var config = await db.CompanySettings.FirstOrDefaultAsync(ct)
            ?? new CompanySettings { CompanyId = tenantContext.CompanyId };
        var isNew = config.Id == Guid.Empty;
        config.AsaasApiKey = req.AsaasApiKey;
        config.AsaasSandbox = req.AsaasSandbox;
        config.ClickSignApiKey = req.ClickSignApiKey;
        config.ClickSignSandbox = req.ClickSignSandbox;
        if (isNew) db.CompanySettings.Add(config);
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarWhiteLabelAsync(SalvarWhiteLabelRequest req, CancellationToken ct)
    {
        var config = await db.CompanySettings.FirstOrDefaultAsync(ct)
            ?? new CompanySettings { CompanyId = tenantContext.CompanyId };
        var isNew = config.Id == Guid.Empty;
        if (req.Slug is not null) config.Slug = req.Slug;
        if (req.LogoUrl is not null) config.LogoUrl = req.LogoUrl;
        if (req.PrimaryColor is not null) config.PrimaryColor = req.PrimaryColor;
        if (req.PublicDescription is not null) config.PublicDescription = req.PublicDescription;
        config.CustomDomain = req.CustomDomain;
        if (isNew) db.CompanySettings.Add(config);
        await db.SaveChangesAsync(ct);
    }

    public async Task SalvarAutomacaoConfigAsync(SalvarAutomacaoConfigRequest req, CancellationToken ct)
    {
        var config = await db.CompanySettings.FirstOrDefaultAsync(ct)
            ?? new CompanySettings { CompanyId = tenantContext.CompanyId };
        var isNew = config.Id == Guid.Empty;
        config.EvolutionApiUrl = req.EvolutionApiUrl;
        if (!string.IsNullOrWhiteSpace(req.EvolutionApiKey)) config.EvolutionApiKey = req.EvolutionApiKey;
        config.EvolutionInstance = req.EvolutionInstance;
        config.Reminder3DaysBefore = req.Reminder3DaysBefore;
        config.Reminder1DayBefore = req.Reminder1DayBefore;
        config.ReminderOnDueDate = req.ReminderOnDueDate;
        config.Reminder1DayAfter = req.Reminder1DayAfter;
        config.Reminder3DaysAfter = req.Reminder3DaysAfter;
        config.Reminder7DaysAfter = req.Reminder7DaysAfter;
        if (isNew) db.CompanySettings.Add(config);
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

        var dir = Path.Combine(env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot"), "logos");
        Directory.CreateDirectory(dir);

        var fileName = $"{tenantContext.CompanyId}{ext}";
        var fullPath = Path.Combine(dir, fileName);

        await using var stream = File.Create(fullPath);
        await file.CopyToAsync(stream, ct);

        var logoUrl = $"/logos/{fileName}";

        var config = await db.CompanySettings.FirstOrDefaultAsync(ct)
            ?? new CompanySettings { CompanyId = tenantContext.CompanyId };

        var isNew = config.Id == Guid.Empty;
        config.LogoUrl = logoUrl;
        if (isNew) db.CompanySettings.Add(config);
        await db.SaveChangesAsync(ct);

        return logoUrl;
    }
}
