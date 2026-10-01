using Api.Models;

namespace Api.Dtos;

public record ItemDto(
    int Id,
    int ProjectId,
    string Title,
    string? Description,
    int ItemTypeId,
    int StatusId
)
{
    public static ItemDto FromEntity(Item item) =>
        new(
            item.Itemid,
            item.Projectid,
            item.Title,
            item.Description,
            item.Itemtypeid,
            item.Statusid
        );
}
