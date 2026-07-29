using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Features.Auth.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentAssistant.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken, string? IpAddress) : IRequest<AuthResultDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public class RefreshTokenCommandHandler(
    IApplicationDbContext context, IJwtTokenGenerator jwtTokenGenerator, ILogger<RefreshTokenCommandHandler> logger)
    : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenGenerator.HashToken(request.RefreshToken);

        var existing = await context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (existing.RevokedAt is not null)
        {
            // Reuse of an already-rotated token: treat as compromised and revoke the whole family.
            logger.LogWarning("Refresh token reuse detected for user {UserId}", existing.UserId);
            await RevokeAllActiveTokensAsync(existing.UserId, request.IpAddress, cancellationToken);
            throw new UnauthorizedException("Refresh token has already been used. All sessions have been revoked.");
        }

        if (!existing.IsActive)
        {
            throw new UnauthorizedException("Refresh token has expired.");
        }

        var newRawToken = jwtTokenGenerator.GenerateRefreshToken();
        var newTokenHash = jwtTokenGenerator.HashToken(newRawToken);

        existing.RevokedAt = DateTime.UtcNow;
        existing.RevokedByIp = request.IpAddress;
        existing.ReplacedByTokenHash = newTokenHash;

        var accessToken = jwtTokenGenerator.GenerateAccessToken(existing.User);

        context.RefreshTokens.Add(new Domain.Entities.RefreshToken
        {
            UserId = existing.UserId,
            TokenHash = newTokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = request.IpAddress
        });

        await context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            accessToken.Token,
            accessToken.ExpiresAt,
            newRawToken,
            new UserDto(existing.User.Id, existing.User.Name, existing.User.Email, existing.User.Role.ToString(), existing.User.AvatarUrl));
    }

    private async Task RevokeAllActiveTokensAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var activeTokens = await context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
