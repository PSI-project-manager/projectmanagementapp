using Api.Dtos;
using FluentValidation;

namespace Api.Validators;

public class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.ItemId).GreaterThan(0).WithMessage("ItemId must be valid.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("Title is required.").MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
        RuleFor(x => x.ItemTypeId).GreaterThan(0).WithMessage("Item type is required.");
        RuleFor(x => x.StatusId).GreaterThan(0).WithMessage("Status is required.");
    }
}
