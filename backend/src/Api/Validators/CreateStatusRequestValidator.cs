using Api.Dtos;
using Api.Models;
using FluentValidation;

namespace Api.Validators;

public class CreateStatusRequestValidator : AbstractValidator<CreateStatusRequest>
{
    public CreateStatusRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(Status.MaxNameLength)
            .WithMessage($"Name cannot exceed {Status.MaxNameLength} characters.");
        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SortOrder.HasValue)
            .WithMessage("Sort order cannot be negative.");
    }
}
