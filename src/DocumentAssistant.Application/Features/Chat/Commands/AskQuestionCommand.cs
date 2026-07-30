using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DocumentAssistant.Application.Common.Exceptions;
using DocumentAssistant.Application.Common.Interfaces;
using DocumentAssistant.Application.Common.Models;
using DocumentAssistant.Application.Features.Chat.Common;
using DocumentAssistant.Domain.Entities;
using DocumentAssistant.Domain.Enums;
using DocumentAssistant.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DocumentAssistant.Application.Features.Chat.Commands;

public record AskQuestionCommand(Guid? ConversationId, Guid? DocumentId, string Question) : IRequest<AskQuestionResultDto>;

public class AskQuestionCommandValidator : AbstractValidator<AskQuestionCommand>
{
    public AskQuestionCommandValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(4000);
    }
}

public class AskQuestionCommandHandler(
    IApplicationDbContext context,
    IEmbeddingService embeddingService,
    IVectorStoreService vectorStoreService,
    IPromptBuilder promptBuilder,
    IAnswerGenerationService answerGenerationService,
    INotificationService notificationService,
    ICacheService cacheService,
    ICurrentUserService currentUserService,
    ILogger<AskQuestionCommandHandler> logger)
    : IRequestHandler<AskQuestionCommand, AskQuestionResultDto>
{
    private const int TopK = 5;
    private const float LowConfidenceThreshold = 0.5f;

    public async Task<AskQuestionResultDto> Handle(AskQuestionCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId ?? throw new UnauthorizedException("Not authenticated.");

        var conversation = await ResolveConversationAsync(request, userId, cancellationToken);

        context.Messages.Add(new Message { ConversationId = conversation.Id, Role = MessageRole.User, Content = request.Question });
        await context.SaveChangesAsync(cancellationToken);

        var questionEmbedding = await GetQuestionEmbeddingAsync(request.Question, cancellationToken);

        var searchResults = await vectorStoreService.SearchAsync(questionEmbedding, userId, request.DocumentId, TopK, cancellationToken);

        var chunkIds = searchResults.Select(r => r.ChunkId).ToList();
        var chunkTextById = await context.Chunks
            .Where(c => chunkIds.Contains(c.EmbeddingId))
            .ToDictionaryAsync(c => c.EmbeddingId, c => c.Text, cancellationToken);

        var sources = searchResults
            .Select(r => new SourceChunk(r.Filename, r.Page, chunkTextById.GetValueOrDefault(r.ChunkId, string.Empty)))
            .ToList();

        var citations = searchResults
            .Select(r => new CitationDto(r.DocumentId, r.Filename, r.Page, Truncate(chunkTextById.GetValueOrDefault(r.ChunkId, string.Empty), 200)))
            .ToList();

        var confidence = searchResults.Count > 0 && searchResults[0].Score >= LowConfidenceThreshold ? "High" : "Low";

        var systemPrompt = promptBuilder.BuildSystemPrompt(sources);
        var history = await GetRecentHistoryAsync(conversation.Id, cancellationToken);

        var cacheKey = BuildResponseCacheKey(userId, request.Question, chunkIds);
        var cachedAnswer = await cacheService.GetAsync<string>(cacheKey, cancellationToken);

        var messageId = Guid.NewGuid();
        string answer;

        if (cachedAnswer is not null)
        {
            answer = cachedAnswer;
            await notificationService.SendChatTokenAsync(userId, conversation.Id, messageId, answer, cancellationToken);
        }
        else
        {
            var answerBuilder = new StringBuilder();
            await foreach (var token in answerGenerationService.StreamCompletionAsync(systemPrompt, history, request.Question, cancellationToken))
            {
                answerBuilder.Append(token);
                await notificationService.SendChatTokenAsync(userId, conversation.Id, messageId, token, cancellationToken);
            }

            answer = answerBuilder.ToString();
            await cacheService.SetAsync(cacheKey, answer, TimeSpan.FromHours(1), cancellationToken);
        }

        context.Messages.Add(new Message
        {
            Id = messageId,
            ConversationId = conversation.Id,
            Role = MessageRole.Assistant,
            Content = answer,
            CitationsJson = JsonSerializer.Serialize(citations)
        });

        // This is the first exchange in the conversation — replace the placeholder
        // "New conversation" title with one generated from what was actually asked.
        if (history.Count == 0)
        {
            var generatedTitle = await TryGenerateTitleAsync(request.Question, cancellationToken);
            if (generatedTitle is not null)
            {
                conversation.Title = generatedTitle;
            }
        }

        conversation.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        await notificationService.SendChatCompletedAsync(userId, conversation.Id, messageId, cancellationToken);

        return new AskQuestionResultDto(conversation.Id, messageId, answer, citations, confidence);
    }

    private async Task<Conversation> ResolveConversationAsync(AskQuestionCommand request, Guid userId, CancellationToken cancellationToken)
    {
        if (request.ConversationId is not null)
        {
            var existing = await context.Conversations.FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken)
                ?? throw new NotFoundException(nameof(Conversation), request.ConversationId);

            if (existing.UserId != userId) throw new ForbiddenAccessException();
            return existing;
        }

        var conversation = new Conversation { UserId = userId, Title = Truncate(request.Question, 80) };
        context.Conversations.Add(conversation);
        await context.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    private async Task<float[]> GetQuestionEmbeddingAsync(string question, CancellationToken cancellationToken)
    {
        var cacheKey = $"emb:{Sha256Hex(question.Trim().ToLowerInvariant())}";
        var cached = await cacheService.GetAsync<float[]>(cacheKey, cancellationToken);
        if (cached is not null) return cached;

        var embedding = await embeddingService.GenerateEmbeddingAsync(question, cancellationToken);
        await cacheService.SetAsync(cacheKey, embedding, TimeSpan.FromHours(24), cancellationToken);
        return embedding;
    }

    private async Task<List<ChatTurn>> GetRecentHistoryAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        const int maxTurns = 10;

        var messages = await context.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(maxTurns + 1) // +1 to skip the just-added user question itself
            .ToListAsync(cancellationToken);

        return messages
            .Skip(1)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new ChatTurn(m.Role.ToString(), m.Content))
            .ToList();
    }

    private static string BuildResponseCacheKey(Guid userId, string question, IEnumerable<Guid> chunkIds) =>
        $"chat:{userId}:{Sha256Hex(question.Trim().ToLowerInvariant() + string.Join(',', chunkIds.OrderBy(c => c)))}";

    private static string Sha256Hex(string input) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));

    private static string Truncate(string text, int maxLength) => text.Length <= maxLength ? text : text[..maxLength] + "...";

    // Reuses the answer-generation service for a one-off, non-streamed completion — the
    // tokens are consumed locally and never sent to SendChatTokenAsync, so they don't
    // leak into the visible chat message. Best-effort: a failure here just keeps the
    // conversation's placeholder title instead of breaking the actual answer.
    private async Task<string?> TryGenerateTitleAsync(string question, CancellationToken cancellationToken)
    {
        const string titlePrompt =
            "Generate a short, specific title (3 to 6 words) that summarizes what this conversation " +
            "will be about, based on the user's first question. Reply with only the title text — no " +
            "quotes, no punctuation at the end, no explanation.";

        try
        {
            var builder = new StringBuilder();
            await foreach (var token in answerGenerationService.StreamCompletionAsync(titlePrompt, [], question, cancellationToken))
            {
                builder.Append(token);
            }

            var title = builder.ToString().Trim().Trim('"', '\'', '.', '\n', '\r').Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                return null;
            }

            return title.Length > 100 ? title[..100] : title;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to auto-generate a conversation title; keeping the default.");
            return null;
        }
    }
}
