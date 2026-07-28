namespace DocumentAssistant.Infrastructure.Auth;

public class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    public string ClientId { get; set; } = string.Empty;
}
