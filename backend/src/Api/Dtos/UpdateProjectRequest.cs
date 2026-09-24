namespace Api.Dtos;

public record UpdateProjectRequest(int ProjectId, string Name, string? Description);
