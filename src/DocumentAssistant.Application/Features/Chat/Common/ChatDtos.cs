using DocumentAssistant.Application.Common.Models;

namespace DocumentAssistant.Application.Features.Chat.Common;

public record ConversationDto(Guid Id, string Title, DateTime CreatedAt, DateTime UpdatedAt);

public record ConversationSummaryDto(Guid Id, string Title, string? LastMessagePreview, DateTime UpdatedAt);

public record MessageDto(Guid Id, string Role, string Content, IReadOnlyList<CitationDto>? Citations, DateTime CreatedAt);

public record ConversationDetailDto(Guid Id, string Title, IReadOnlyList<MessageDto> Messages);

/// <summary>Confidence is a heuristic based on the top search result's similarity score, not a calibrated ML score.</summary>
public record AskQuestionResultDto(
    Guid ConversationId, Guid MessageId, string Answer, IReadOnlyList<CitationDto> Citations, string Confidence);
