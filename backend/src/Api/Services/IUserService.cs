using Api.Dtos;

namespace Api.Services;

public interface IUserService
{
    Task<IEnumerable<UserDto>> ListAsync(bool activeOnly = false, CancellationToken ct = default);
    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(UpdateUserRequest request, CancellationToken ct = default);
    Task<UserDto> SetActiveAsync(int userId, bool isActive, CancellationToken ct = default);
}
