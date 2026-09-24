using Api.Dtos;
using Api.Models;
using FluentValidation;

namespace Api.Validators;

public class UpdateItemTypeRequestValidator : AbstractValidator<UpdateItemTypeRequest>
{
    public UpdateItemTypeRequestValidator()
    {
        RuleFor(x => x.ItemTypeId).GreaterThan(0).WithMessage("ItemTypeId must be valid.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(Itemtype.MaxNameLength)
            .WithMessage($"Name cannot exceed {Itemtype.MaxNameLength} characters.");
    }
}
