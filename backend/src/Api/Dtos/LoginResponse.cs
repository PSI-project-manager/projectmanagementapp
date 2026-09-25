namespace Api.Dtos;

public record LoginResponse(string Token, int UserId, string Email, string FullName);
