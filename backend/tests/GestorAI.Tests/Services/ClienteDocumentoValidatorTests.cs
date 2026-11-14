using GestorAI.API.DTOs.Clientes;
using GestorAI.API.Services.Clientes;

namespace GestorAI.Tests.Services;

public class ClienteDocumentoValidatorTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("PF", "")]
    [InlineData("PF", "12345678909")]
    [InlineData("PF", "123.456.789-09")]
    [InlineData("PJ", "11222333000181")]
    [InlineData("PJ", "12.ABC.345/01DE-35")]
    [InlineData("PJ", "12abc34501de35")]
    public void Validate_AceitaDocumentoOpcionalComOuSemMascara(string? tipo, string? documento)
    {
        var result = new CreateClienteValidator().Validate(Request(tipo, documento));

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
    }

    [Theory]
    [InlineData("PF", "1234567890")]
    [InlineData("PF", "12ABC34501DE35")]
    [InlineData("PJ", "12345678901")]
    [InlineData("PJ", "12ABC34501DE3A")]
    public void Validate_RejeitaDocumentoForaDoFormatoDoTipo(string tipo, string documento)
    {
        var result = new CreateClienteValidator().Validate(Request(tipo, documento));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_RejeitaTipoDePessoaDesconhecido()
    {
        var result = new CreateClienteValidator().Validate(Request("MEI", "12345678909"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_ExigeTipoDePessoaQuandoDocumentoInformado()
    {
        var result = new CreateClienteValidator().Validate(Request(null, "12345678909"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == ClienteDocumento.TipoObrigatorio);
    }

    private static CreateClienteRequest Request(string? tipo, string? documento) => new(
        Name: "Cliente",
        WhatsApp: "11999990001",
        Email: null,
        Notes: null,
        PersonType: tipo,
        CnpjCpf: documento);
}
