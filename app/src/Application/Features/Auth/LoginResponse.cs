namespace RefaccionariaCuate.Application.Features.Auth;

public sealed record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt, string UserName, string Role, string FullName);
