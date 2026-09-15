using FluentValidation;

namespace Application.Projects;

public sealed class UpdateProjectHandler(
    IProjectRepository projects,
    IValidator<UpdateProjectRequest> validator
)
{
    public async Task<ProjectDto> HandleAsync(
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var project =
            await projects.GetByIdAsync(request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project '{request.ProjectId}' was not found.");

        project.Update(request.Name, request.Description);
        await projects.UpdateAsync(project, cancellationToken);

        return ProjectDto.FromDomain(project);
    }
}
