using Domain.Entities;
using FluentValidation;

namespace Application.Projects;

public sealed record UpdateProjectRequest(Guid ProjectId, string Name, string? Description);

public sealed class UpdateProjectRequestValidator : AbstractValidator<UpdateProjectRequest>
{
    public UpdateProjectRequestValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name is required.")
            .MaximumLength(Project.MaxNameLength);

        RuleFor(x => x.Description).MaximumLength(Project.MaxDescriptionLength);
    }
}
