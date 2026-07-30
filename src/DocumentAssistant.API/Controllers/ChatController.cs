using DocumentAssistant.Application.Features.Chat.Commands;
using DocumentAssistant.Application.Features.Chat.Common;
using DocumentAssistant.Application.Features.Chat.Queries;
using DocumentAssistant.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DocumentAssistant.API.Controllers;

public record AskQuestionRequest(Guid? ConversationId, Guid? DocumentId, string Question);
public record CreateConversationRequest(string? Title);

[ApiController]
[Route("api/chat")]
[Authorize]
[EnableRateLimiting("chat")]
public class ChatController(ISender sender) : ControllerBase
{
    [HttpPost("ask")]
    public async Task<ActionResult<AskQuestionResultDto>> Ask(AskQuestionRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AskQuestionCommand(request.ConversationId, request.DocumentId, request.Question), cancellationToken);
        return Ok(result);
    }

    [HttpPost("conversations")]
    public async Task<ActionResult<ConversationDto>> CreateConversation(CreateConversationRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new CreateConversationCommand(request.Title), cancellationToken));
    }

    [HttpDelete("conversations/{conversationId:guid}")]
    public async Task<IActionResult> DeleteConversation(Guid conversationId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteConversationCommand(conversationId), cancellationToken);
        return NoContent();
    }

    [HttpDelete("conversations")]
    public async Task<IActionResult> DeleteAllConversations(CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteAllConversationsCommand(), cancellationToken);
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<ActionResult<PaginatedList<ConversationSummaryDto>>> GetHistory(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await sender.Send(new GetChatHistoryQuery(pageNumber, pageSize), cancellationToken));
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult<ConversationDetailDto>> GetConversation(Guid conversationId, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetConversationByIdQuery(conversationId), cancellationToken));
    }
}
