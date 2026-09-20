using Api.Dtos;
using Api.Models;
using FluentValidation;

namespace Api.Validators;

public sealed class CreateItemTypeRequestValidator : AbstractValidator<CreateItemTypeRequest>
{
    public CreateItemTypeRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(Itemtype.MaxNameLength)
            .WithMessage($"Name cannot exceed {Itemtype.MaxNameLength} characters.");
    }
}
