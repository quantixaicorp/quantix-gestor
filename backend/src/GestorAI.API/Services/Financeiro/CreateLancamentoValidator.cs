using FluentValidation;
using GestorAI.API.DTOs.Financeiro;

namespace GestorAI.API.Services.Financeiro;

public class CreateLancamentoValidator : AbstractValidator<CreateLancamentoRequest>
{
    public CreateLancamentoValidator()
    {
        RuleFor(x => x.Tipo)
            .Must(t => t is "Receita" or "Despesa")
            .WithMessage("Tipo deve ser 'Receita' ou 'Despesa'.");
        RuleFor(x => x.Descricao).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Valor).GreaterThan(0);
        RuleFor(x => x.DataVencimento).NotEmpty();
        RuleFor(x => x.Categoria).NotEmpty().MaximumLength(100);
    }
}
