using Domain.Entities;
using FluentValidation;

namespace Application.Projects;

public sealed class CreateProjectHandler(IProjectRepository projects, IValidator<CreateProjectRequest> validator)
{
    public async Task<ProjectDto> HandleAsync(CreateProjectRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var project = new Project(request.OrganizationId, request.Name, request.Description);
        await projects.AddAsync(project, cancellationToken);

        return ProjectDto.FromDomain(project);
    }
}
