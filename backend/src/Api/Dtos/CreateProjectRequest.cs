namespace Api.Dtos;

public sealed record CreateProjectRequest(string Name, string? Description, int CreatedByUserId);
