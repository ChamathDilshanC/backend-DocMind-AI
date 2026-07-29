using DocumentAssistant.Domain.Common;
using DocumentAssistant.Domain.Enums;

namespace DocumentAssistant.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    /// <summary>Null for accounts that only ever signed in with Google.</summary>
    public string? PasswordHash { get; set; }

    /// <summary>Google "sub" claim. Null for local-only accounts. A row may have both this and PasswordHash set.</summary>
    public string? GoogleId { get; set; }

    /// <summary>Profile photo URL from the Google "picture" claim. Refreshed on every Google sign-in. Null for local-only accounts.</summary>
    public string? AvatarUrl { get; set; }

    public UserRole Role { get; set; } = UserRole.User;
    public bool EmailVerified { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Conversation> Conversations { get; set; } = new List<Conversation>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
