using DocumentAssistant.Domain.Common;
using DocumentAssistant.Domain.Enums;

namespace DocumentAssistant.Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public Conversation Conversation { get; set; } = null!;

    public MessageRole Role { get; set; }
    public string Content { get; set; } = string.Empty;

    /// <summary>Serialized List&lt;CitationDto&gt; — null for user messages.</summary>
    public string? CitationsJson { get; set; }
}
