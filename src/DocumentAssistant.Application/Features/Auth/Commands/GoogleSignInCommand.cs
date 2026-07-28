using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Features.Auth.Common;
using DocumentAssistant.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Auth.Commands;

public record GoogleSignInCommand(string IdToken, string? IpAddress) : IRequest<AuthResultDto>;

public class GoogleSignInCommandValidator : AbstractValidator<GoogleSignInCommand>
{
    public GoogleSignInCommandValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}

public class GoogleSignInCommandHandler(
    IApplicationDbContext context, IGoogleTokenValidator googleTokenValidator, IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<GoogleSignInCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(GoogleSignInCommand request, CancellationToken cancellationToken)
    {
        var googleUser = await googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken)
            ?? throw new UnauthorizedException("Invalid Google sign-in token.");

        var normalizedEmail = googleUser.Email.Trim().ToLowerInvariant();

        // Find by GoogleId first, then fall back to linking an existing local account by email.
        var user = await context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleUser.Subject, cancellationToken)
            ?? await context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Name = googleUser.Name,
                Email = normalizedEmail,
                GoogleId = googleUser.Subject,
                EmailVerified = googleUser.EmailVerified
            };
            context.Users.Add(user);
        }
        else if (user.GoogleId is null)
        {
            user.GoogleId = googleUser.Subject;
            if (googleUser.EmailVerified) user.EmailVerified = true;
        }

        await context.SaveChangesAsync(cancellationToken);

        return await TokenIssuer.IssueAsync(user, jwtTokenGenerator, context, request.IpAddress, cancellationToken);
    }
}
