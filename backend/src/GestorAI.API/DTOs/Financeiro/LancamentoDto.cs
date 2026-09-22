namespace GestorAI.API.DTOs.Financeiro;

public record LancamentoResponse(
    Guid Id,
    string Type,
    string Description,
    decimal Amount,
    DateTime DueDate,
    DateTime? PaymentDate,
    string Status,
    string Category,
    Guid? SaleId,
    string? Notes,
    bool Vencido,
    Guid? InstallmentPlanId = null,
    int? InstallmentNumber = null);

public record CreateLancamentoRequest(
    string Type,
    string Description,
    decimal Amount,
    DateTime DueDate,
    string Category,
    string? Notes);

public record PagarLancamentoRequest(DateTime PaymentDate);

public record FluxoCaixaItemResponse(DateTime Data, decimal Receitas, decimal Despesas, decimal Saldo);

public record FluxoCaixaResponse(
    decimal TotalReceitas,
    decimal TotalDespesas,
    decimal SaldoFinal,
    List<FluxoCaixaItemResponse> Items);

public record LancamentoResumo(
    decimal TotalReceitasMes,
    decimal TotalDespesasMes,
    decimal SaldoMes,
    decimal TotalPendente);

public record UpdateLancamentoRequest(
    string Type,
    string Description,
    decimal Amount,
    DateTime DueDate,
    string Category,
    string? Notes);

public record CreateParceladoRequest(
    string Type,
    string Description,
    string Category,
    string? Notes,
    List<ParcelaItemRequest> Installments);

public record ParcelaItemRequest(decimal Amount, DateTime DueDate);
