namespace Application.Projects;

public sealed record ProjectDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
)
{
    public static ProjectDto FromDomain(Domain.Entities.Project project) =>
        new(
            project.Id,
            project.OrganizationId,
            project.Name,
            project.Description,
            project.CreatedAt,
            project.UpdatedAt
        );
}
