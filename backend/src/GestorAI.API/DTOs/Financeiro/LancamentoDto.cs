namespace GestorAI.API.DTOs.Financeiro;

public record LancamentoResponse(
    Guid Id,
    string Tipo,
    string Descricao,
    decimal Valor,
    DateTime DataVencimento,
    DateTime? DataPagamento,
    string Status,
    string Categoria,
    Guid? VendaId,
    string? Observacao,
    bool Vencido,
    Guid? ParcelamentoId = null,
    int? NumeroParcela = null);

public record CreateLancamentoRequest(
    string Tipo,
    string Descricao,
    decimal Valor,
    DateTime DataVencimento,
    string Categoria,
    string? Observacao);

public record PagarLancamentoRequest(DateTime DataPagamento);

public record FluxoCaixaItemResponse(DateTime Data, decimal Receitas, decimal Despesas, decimal Saldo);

public record FluxoCaixaResponse(
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal SaldoFinal,
    List<FluxoCaixaItemResponse> Itens);

public record LancamentoResumo(
    decimal TotalReceitasMes,
    decimal TotalDespesasMes,
    decimal SaldoMes,
    decimal TotalPendente);

public record UpdateLancamentoRequest(
    string Tipo,
    string Descricao,
    decimal Valor,
    DateTime DataVencimento,
    string Categoria,
    string? Observacao);
