using Api.Models;

namespace Api.Dtos;

public sealed record ProjectAccessDto(
    int ProjectId,
    int UserId,
    string UserEmail,
    string UserFullName
)
{
    public static ProjectAccessDto FromEntity(Projectuser projectuser) =>
        new(
            projectuser.Projectid,
            projectuser.Userid,
            projectuser.User.Email,
            projectuser.User.Fullname
        );
}