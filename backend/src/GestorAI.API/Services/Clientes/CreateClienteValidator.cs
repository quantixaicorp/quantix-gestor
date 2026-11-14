using FluentValidation;
using GestorAI.API.DTOs.Clientes;

namespace GestorAI.API.Services.Clientes;

public class CreateClienteValidator : AbstractValidator<CreateClienteRequest>
{
    public CreateClienteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WhatsApp).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PersonType)
            .Must(ClienteDocumento.IsTipoValido)
            .WithMessage(ClienteDocumento.TipoInvalido)
            .When(x => !string.IsNullOrEmpty(x.PersonType));
        RuleFor(x => x.PersonType)
            .NotEmpty()
            .WithMessage(ClienteDocumento.TipoObrigatorio)
            .When(x => !string.IsNullOrEmpty(x.CnpjCpf));
        RuleFor(x => x.CnpjCpf)
            .Must((req, doc) => ClienteDocumento.IsDocumentoValido(req.PersonType, doc))
            .WithMessage(req => req.PersonType == ClienteDocumento.PessoaFisica
                ? ClienteDocumento.CpfInvalido
                : ClienteDocumento.CnpjInvalido)
            .When(x => ClienteDocumento.IsTipoValido(x.PersonType));
    }
}

public class UpdateClienteValidator : AbstractValidator<UpdateClienteRequest>
{
    public UpdateClienteValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WhatsApp).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrEmpty(x.Email));

        RuleFor(x => x.PersonType)
            .Must(ClienteDocumento.IsTipoValido)
            .WithMessage(ClienteDocumento.TipoInvalido)
            .When(x => !string.IsNullOrEmpty(x.PersonType));
        RuleFor(x => x.PersonType)
            .NotEmpty()
            .WithMessage(ClienteDocumento.TipoObrigatorio)
            .When(x => !string.IsNullOrEmpty(x.CnpjCpf));
        RuleFor(x => x.CnpjCpf)
            .Must((req, doc) => ClienteDocumento.IsDocumentoValido(req.PersonType, doc))
            .WithMessage(req => req.PersonType == ClienteDocumento.PessoaFisica
                ? ClienteDocumento.CpfInvalido
                : ClienteDocumento.CnpjInvalido)
            .When(x => ClienteDocumento.IsTipoValido(x.PersonType));
    }
}
