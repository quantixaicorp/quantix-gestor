using FluentValidation;
using GestorAI.API.DTOs.Vendas;

namespace GestorAI.API.Services.Vendas;

public class CreateVendaValidator : AbstractValidator<CreateVendaRequest>
{
    public CreateVendaValidator()
    {
        RuleFor(x => x.Items).NotEmpty().WithMessage("A venda precisa ter ao menos um item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.Discount).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Discount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentMethod)
            .Must(f => new[] { "Dinheiro", "Pix", "Cartao", "Outro" }.Contains(f))
            .WithMessage("Forma de pagamento inválida.");
        RuleFor(x => x.Installments).GreaterThan(0).When(x => x.Installments.HasValue);
    }
}
