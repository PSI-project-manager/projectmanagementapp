using Api.Data;
using Api.Dtos;
using Api.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

/// <summary>
/// Project CRUD plus project-level access control (US-06). Access is granted by an
/// explicit <see cref="Projectuser"/> row; a project's creator is granted access
/// automatically on creation. Admins bypass the access check and see/open every project.
///
/// Note: nothing currently authenticates the caller and derives requestingUserId/isAdmin
/// from it (no [Authorize]/JWT middleware is wired in Program.cs yet, matching the rest of
/// the API). Callers pass those values in explicitly for now, same as CreatedByUserId on
/// CreateProjectRequest — wiring real authentication is separate from this story.
/// </summary>
public sealed class ProjectService(
    AppDbContext db,
    IValidator<CreateProjectRequest> createValidator,
    IValidator<UpdateProjectRequest> updateValidator,
    IValidator<ProjectAccessRequest> accessValidator
) : IProjectService
{
    public async Task<IReadOnlyList<ProjectDto>> ListAsync(
        int requestingUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default
    )
    {
        var query = db.Projects.AsNoTracking();

        if (!isAdmin)
        {
            query = query.Where(p => p.Projectusers.Any(pu => pu.Userid == requestingUserId));
        }

        var projects = await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);

        return projects.Select(ProjectDto.FromEntity).ToList();
    }

    public async Task<ProjectDto> GetByIdAsync(
        int projectId,
        int requestingUserId,
        bool isAdmin,
        CancellationToken cancellationToken = default
    )
    {
        var project =
            await db
                .Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Projectid == projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project '{projectId}' was not found.");

        if (!isAdmin && !await HasAccessAsync(projectId, requestingUserId, cancellationToken))
        {
            throw new UnauthorizedAccessException(
                $"User '{requestingUserId}' does not have access to project '{projectId}'."
            );
        }

        return ProjectDto.FromEntity(project);
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

        // The creator can always see their own project.
        project.Projectusers.Add(new Projectuser { Userid = request.CreatedByUserId });

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

    public async Task<IReadOnlyList<ProjectAccessDto>> ListAccessAsync(
        int projectId,
        CancellationToken cancellationToken = default
    )
    {
        var projectExists = await db.Projects.AnyAsync(
            p => p.Projectid == projectId,
            cancellationToken
        );

        if (!projectExists)
        {
            throw new KeyNotFoundException($"Project '{projectId}' was not found.");
        }

        var grants = await db
            .Projectusers.AsNoTracking()
            .Include(pu => pu.User)
            .Where(pu => pu.Projectid == projectId)
            .OrderBy(pu => pu.User.Fullname)
            .ToListAsync(cancellationToken);

        return grants.Select(ProjectAccessDto.FromEntity).ToList();
    }

    public async Task GrantAccessAsync(
        ProjectAccessRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await accessValidator.ValidateAndThrowAsync(request, cancellationToken);

        var projectExists = await db.Projects.AnyAsync(
            p => p.Projectid == request.ProjectId,
            cancellationToken
        );

        if (!projectExists)
        {
            throw new KeyNotFoundException($"Project '{request.ProjectId}' was not found.");
        }

        var user =
            await db.Users.FirstOrDefaultAsync(u => u.Userid == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{request.UserId}' was not found.");

        if (!user.Isactive)
        {
            throw new ValidationException(
                $"User '{request.UserId}' is inactive and cannot be granted project access."
            );
        }

        var alreadyGranted = await HasAccessAsync(
            request.ProjectId,
            request.UserId,
            cancellationToken
        );

        if (alreadyGranted)
        {
            return;
        }

        db.Projectusers.Add(
            new Projectuser { Projectid = request.ProjectId, Userid = request.UserId }
        );

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAccessAsync(
        ProjectAccessRequest request,
        CancellationToken cancellationToken = default
    )
    {
        await accessValidator.ValidateAndThrowAsync(request, cancellationToken);

        var grant = await db.Projectusers.FirstOrDefaultAsync(
            pu => pu.Projectid == request.ProjectId && pu.Userid == request.UserId,
            cancellationToken
        );

        if (grant is null)
        {
            // Already has no access - revoking is idempotent.
            return;
        }

        db.Projectusers.Remove(grant);
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> HasAccessAsync(
        int projectId,
        int userId,
        CancellationToken cancellationToken
    ) =>
        db.Projectusers.AnyAsync(
            pu => pu.Projectid == projectId && pu.Userid == userId,
            cancellationToken
        );

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}