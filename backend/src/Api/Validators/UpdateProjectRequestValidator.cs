using Api.Dtos;
using Api.Models;
using FluentValidation;

namespace Api.Validators;

public class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.ProjectId).GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name is required.")
            .MaximumLength(Project.MaxNameLength);

        RuleFor(x => x.Description).MaximumLength(Project.MaxDescriptionLength);
    }
}
