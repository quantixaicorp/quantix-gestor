using FluentValidation;
using GestorAI.API.DTOs.Fornecedores;

namespace GestorAI.API.Services.Fornecedores;

public class CreateFornecedorValidator : AbstractValidator<CreateFornecedorRequest>
{
    public CreateFornecedorValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CnpjCpf)
            .Matches(@"(?i)^(?:\d{11}|[A-Z0-9]{12}\d{2})$")
            .WithMessage("CPF deve ter 11 dígitos ou CNPJ 12 caracteres alfanuméricos e 2 dígitos verificadores")
            .When(x => !string.IsNullOrEmpty(x.CnpjCpf));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Phone).MaximumLength(20);
        RuleFor(x => x.Uf).MaximumLength(2);
        RuleFor(x => x.Cep).MaximumLength(9);
    }
}
