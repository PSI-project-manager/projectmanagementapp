using Api.Models;

namespace Api.Dtos;

public sealed record ItemTypeDto(int Id, string Name, bool IsActive)
{
    public static ItemTypeDto FromEntity(Itemtype itemType) =>
        new(itemType.Itemtypeid, itemType.Name, itemType.Isactive);
}
