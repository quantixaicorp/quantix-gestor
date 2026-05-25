using FluentValidation;
using GestorAI.API.DTOs.Clientes;

namespace GestorAI.API.Services.Clientes;

public class CreateClienteValidator : AbstractValidator<CreateClienteRequest>
{
    public CreateClienteValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Whatsapp).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));
    }
}
