using Api.Dtos;
using Api.Models;
using FluentValidation;

namespace Api.Validators;

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.StatusId).GreaterThan(0).WithMessage("StatusId must be valid.");
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(Status.MaxNameLength)
            .WithMessage($"Name cannot exceed {Status.MaxNameLength} characters.");
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
