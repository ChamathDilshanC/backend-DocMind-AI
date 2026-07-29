using DocumentAssistant.Application.Common.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocumentAssistant.Infrastructure.Auth;

public class GoogleTokenValidator(IOptions<GoogleAuthOptions> options, ILogger<GoogleTokenValidator> logger)
    : IGoogleTokenValidator
{
    private readonly GoogleAuthOptions _options = options.Value;

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_options.ClientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            return new GoogleUserInfo(payload.Subject, payload.Email, payload.Name, payload.EmailVerified, payload.Picture);
        }
        catch (InvalidJwtException ex)
        {
            logger.LogWarning(ex, "Google ID token validation failed");
            return null;
        }
    }
}
