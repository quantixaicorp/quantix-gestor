using System.Text.RegularExpressions;

namespace GestorAI.API.Services.Clientes;

/// <summary>CPF/CNPJ opcional do cliente: tipo de pessoa, normalização e formato do documento.</summary>
public static partial class ClienteDocumento
{
    public const string PessoaFisica = "PF";
    public const string PessoaJuridica = "PJ";

    public const string TipoInvalido = "Tipo de pessoa deve ser PF ou PJ";
    public const string TipoObrigatorio = "Informe se o documento é CPF (PF) ou CNPJ (PJ)";
    public const string CpfInvalido = "CPF deve ter 11 dígitos";
    public const string CnpjInvalido = "CNPJ deve ter 12 caracteres alfanuméricos e 2 dígitos verificadores";

    [GeneratedRegex("[^A-Za-z0-9]")]
    private static partial Regex NaoAlfanumerico { get; }

    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex Cpf { get; }

    [GeneratedRegex(@"^[A-Z0-9]{12}\d{2}$")]
    private static partial Regex Cnpj { get; }

    public static bool IsTipoValido(string? personType) =>
        personType is PessoaFisica or PessoaJuridica;

    /// <summary>Valida o formato do documento já sem máscara, conforme o tipo de pessoa.</summary>
    public static bool IsDocumentoValido(string? personType, string? cnpjCpf)
    {
        var (_, documento) = Normalize(personType, cnpjCpf);
        if (documento is null) return true;

        return personType == PessoaFisica ? Cpf.IsMatch(documento) : Cnpj.IsMatch(documento);
    }

    /// <summary>Remove máscara e normaliza; documento vazio zera também o tipo de pessoa.</summary>
    public static (string? PersonType, string? CnpjCpf) Normalize(string? personType, string? cnpjCpf)
    {
        var documento = cnpjCpf is null
            ? null
            : NaoAlfanumerico.Replace(cnpjCpf, "").ToUpperInvariant();

        return string.IsNullOrEmpty(documento)
            ? (null, null)
            : (personType, documento);
    }
}
