namespace GestorAI.API.Domain.Entities;

public class CompanySettings : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Cnpj { get; set; }
    public string? Phone { get; set; }
    public string? Email    { get; set; }
    public string? InscricaoEstadual { get; set; }
    public string? InscricaoMunicipal { get; set; }
    public string? Logradouro { get; set; }
    public string? Numero { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? CodigoMunicipio { get; set; }
    public string? Municipio { get; set; }
    public string? Uf { get; set; }
    public string? Cep { get; set; }
    public int? RegimeTributario { get; set; }
    public string? CscId { get; set; }
    public string? CscToken { get; set; }
    public int? Ambiente { get; set; }
    public int? SerieNfe { get; set; }
    public int? SerieNfce { get; set; }
    public string? FocusNfeToken { get; set; }

    // Public branding
    public string? Slug { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? PublicDescription { get; set; }
    public string? AsaasApiKey { get; set; }
    public bool AsaasSandbox { get; set; } = true;
    // ClickSign (digital signature)
    public string? ClickSignApiKey { get; set; }
    public bool ClickSignSandbox { get; set; } = true;

    // Evolution API (WhatsApp)
    public string? EvolutionApiUrl { get; set; }
    public string? EvolutionApiKey { get; set; }
    public string? EvolutionInstance { get; set; }

    // Charge reminders
    public bool Reminder3DaysBefore { get; set; }
    public bool Reminder1DayBefore  { get; set; }
    public bool ReminderOnDueDate   { get; set; }
    public bool Reminder1DayAfter   { get; set; }
    public bool Reminder3DaysAfter  { get; set; }
    public bool Reminder7DaysAfter  { get; set; }

    // Advanced scheduling
    public bool AutoApprove { get; set; } = true;
    public decimal? DepositAmount { get; set; }
    public int? CancellationLimitHours { get; set; }

    // White label — custom domain
    public string? CustomDomain { get; set; }

    public string TipoNegocio { get; set; } = "Lojista";
}
