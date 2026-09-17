namespace Api.Dtos;

public sealed record LoginResponse(string Token, int UserId, string Email, string FullName);
