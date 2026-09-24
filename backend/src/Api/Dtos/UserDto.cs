using Api.Models;

namespace Api.Dtos;

public record UserDto(int Id, string Email, string FullName, bool IsActive)
{
    public static UserDto FromEntity(User user) =>
        new(user.Userid, user.Email, user.Fullname, user.Isactive);
}
