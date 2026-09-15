using Application.Projects;
using Domain.Entities;

namespace Application.Tests.Fakes;

internal sealed class FakeProjectRepository : IProjectRepository
{
    private readonly Dictionary<Guid, Project> _projects = new();

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _projects.TryGetValue(id, out var project);
        return Task.FromResult(project);
    }

    public Task<IReadOnlyList<Project>> ListByOrganizationAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default
    )
    {
        IReadOnlyList<Project> result = _projects
            .Values.Where(p => p.OrganizationId == organizationId)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        _projects[project.Id] = project;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        _projects[project.Id] = project;
        return Task.CompletedTask;
    }
}
