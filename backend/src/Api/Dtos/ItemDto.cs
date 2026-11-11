using Api.Models;

namespace Api.Dtos;

public record ItemDto(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    int ItemTypeId,
    int StatusId,
    string? ItemTypeName = null,
    string? StatusName = null,
    string? ProjectName = null
)
{
    public static ItemDto FromEntity(Item item) =>
        new(
            item.Itemid,
            item.Projectid,
            item.Title,
            item.Description,
            item.Itemtypeid,
            item.Statusid,
            item.Itemtype?.Name,
            item.Status?.Name,
            item.Project?.Name
        );
}
