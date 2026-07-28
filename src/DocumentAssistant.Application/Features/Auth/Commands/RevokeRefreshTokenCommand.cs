using DocumentAssistant.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Auth.Commands;

/// <summary>Logout: revokes the presented refresh token so it can no longer be used to mint new access tokens.</summary>
public record RevokeRefreshTokenCommand(string RefreshToken, string? IpAddress) : IRequest;

public class RevokeRefreshTokenCommandHandler(IApplicationDbContext context, IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<RevokeRefreshTokenCommand>
{
    public async Task Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenGenerator.HashToken(request.RefreshToken);
        var token = await context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token is not null && token.RevokedAt is null)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = request.IpAddress;
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
