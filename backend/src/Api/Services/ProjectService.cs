using Api.Data;
using Api.Dtos;
using Api.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class ProjectService(
    AppDbContext db,
    IValidator<CreateProjectRequest> createValidator,
    IValidator<UpdateProjectRequest> updateValidator
)
{
    public async Task<IReadOnlyList<ProjectDto>> ListAsync(
        int currentUserId,
        bool currentUserIsAdmin,
        CancellationToken cancellationToken = default
    )
    {
        var query = db.Projects.AsNoTracking();

        if (!currentUserIsAdmin)
        {
            query = query.Where(p => p.Projectusers.Any(pu => pu.Userid == currentUserId));
        }

        var projects = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);

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

        // the creator can always see their own project
        project.Projectusers.Add(new Projectuser { Userid = request.CreatedByUserId });

        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return ProjectDto.FromEntity(project);
    }

    public async Task<ProjectDto> UpdateAsync(
        UpdateProjectRequest request,
        int currentUserId,
        bool currentUserIsAdmin,
        CancellationToken cancellationToken = default
    )
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var project =
            await db.Projects.FirstOrDefaultAsync(
                p => p.Projectid == request.ProjectId,
                cancellationToken
            ) ?? throw new KeyNotFoundException($"Project '{request.ProjectId}' was not found.");

        // same response as a missing project so we don't leak which ids exist
        if (
            !await CanAccessAsync(
                project.Projectid,
                currentUserId,
                currentUserIsAdmin,
                cancellationToken
            )
        )
        {
            throw new KeyNotFoundException($"Project '{request.ProjectId}' was not found.");
        }

        project.Name = request.Name.Trim();
        project.Description = NormalizeDescription(request.Description);

        await db.SaveChangesAsync(cancellationToken);

        return ProjectDto.FromEntity(project);
    }

    // admins reach every project; everyone else only the ones they are a member of
    private async Task<bool> CanAccessAsync(
        int projectId,
        int userId,
        bool isAdmin,
        CancellationToken cancellationToken
    ) =>
        isAdmin
        || await db.Projectusers.AnyAsync(
            pu => pu.Projectid == projectId && pu.Userid == userId,
            cancellationToken
        );

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
