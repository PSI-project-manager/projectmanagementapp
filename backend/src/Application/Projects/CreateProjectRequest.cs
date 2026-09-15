using Domain.Entities;
using FluentValidation;

namespace Application.Projects;

public sealed record CreateProjectRequest(Guid OrganizationId, string Name, string? Description);

public sealed class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty().WithMessage("A project must belong to an organization.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name is required.")
            .MaximumLength(Project.MaxNameLength);

        RuleFor(x => x.Description).MaximumLength(Project.MaxDescriptionLength);
    }
}
