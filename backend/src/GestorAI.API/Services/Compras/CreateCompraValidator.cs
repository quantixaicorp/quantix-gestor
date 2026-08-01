using FluentValidation;
using GestorAI.API.DTOs.Compras;

namespace GestorAI.API.Services.Compras;

public class CreateCompraValidator : AbstractValidator<CreateCompraRequest>
{
    private static readonly string[] CondicoesPagamento =
        ["AVista", "30d", "30_60_90d", "Parcelado", "Personalizado"];

    public CreateCompraValidator()
    {
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.PurchaseType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PaymentTerms).Must(v => CondicoesPagamento.Contains(v))
            .WithMessage("Condição de pagamento inválida.");
        RuleFor(x => x.PaymentMethod).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("A compra deve ter ao menos um item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Description).NotEmpty().MaximumLength(200);
            item.RuleFor(i => i.Quantity).GreaterThan(0);
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.InstallmentCount)
            .GreaterThan(0)
            .When(x => x.PaymentTerms == "Parcelado")
            .WithMessage("Informe a quantidade de parcelas.");
        RuleFor(x => x.ParcelasPersonalizadas)
            .NotEmpty()
            .When(x => x.PaymentTerms == "Personalizado")
            .WithMessage("Informe as parcelas personalizadas.");
    }
}
