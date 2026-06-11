namespace GestorAI.API.DTOs.Fiscal;

public record ConfiguracaoEmpresaResponse(
    Guid Id,
    string? RazaoSocial,
    string? NomeFantasia,
    string? Cnpj,
    string? InscricaoEstadual,
    string? InscricaoMunicipal,
    string? Telefone,
    string? Email,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? CodigoMunicipio,
    string? Municipio,
    string? Uf,
    string? Cep,
    int? RegimeTributario,
    int? Ambiente,
    int? SerieNfe,
    int? SerieNfce,
    bool TemToken,
    string? Slug,
    string? LogoUrl,
    string? CorPrimaria,
    string? DescricaoPublica,
    string? AsaasApiKey,
    bool AsaasSandbox,
    string? ClickSignApiKey,
    bool ClickSignSandbox,
    string? EvolutionApiUrl,
    bool TemEvolutionKey,
    string? EvolutionInstance,
    bool Lembrete3dAntes,
    bool Lembrete1dAntes,
    bool LembreteNoDia,
    bool Lembrete1dDepois,
    bool Lembrete3dDepois,
    bool Lembrete7dDepois,
    string? DominioCustomizado,
    bool AprovarAutomaticamente,
    decimal? ValorSinal,
    int? HorasLimiteCancelamento);

public record SalvarIntegracoesRequest(string? AsaasApiKey, bool AsaasSandbox, string? ClickSignApiKey, bool ClickSignSandbox);

public record SalvarWhiteLabelRequest(
    string? Slug,
    string? LogoUrl,
    string? CorPrimaria,
    string? DescricaoPublica,
    string? DominioCustomizado);

public record SalvarAutomacaoConfigRequest(
    string? EvolutionApiUrl,
    string? EvolutionApiKey,
    string? EvolutionInstance,
    bool Lembrete3dAntes,
    bool Lembrete1dAntes,
    bool LembreteNoDia,
    bool Lembrete1dDepois,
    bool Lembrete3dDepois,
    bool Lembrete7dDepois);

public record SalvarAgendamentoConfigRequest(
    bool AprovarAutomaticamente,
    decimal? ValorSinal,
    int? HorasLimiteCancelamento);

public record AtualizarConfiguracaoEmpresaRequest(
    string? RazaoSocial,
    string? NomeFantasia,
    string? Cnpj,
    string? InscricaoEstadual,
    string? InscricaoMunicipal,
    string? Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string? CodigoMunicipio,
    string? Municipio,
    string? Uf,
    string? Cep,
    int? RegimeTributario,
    string? CscId,
    string? CscToken,
    int? Ambiente,
    int? SerieNfe,
    int? SerieNfce,
    string? FocusNfeToken,
    string? Telefone = null,
    string? Email = null);
