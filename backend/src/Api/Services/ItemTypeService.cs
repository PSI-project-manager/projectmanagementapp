using Api.Data;
using Api.Dtos;
using Api.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public sealed class ItemTypeService(
    AppDbContext db,
    IValidator<CreateItemTypeRequest> createValidator,
    IValidator<UpdateItemTypeRequest> updateValidator
) : IItemTypeService
{
    public async Task<IEnumerable<ItemTypeDto>> ListAsync(
        bool activeOnly = false,
        CancellationToken ct = default
    )
    {
        var query = db.Itemtypes.AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(t => t.Isactive);
        }

        return await query
            .OrderBy(t => t.Name)
            .Select(t => ItemTypeDto.FromEntity(t))
            .ToListAsync(ct);
    }

    public async Task<ItemTypeDto> CreateAsync(
        CreateItemTypeRequest request,
        CancellationToken ct = default
    )
    {
        await createValidator.ValidateAndThrowAsync(request, ct);

        var trimmedName = request.Name.Trim();

        var exists = await db.Itemtypes.AnyAsync(
            t => t.Name.ToLower() == trimmedName.ToLower(),
            ct
        );

        if (exists)
        {
            throw new ValidationException($"An item type named '{trimmedName}' already exists.");
        }

        var entity = new Itemtype { Name = trimmedName, Isactive = true };

        db.Itemtypes.Add(entity);
        await db.SaveChangesAsync(ct);

        return ItemTypeDto.FromEntity(entity);
    }

    public async Task<ItemTypeDto> UpdateAsync(
        UpdateItemTypeRequest request,
        CancellationToken ct = default
    )
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var entity =
            await db.Itemtypes.FirstOrDefaultAsync(t => t.Itemtypeid == request.ItemTypeId, ct)
            ?? throw new KeyNotFoundException(
                $"Item type with ID '{request.ItemTypeId}' was not found."
            );

        var trimmedName = request.Name.Trim();

        var duplicateExists = await db.Itemtypes.AnyAsync(
            t => t.Itemtypeid != request.ItemTypeId && t.Name.ToLower() == trimmedName.ToLower(),
            ct
        );

        if (duplicateExists)
        {
            throw new ValidationException($"An item type named '{trimmedName}' already exists.");
        }

        entity.Name = trimmedName;
        entity.Isactive = request.IsActive;

        await db.SaveChangesAsync(ct);

        return ItemTypeDto.FromEntity(entity);
    }

    public async Task EnsureAssignableAsync(
        int itemTypeId,
        int? currentItemTypeId,
        CancellationToken ct = default
    )
    {
        if (itemTypeId == currentItemTypeId)
        {
            return;
        }

        var type =
            await db
                .Itemtypes.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Itemtypeid == itemTypeId, ct)
            ?? throw new KeyNotFoundException($"Item type '{itemTypeId}' was not found.");

        if (!type.Isactive)
        {
            throw new ValidationException(
                $"Item type '{type.Name}' is inactive and cannot be assigned."
            );
        }
    }
}
