using Api.Data;
using Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class StatusService(AppDbContext db)
{
    public async Task<IEnumerable<StatusDto>> ListAsync(CancellationToken ct = default) =>
        await db
            .Statuses.AsNoTracking()
            .OrderBy(s => s.Sortorder)
            .ThenBy(s => s.Name)
            .Select(s => new StatusDto(s.Statusid, s.Name, s.Isactive))
            .ToListAsync(ct);
}
