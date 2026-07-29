namespace DocumentAssistant.Application.Common.Interfaces;

public record GoogleUserInfo(string Subject, string Email, string Name, bool EmailVerified, string? Picture);

public interface IGoogleTokenValidator
{
    /// <summary>Validates a Google-issued ID token server-side. Returns null if invalid/expired.</summary>
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}
