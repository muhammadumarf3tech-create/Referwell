namespace ReferWell.Application.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string FullName, string Email, List<string> Roles, string? Title);
