using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Entities;

namespace DocumentAssistant.Application.Features.Auth.Common;

/// <summary>Shared by Register/Login/GoogleSignIn/Refresh handlers so token issuance stays in one place.</summary>
public static class TokenIssuer
{
    public static async Task<AuthResultDto> IssueAsync(
        User user, IJwtTokenGenerator jwtTokenGenerator, IApplicationDbContext context,
        string? createdByIp, CancellationToken cancellationToken)
    {
        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);
        var rawRefreshToken = jwtTokenGenerator.GenerateRefreshToken();

        context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = jwtTokenGenerator.HashToken(rawRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = createdByIp
        });

        await context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            accessToken.Token,
            accessToken.ExpiresAt,
            rawRefreshToken,
            new UserDto(user.Id, user.Name, user.Email, user.Role.ToString(), user.AvatarUrl));
    }
}
