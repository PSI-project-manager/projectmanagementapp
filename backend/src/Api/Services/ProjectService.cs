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

    public async Task AddUserAsync(
        int userToAddId,
        int projectId,
        bool clientIsAdmin,
        CancellationToken cancellationToken
    )
    {
        if (!CanManage(clientIsAdmin))
            throw new Exception("You don't have permission to add users to this project.");

        bool userExists = await db.Users.AnyAsync(u => u.Userid == userToAddId, cancellationToken);
        if (!userExists)
            throw new KeyNotFoundException(
                "User you're trying to add to the project doesn't exist."
            );

        bool userAlreadyInSpecifiedProject = await db.Projectusers.AnyAsync(
            pu => pu.Projectid == projectId && pu.Userid == userToAddId,
            cancellationToken
        );
        if (userAlreadyInSpecifiedProject)
            throw new Exception("User you're trying to add is already in this project.");

        var project =
            await db.Projects.FirstOrDefaultAsync(p => p.Projectid == projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project '{projectId}' was not found.");

        project.Projectusers.Add(new Projectuser { Userid = userToAddId });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveUserAsync(
        int userToRemoveId,
        int projectId,
        bool clientIsAdmin,
        CancellationToken cancellationToken
    )
    {
        if (!CanManage(clientIsAdmin))
            throw new Exception("You don't have permission to remove users from this project.");

        var project =
            await db.Projects.FirstOrDefaultAsync(p => p.Projectid == projectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project '{projectId}' was not found.");

        var userToRemove =
            await db.Projectusers.FirstOrDefaultAsync(
                pu => pu.Projectid == projectId && pu.Userid == userToRemoveId,
                cancellationToken
            )
            ?? throw new KeyNotFoundException(
                "User isn't in the project you're trying to remove them from."
            );

        db.Projectusers.Remove(userToRemove);
        await db.SaveChangesAsync(cancellationToken);
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
    public async Task<bool> CanAccessAsync(
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

    // looks redundant for now, but if we decide to add more complex management logic later
    // we will just have to change few lines here instead of having to look all over
    // the project, and i think it's helpful to define the invariant in one place vs
    // putting isAdmin checks everywhere in methods like RemoveUserAsync and AddUserAsync
    // because semantically we're checking if the client can manage the project and not if they're
    // an admin
    public bool CanManage(bool isAdmin)
    {
        return isAdmin;
    }

    private static string? NormalizeDescription(string? description) =>
        string.IsNullOrWhiteSpace(description) ? null : description.Trim();
}
