namespace Api.Dtos;

public sealed record UpdateProjectRequest(int ProjectId, string Name, string? Description);
