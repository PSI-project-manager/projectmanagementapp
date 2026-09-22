using Api.Dtos;
using Api.Models;
using FluentValidation;

namespace Api.Validators;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email address.")
            .MaximumLength(User.MaxEmailLength);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(User.MaxFullNameLength);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(User.MinPasswordLength)
            .WithMessage($"Password must be at least {User.MinPasswordLength} characters long.");
    }
}
