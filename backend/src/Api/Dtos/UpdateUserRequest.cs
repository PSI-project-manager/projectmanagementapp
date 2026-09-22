namespace Api.Dtos;

public sealed record UpdateUserRequest(int UserId, string Email, string FullName);
