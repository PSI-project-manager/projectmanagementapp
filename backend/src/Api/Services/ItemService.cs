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
        CancellationToken ct = default
    )
    {
        var projectExists = await db.Projects.AnyAsync(p => p.Projectid == projectId, ct);

        if (!projectExists || !await projectService.CanAccessAsync(projectId, currentUserId, ct))
        {
            throw new KeyNotFoundException($"Project '{projectId}' was not found.");
        }

        var items = await db
            .Items.AsNoTracking()
            .Where(i => i.Projectid == projectId)
            .OrderByDescending(i => i.Itemid)
            .ToListAsync(ct);

        return items.Select(ItemDto.FromEntity).ToList();
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
