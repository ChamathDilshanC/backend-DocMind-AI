namespace DocumentAssistant.Application.Features.Auth.Common;

public record UserDto(Guid Id, string Name, string Email, string Role);

public record AuthResultDto(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, UserDto User);
