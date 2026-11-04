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
    ProjectService projectService
)
{
    public async Task<IReadOnlyList<ItemDto>> GetItemsFromProjectAsync(
        int projectId,
        int currentUserId,
        bool currentUserIsAdmin,
        CancellationToken ct = default
    )
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Projectid == projectId, ct);

        // same response for "doesn't exist" and "no access" so we don't leak which ids exist
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

        // projected in SQL so the type and status names come back in the same query
        return await db
            .Items.AsNoTracking()
            .Where(i => i.Projectid == projectId)
            .OrderByDescending(i => i.Itemid)
            .Select(i => new ItemDto(
                i.Itemid,
                i.Projectid,
                i.Title,
                i.Description,
                i.Itemtypeid,
                i.Statusid,
                i.Itemtype.Name,
                i.Status.Name
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

        // TODO (US-08): replace with the status assignability check
        var statusExists = await db.Statuses.AnyAsync(s => s.Statusid == request.StatusId, ct);
        if (!statusExists)
        {
            throw new KeyNotFoundException($"Status '{request.StatusId}' was not found.");
        }

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
