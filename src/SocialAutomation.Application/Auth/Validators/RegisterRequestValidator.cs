// src/SocialAutomation.Application/Auth/Validators/RegisterRequestValidator.cs

using FluentValidation;
using SocialAutomation.Application.Auth.DTOs;

namespace SocialAutomation.Application.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.TenantName)
            .NotEmpty().WithMessage("Nome da empresa é obrigatório.")
            .MinimumLength(2).WithMessage("Nome da empresa deve ter pelo menos 2 caracteres.")
            .MaximumLength(255);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MinimumLength(2).WithMessage("Nome deve ter pelo menos 2 caracteres.")
            .MaximumLength(255);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email é obrigatório.")
            .EmailAddress().WithMessage("Email inválido.")
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter pelo menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("Senha deve conter pelo menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("Senha deve conter pelo menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("Senha deve conter pelo menos um número.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage("Confirmação de senha não confere.");
    }
}