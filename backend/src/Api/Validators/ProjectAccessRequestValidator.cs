using Api.Dtos;
using FluentValidation;

namespace Api.Validators;

public sealed class ProjectAccessRequestValidator : AbstractValidator<ProjectAccessRequest>
{
    public ProjectAccessRequestValidator()
    {
        RuleFor(x => x.ProjectId)
            .GreaterThan(0)
            .WithMessage("A project must be specified.");

        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("A user must be specified.");
    }
}