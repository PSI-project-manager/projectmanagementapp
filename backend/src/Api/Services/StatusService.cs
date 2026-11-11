using System.Linq;
using Api.Data;
using Api.Dtos;
using Api.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class StatusService(
    AppDbContext db,
    IValidator<CreateStatusRequest> createValidator,
    IValidator<UpdateStatusRequest> updateValidator
)
{
    public async Task<IEnumerable<StatusDto>> ListAsync(
        bool activeOnly = false,
        CancellationToken ct = default
    )
    {
        var query = db.Statuses.AsNoTracking();

        if (activeOnly)
            query = query.Where(s => s.Isactive);

        return await query
            .OrderBy(s => s.Sortorder)
            .ThenBy(s => s.Name)
            .Select(s => StatusDto.FromEntity(s))
            .ToListAsync(ct);
    }

    public async Task<StatusDto> CreateAsync(
        CreateStatusRequest request,
        CancellationToken ct = default
    )
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        var name = request.Name.Trim();

        if (await db.Statuses.AnyAsync(
            s => s.Name.ToLower() == name.ToLower(), ct))
            throw new ValidationException($"A status named '{name}' already exists.");

        var sortOrder =
            request.SortOrder ?? (await db.Statuses.MaxAsync(s => (int?)s.Sortorder, ct) ?? 0) + 1;

        var entity = new Status
        {
            Name = name,
            Sortorder = sortOrder,
            Isactive = true,
        };

        db.Statuses.Add(entity);
        await db.SaveChangesAsync(ct);

        return StatusDto.FromEntity(entity);
    }

    public async Task<StatusDto> UpdateAsync(
        UpdateStatusRequest request,
        CancellationToken ct = default
    )
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);

        var entity =
            await db.Statuses.FirstOrDefaultAsync(
                s => s.Statusid == request.StatusId, ct)
            ?? throw new KeyNotFoundException(
                $"Status with ID '{request.StatusId}' was not found."
            );

        var name = request.Name.Trim();

        if (await db.Statuses.AnyAsync(
            s => s.Statusid != request.StatusId
                 && s.Name.ToLower() == name.ToLower(),
            ct))
        {
            throw new ValidationException(
                $"A status named '{name}' already exists."
            );
        }

        entity.Name = name;
        entity.Sortorder = request.SortOrder;
        entity.Isactive = request.IsActive;

        await db.SaveChangesAsync(ct);

        return StatusDto.FromEntity(entity);
    }

    public async Task EnsureAssignableAsync(
        int statusId,
        int? currentStatusId,
        CancellationToken ct = default
    )
    {
        if (statusId == currentStatusId)
            return;

        var status =
            await db.Statuses.AsNoTracking()
                .FirstOrDefaultAsync(s => s.Statusid == statusId, ct)
            ?? throw new KeyNotFoundException(
                $"Status '{statusId}' was not found."
            );

        if (!status.Isactive)
            throw new ValidationException(
                $"Status '{status.Name}' is inactive and cannot be assigned."
            );
    }
}