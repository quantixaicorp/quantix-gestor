using GestorAI.API.Domain.Entities;
using GestorAI.API.Services.Shared;

namespace GestorAI.Tests.Services;

public class HtmlDocumentoBaseTests
{
    [Fact]
    public void WrapDocument_ComCorPrimaria_ContemCorNoCabecalho()
    {
        var cfg = new ConfiguracaoEmpresa
        {
            NomeFantasia = "Barbearia do João",
            CorPrimaria = "#ff5500",
        };

        var html = HtmlDocumentoBase.WrapDocument("Titulo", "<p>corpo</p>", cfg, "");

        Assert.Contains("#ff5500", html);
    }

    [Fact]
    public void WrapDocument_SemCfg_UsaCorFallback()
    {
        var html = HtmlDocumentoBase.WrapDocument("Titulo", "<p>corpo</p>", null, "");

        Assert.Contains("#2563eb", html);
        Assert.DoesNotContain("NullReference", html);
    }

    [Fact]
    public void WrapDocument_ComEndereco_ContemRodape()
    {
        var cfg = new ConfiguracaoEmpresa
        {
            Logradouro = "Rua das Flores", Numero = "10",
            Municipio = "São Paulo", Uf = "SP", Cep = "01310-100"
        };

        var html = HtmlDocumentoBase.WrapDocument("T", "<p>corpo</p>", cfg, "");

        Assert.Contains("Rua das Flores", html);
        Assert.Contains("São Paulo/SP", html);
    }

    [Fact]
    public void WrapDocument_SemEndereco_SemRodape()
    {
        var cfg = new ConfiguracaoEmpresa { NomeFantasia = "Empresa" };

        var html = HtmlDocumentoBase.WrapDocument("T", "<p>corpo</p>", cfg, "");

        Assert.DoesNotContain("footer", html);
    }

    [Fact]
    public void WrapDocument_ComLogo_ContemImgTag()
    {
        var cfg = new ConfiguracaoEmpresa
        {
            LogoUrl = "/uploads/logos/logo.png",
            NomeFantasia = "Empresa X",
        };

        var html = HtmlDocumentoBase.WrapDocument("T", "<p>corpo</p>", cfg, "https://api.gestorai.com.br");

        Assert.Contains("https://api.gestorai.com.br/uploads/logos/logo.png", html);
    }
}
