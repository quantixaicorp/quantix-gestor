using GestorAI.API.Services.Conciliacao;
using System.Text;

namespace GestorAI.Tests.Services;

public class BankStatementParserServiceTests
{
    private readonly BankStatementParserService _svc = new();

    [Fact]
    public async Task ParseOfxAsync_ExtractsTrnAmtAndMemo()
    {
        var ofx = """
            OFXHEADER:100
            DATA:OFXSGML
            <OFX>
            <STMTTRN>
            <TRNTYPE>DEBIT
            <DTPOSTED>20250801000000[-03:BRT]
            <TRNAMT>-250.00
            <FITID>ABC123
            <MEMO>PIX ENVIADO JOAO
            </STMTTRN>
            <STMTTRN>
            <TRNTYPE>CREDIT
            <DTPOSTED>20250802120000
            <TRNAMT>1500.00
            <FITID>DEF456
            <MEMO>TED RECEBIDA
            </STMTTRN>
            </OFX>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));

        var result = await _svc.ParseOfxAsync(stream);

        Assert.Equal(2, result.Count);
        Assert.Equal(-250m, result[0].Amount);
        Assert.Equal("PIX ENVIADO JOAO", result[0].Description);
        Assert.Equal("ABC123", result[0].BankTransactionId);
        Assert.Equal(new DateOnly(2025, 8, 1), result[0].Date);
        Assert.Equal(1500m, result[1].Amount);
    }

    [Fact]
    public async Task ParseOfxAsync_SgmlFormatWithoutClosingTags()
    {
        var ofx = """
            OFXHEADER:100
            DATA:OFXSGML
            <OFX>
            <STMTTRN>
            <TRNTYPE>DEBIT
            <DTPOSTED>20250801000000[-03:BRT]
            <TRNAMT>-250.00
            <FITID>ABC123
            <MEMO>PIX ENVIADO JOAO
            <STMTTRN>
            <TRNTYPE>CREDIT
            <DTPOSTED>20250802120000
            <TRNAMT>1500.00
            <FITID>DEF456
            <MEMO>TED RECEBIDA
            </OFX>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));

        var result = await _svc.ParseOfxAsync(stream);

        Assert.Equal(2, result.Count);
        Assert.Equal(-250m, result[0].Amount);
        Assert.Equal("PIX ENVIADO JOAO", result[0].Description);
        Assert.Equal(new DateOnly(2025, 8, 1), result[0].Date);
        Assert.Equal(1500m, result[1].Amount);
    }

    [Fact]
    public async Task ParseOfxAsync_CommaDecimalSeparator()
    {
        var ofx = """
            OFXHEADER:100
            DATA:OFXSGML
            <OFX>
            <STMTTRN>
            <TRNTYPE>DEBIT
            <DTPOSTED>20250801
            <TRNAMT>-250,50
            <FITID>X1
            <MEMO>BOLETO PAGO
            </STMTTRN>
            </OFX>
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(ofx));

        var result = await _svc.ParseOfxAsync(stream);

        Assert.Single(result);
        Assert.Equal(-250.50m, result[0].Amount);
    }

    [Fact]
    public async Task ParseCsvAsync_ExtractsColumnsCorrectly()
    {
        var csv = """
            Data,Descrição,Valor
            2025-08-01,PIX RECEBIDO JOAO,250.00
            01/08/2025,PAGTO FORNECEDOR,-1500.00
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _svc.ParseCsvAsync(stream);

        Assert.Equal(2, result.Count);
        Assert.Equal(250m, result[0].Amount);
        Assert.Equal("PIX RECEBIDO JOAO", result[0].Description);
        Assert.Equal(new DateOnly(2025, 8, 1), result[0].Date);
        Assert.Equal(-1500m, result[1].Amount);
    }

    [Fact]
    public async Task ParseCsvAsync_SkipsHeaderAndEmptyLines()
    {
        var csv = "Data,Descrição,Valor\n2025-08-01,Teste,100.00\n\n";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));

        var result = await _svc.ParseCsvAsync(stream);

        Assert.Single(result);
    }
}
