using Api.Models;

namespace Api.Dtos;

public record StatusDto(int Id, string Name, int SortOrder, bool IsActive)
{
    public static StatusDto FromEntity(Status s) =>
        new(s.Statusid, s.Name, s.Sortorder, s.Isactive);
}