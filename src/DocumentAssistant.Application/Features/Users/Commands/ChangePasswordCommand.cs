using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Users.Commands;

public record ChangePasswordCommand(string? CurrentPassword, string NewPassword) : IRequest;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

public class ChangePasswordCommandHandler(
    IApplicationDbContext context, ICurrentUserService currentUserService, IPasswordHasher passwordHasher)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Entities.User), userId);

        // Accounts with an existing password must prove they know it; Google-only accounts are setting one for the first time.
        if (user.PasswordHash is not null)
        {
            if (request.CurrentPassword is null || !passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedException("Current password is incorrect.");
            }
        }

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        await context.SaveChangesAsync(cancellationToken);
    }
}
