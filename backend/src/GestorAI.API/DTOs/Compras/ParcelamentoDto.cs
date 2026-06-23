namespace GestorAI.API.DTOs.Compras;

public record ParcelaResponse(
    Guid Id,
    int NumeroParcela,
    decimal Valor,
    DateTime DataVencimento,
    DateTime? DataPagamento,
    string Status,
    bool Vencido);

public record ParcelamentoDetalheResponse(
    Guid Id,
    Guid? CompraId,
    string Descricao,
    decimal ValorTotal,
    int QtdParcelas,
    string Status,
    string Categoria,
    List<ParcelaResponse> Parcelas);
