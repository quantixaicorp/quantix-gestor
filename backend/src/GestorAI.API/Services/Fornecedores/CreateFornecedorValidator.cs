using FluentValidation;
using GestorAI.API.DTOs.Fornecedores;

namespace GestorAI.API.Services.Fornecedores;

public class CreateFornecedorValidator : AbstractValidator<CreateFornecedorRequest>
{
    public CreateFornecedorValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CnpjCpf)
            .Must(v => v == null || System.Text.RegularExpressions.Regex.IsMatch(v, @"^\d{11}$|^\d{14}$"))
            .WithMessage("CNPJ deve ter 14 dígitos ou CPF 11 dígitos (apenas números)")
            .When(x => !string.IsNullOrEmpty(x.CnpjCpf));
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
        RuleFor(x => x.Telefone).MaximumLength(20);
        RuleFor(x => x.Uf).MaximumLength(2);
        RuleFor(x => x.Cep).MaximumLength(9);
    }
}
