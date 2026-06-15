using GestorAI.API.DTOs.Compras;

namespace GestorAI.API.Services.Compras;

public static class VencimentoCalculator
{
    public static List<(DateTime DataVencimento, decimal Valor)> Calcular(
        string condicaoPagamento,
        DateTime dataCompra,
        decimal valorTotal,
        int? qtdParcelas,
        List<ParcelaPreviewRequest>? parcelasPersonalizadas)
    {
        return condicaoPagamento switch
        {
            "AVista" => [(dataCompra, valorTotal)],
            "30d" => [(dataCompra.AddDays(30), valorTotal)],
            "30_60_90d" => GerarParcelas(dataCompra, valorTotal, 3, 30),
            "Parcelado" => GerarParcelas(dataCompra, valorTotal, qtdParcelas ?? 1, 30),
            "Personalizado" => parcelasPersonalizadas?
                .OrderBy(p => p.Numero)
                .Select(p => (p.DataVencimento, p.Valor))
                .ToList() ?? [(dataCompra, valorTotal)],
            _ => [(dataCompra, valorTotal)],
        };
    }

    private static List<(DateTime, decimal)> GerarParcelas(
        DateTime dataBase, decimal valorTotal, int n, int intervaloDias)
    {
        var valorParcela = Math.Round(valorTotal / n, 2);
        var parcelas = new List<(DateTime, decimal)>();
        var acumulado = 0m;

        for (var i = 1; i <= n; i++)
        {
            var valor = i == n ? valorTotal - acumulado : valorParcela;
            parcelas.Add((dataBase.AddDays(intervaloDias * i), valor));
            acumulado += valorParcela;
        }

        return parcelas;
    }
}
