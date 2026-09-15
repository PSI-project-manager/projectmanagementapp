namespace Application.Projects;

/// <summary>
/// Lists every project in an organization. Per the current plan, project-level access
/// scoping for contributors (US-06) doesn't exist yet, so this intentionally returns all
/// of an organization's projects until that access model lands.
/// </summary>
public sealed class ListProjectsHandler(IProjectRepository projects)
{
    public async Task<IReadOnlyList<ProjectDto>> HandleAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        var result = await projects.ListByOrganizationAsync(organizationId, cancellationToken);
        return result.Select(ProjectDto.FromDomain).ToList();
    }
}
