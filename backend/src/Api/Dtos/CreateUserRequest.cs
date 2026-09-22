namespace Api.Dtos;

public sealed record CreateUserRequest(string Email, string FullName, string Password);
