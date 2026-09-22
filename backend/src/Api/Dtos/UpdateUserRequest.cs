namespace Api.Dtos;

public record UpdateUserRequest(int UserId, string Email, string FullName);
