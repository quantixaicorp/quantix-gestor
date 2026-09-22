using GestorAI.API.DTOs.Fornecedores;
using GestorAI.API.Services.Fornecedores;

namespace GestorAI.Tests.Services;

public class FornecedorValidatorTests
{
    [Theory]
    [InlineData("12345678901")]
    [InlineData("12ABC34501DE35")]
    [InlineData("12abc34501de35")]
    public void Validate_AceitaCpfNumericoOuCnpjAlfanumerico(string documento)
    {
        var result = new CreateFornecedorValidator().Validate(Request(documento));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_RejeitaLetraNosDigitosVerificadores()
    {
        var result = new CreateFornecedorValidator().Validate(Request("12ABC34501DE3A"));

        Assert.False(result.IsValid);
    }

    private static CreateFornecedorRequest Request(string documento) => new(
        Name: "Fornecedor",
        CnpjCpf: documento,
        Phone: null,
        Email: null,
        Logradouro: null,
        City: null,
        Uf: null,
        Cep: null,
        ContactPerson: null,
        Notes: null);
}
