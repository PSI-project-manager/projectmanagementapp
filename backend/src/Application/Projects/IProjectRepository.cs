using Domain.Entities;

namespace Application.Projects;

/// <summary>
/// Persistence port for <see cref="Project"/>. Implemented in Infrastructure once the
/// database is wired up; Application only depends on this abstraction.
/// </summary>
public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Project>> ListByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(Project project, CancellationToken cancellationToken = default);

    Task UpdateAsync(Project project, CancellationToken cancellationToken = default);
}
