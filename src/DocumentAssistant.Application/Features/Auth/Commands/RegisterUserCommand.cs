using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Features.Auth.Common;
using DocumentAssistant.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DocumentAssistant.Application.Features.Auth.Commands;

public record RegisterUserCommand(string Name, string Email, string Password, string? IpAddress) : IRequest<AuthResultDto>;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
    }
}

public class RegisterUserCommandHandler(
    IApplicationDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<RegisterUserCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var exists = await context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (exists)
        {
            throw new ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.Email), "An account with this email already exists.")]);
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.Hash(request.Password),
            EmailVerified = false
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        return await TokenIssuer.IssueAsync(user, jwtTokenGenerator, context, request.IpAddress, cancellationToken);
    }
}
