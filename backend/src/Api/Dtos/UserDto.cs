using Api.Models;

namespace Api.Dtos;

public record UserDto(
    int Id,
    string Email,
    string FullName,
    bool IsActive,
    ICollection<string> Roles
)
{
    public static UserDto FromEntity(User user, ICollection<string> roles) =>
        new(user.Userid, user.Email, user.Fullname, user.Isactive, roles);
}
