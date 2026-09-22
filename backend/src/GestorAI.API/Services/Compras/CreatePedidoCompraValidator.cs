using FluentValidation;
using GestorAI.API.DTOs.Compras;

namespace GestorAI.API.Services.Compras;

public class CreatePedidoCompraValidator : AbstractValidator<CreatePedidoCompraRequest>
{
    public CreatePedidoCompraValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("O pedido deve ter ao menos um item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.EstimatedAmount).GreaterThanOrEqualTo(0);
        });
    }
}
