using Api.Models;

namespace Api.Dtos;

public sealed record ProjectDto(
    int Id,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int CreatedByUserId
)
{
    public static ProjectDto FromEntity(Project project) =>
        new(
            project.Projectid,
            project.Name,
            project.Description,
            project.Isactive,
            project.Createdat,
            project.Updatedat,
            project.Createdbyuserid
        );
}
