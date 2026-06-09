namespace GestorAI.API.Domain.Entities;

public class ConfiguracaoEmpresa : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid EmpresaId { get; set; }
    public string? RazaoSocial { get; set; }
    public string? NomeFantasia { get; set; }
    public string? Cnpj { get; set; }
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

    // Branding público
    public string? Slug { get; set; }
    public string? LogoUrl { get; set; }
    public string? CorPrimaria { get; set; }
    public string? DescricaoPublica { get; set; }

    // Evolution API (WhatsApp)
    public string? EvolutionApiUrl { get; set; }
    public string? EvolutionApiKey { get; set; }
    public string? EvolutionInstance { get; set; }

    // Lembretes de cobrança
    public bool Lembrete3dAntes  { get; set; }
    public bool Lembrete1dAntes  { get; set; }
    public bool LembreteNoDia    { get; set; }
    public bool Lembrete1dDepois { get; set; }
    public bool Lembrete3dDepois { get; set; }
    public bool Lembrete7dDepois { get; set; }
}
