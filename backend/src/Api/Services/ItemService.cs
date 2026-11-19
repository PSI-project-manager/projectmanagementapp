using Api.Data;
using Api.Dtos;
using Api.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class ItemService(
    AppDbContext db,
    IValidator<CreateItemRequest> createValidator,
    IValidator<UpdateItemRequest> updateValidator,
    ItemTypeService itemTypeService,
    StatusService statusService,
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
        int currentUserId,
        bool currentUserIsAdmin,
        CancellationToken ct = default
    )
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var projectExists = await db.Projects.AnyAsync(p => p.Projectid == request.ProjectId, ct);

        // same response as a missing project so we don't leak which ids exist
        if (
            !projectExists
            || !await projectService.CanAccessAsync(
                request.ProjectId,
                currentUserId,
                currentUserIsAdmin,
                ct
            )
        )
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

    public async Task<ItemDto> UpdateAsync(
        UpdateItemRequest request,
        int currentUserId,
        bool currentUserIsAdmin,
        CancellationToken ct = default
    )
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var item =
            await db.Items.FirstOrDefaultAsync(i => i.Itemid == request.ItemId, ct)
            ?? throw new KeyNotFoundException($"Item '{request.ItemId}' was not found.");

        if (
            !await projectService.CanAccessAsync(
                item.Projectid,
                currentUserId,
                currentUserIsAdmin,
                ct
            )
        )
        {
            throw new KeyNotFoundException($"Item '{request.ItemId}' was not found.");
        }

        await itemTypeService.EnsureAssignableAsync(request.ItemTypeId, item.Itemtypeid, ct);
        await statusService.EnsureAssignableAsync(request.StatusId, item.Statusid, ct);

        item.Title = request.Title.Trim();
        item.Description = request.Description.Trim();
        item.Itemtypeid = request.ItemTypeId;
        item.Statusid = request.StatusId;

        await db.SaveChangesAsync(ct);

        await db.Entry(item).Reference(i => i.Itemtype).LoadAsync(ct);
        await db.Entry(item).Reference(i => i.Status).LoadAsync(ct);

        return ItemDto.FromEntity(item);
    }
}
