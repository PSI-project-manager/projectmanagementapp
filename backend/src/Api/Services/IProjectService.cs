using Api.Dtos;

namespace Api.Services;

public interface IProjectService
{
    /// <summary>
    /// Lists projects visible to <paramref name="requestingUserId"/>: every project when
    /// <paramref name="isAdmin"/> is true, otherwise only projects that user has been
    /// granted access to (US-06).
    /// </summary>
    Task<IReadOnlyList<ProjectDto>> ListAsync(
        int requestingUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Fetches a single project, throwing <see cref="UnauthorizedAccessException"/> if
    /// <paramref name="requestingUserId"/> has not been granted access to it (US-06).
    /// </summary>
    Task<ProjectDto> GetByIdAsync(
        int projectId,
        int requestingUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default
    );

    Task<ProjectDto> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default
    );

    Task<ProjectDto> UpdateAsync(
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Lists the users currently granted access to a project.</summary>
    Task<IReadOnlyList<ProjectAccessDto>> ListAccessAsync(
        int projectId,
        CancellationToken cancellationToken = default
    );

    /// <summary>Grants a user access to a project. Idempotent if already granted.</summary>
    Task GrantAccessAsync(
        ProjectAccessRequest request,
        CancellationToken cancellationToken = default
    );

    /// <summary>Revokes a user's access to a project. Idempotent if already revoked.</summary>
    Task RevokeAccessAsync(
        ProjectAccessRequest request,
        CancellationToken cancellationToken = default
    );
}