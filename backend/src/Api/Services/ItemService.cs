using Api.Data;
using Api.Dtos;
using Api.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class ItemService(
    AppDbContext db,
    IValidator<CreateItemRequest> createValidator,
    ItemTypeService itemTypeService,
    StatusService statusService,
    ProjectService projectService
)
{
    public async Task<IReadOnlyList<ItemDto>> ListAsync(
        ItemFilter filter,
        int currentUserId,
        bool currentUserIsAdmin,
        CancellationToken ct = default
    )
    {
        var query = db.Items.AsNoTracking();

        // 1) access scope first - filters below can only narrow it down, never widen it
        if (filter.ProjectId is { } projectId)
        {
            var projectExists = await db.Projects.AnyAsync(p => p.Projectid == projectId, ct);

            // same response as a missing project so we don't leak which ids exist
            if (
                !projectExists
                || !await projectService.CanAccessAsync(
                    projectId,
                    currentUserId,
                    currentUserIsAdmin,
                    ct
                )
            )
            {
                throw new KeyNotFoundException($"Project '{projectId}' was not found.");
            }

            query = query.Where(i => i.Projectid == projectId);
        }
        else if (!currentUserIsAdmin)
        {
            // no project picked: everything from the projects the user is a member of
            query = query.Where(i => i.Project.Projectusers.Any(pu => pu.Userid == currentUserId));
        }

        // 2) optional filters
        if (filter.ItemTypeId is { } itemTypeId)
        {
            query = query.Where(i => i.Itemtypeid == itemTypeId);
        }

        if (filter.StatusId is { } statusId)
        {
            query = query.Where(i => i.Statusid == statusId);
        }

        return await query
            .OrderByDescending(i => i.Itemid)
            .Select(i => new ItemDto(
                i.Itemid,
                i.Projectid,
                i.Title,
                i.Description,
                i.Itemtypeid,
                i.Statusid,
                i.Itemtype.Name,
                i.Status.Name,
                i.Project.Name
            ))
            .ToListAsync(ct);
    }

    public async Task<ItemDto> CreateAsync(
        CreateItemRequest request,
        CancellationToken ct = default
    )
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var projectExists = await db.Projects.AnyAsync(p => p.Projectid == request.ProjectId, ct);
        if (!projectExists)
        {
            throw new KeyNotFoundException($"Project '{request.ProjectId}' was not found.");
        }

        await itemTypeService.EnsureAssignableAsync(request.ItemTypeId, null, ct);
        await statusService.EnsureAssignableAsync(request.StatusId, null, ct);

        var item = new Item
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Projectid = request.ProjectId,
            Itemtypeid = request.ItemTypeId,
            Statusid = request.StatusId,
        };

        db.Items.Add(item);
        await db.SaveChangesAsync(ct);

        return ItemDto.FromEntity(item);
    }
}
