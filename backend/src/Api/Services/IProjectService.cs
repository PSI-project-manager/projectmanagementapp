using Api.Dtos;

namespace Api.Services;

public interface IProjectService
{
    Task<IReadOnlyList<ProjectDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<ProjectDto> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default
    );

    Task<ProjectDto> UpdateAsync(
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default
    );
}
