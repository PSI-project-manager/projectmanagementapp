using Api.Dtos;
using Api.Models;
using FluentValidation;

namespace Api.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("UserId must be valid.");

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
    }
}
