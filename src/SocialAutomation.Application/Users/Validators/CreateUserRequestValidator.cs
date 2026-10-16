// src/SocialAutomation.Application/Users/Validators/CreateUserRequestValidator.cs

using FluentValidation;
using SocialAutomation.Application.Users.DTOs;

namespace SocialAutomation.Application.Users.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2)
            .MaximumLength(255);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter pelo menos 8 caracteres.");
    }
}