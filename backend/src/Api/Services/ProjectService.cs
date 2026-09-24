using Api.Data;
using Api.Dtos;
using Api.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Project-level access scoping for contributors (US-06) doesn't exist yet, so
/// <see cref="ListAsync"/> intentionally returns every project until that access model lands.
/// </summary>
public class ProjectService(
    AppDbContext db,
    IValidator<CreateProjectRequest> createValidator,
    IValidator<UpdateProjectRequest> updateValidator
)
{
    public async Task<IReadOnlyList<ProjectDto>> ListAsync(
        CancellationToken cancellationToken = default
    )
    {
        var projects = await db
            .Projects.AsNoTracking()
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return projects.Select(ProjectDto.FromEntity).ToList();
    }

    public async Task<ProjectDto> CreateAsync(
        CreateProjectRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var project = new Project
        {
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            Createdbyuserid = request.CreatedByUserId,
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return ProjectDto.FromEntity(project);
    }

    public async Task<ProjectDto> UpdateAsync(
        UpdateProjectRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var project =
            await db.Projects.FirstOrDefaultAsync(
                p => p.Projectid == request.ProjectId,
                cancellationToken
            ) ?? throw new KeyNotFoundException($"Project '{request.ProjectId}' was not found.");

        project.Name = request.Name.Trim();
        project.Description = NormalizeDescription(request.Description);

        await db.SaveChangesAsync(cancellationToken);

        return ProjectDto.FromEntity(project);
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
