namespace Api.Dtos;

public record ProjectDto(int Id, string Name, string? Description, bool IsActive, int CreatedByUserId);
