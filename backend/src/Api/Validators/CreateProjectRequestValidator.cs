using Api.Dtos;
using Api.Models;
using FluentValidation;

namespace Api.Validators;

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name is required.")
            .MaximumLength(Project.MaxNameLength);

        RuleFor(x => x.Description).MaximumLength(Project.MaxDescriptionLength);

        RuleFor(x => x.CreatedByUserId)
            .GreaterThan(0)
            .WithMessage("A project must have a creator.");
    }
}
