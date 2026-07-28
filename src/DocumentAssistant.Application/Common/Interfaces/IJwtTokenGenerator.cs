using DocumentAssistant.Domain.Entities;

namespace DocumentAssistant.Application.Common.Interfaces;

public record AccessTokenResult(string Token, DateTime ExpiresAt);

public interface IJwtTokenGenerator
{
    AccessTokenResult GenerateAccessToken(User user);

    /// <summary>Returns the raw opaque refresh token (caller stores only its hash).</summary>
    string GenerateRefreshToken();
    string HashToken(string rawToken);
}
