using DocumentAssistant.Application.Features.Embeddings.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentAssistant.API.Controllers;

public record CreateEmbeddingsRequest(Guid DocumentId);

[ApiController]
[Route("api/embeddings")]
[Authorize]
public class EmbeddingsController(ISender sender) : ControllerBase
{
    [HttpPost("create")]
    public async Task<IActionResult> Create(CreateEmbeddingsRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new CreateEmbeddingsCommand(request.DocumentId), cancellationToken);
        return Accepted();
    }

    [HttpDelete("{documentId:guid}")]
    public async Task<IActionResult> Delete(Guid documentId, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteEmbeddingsCommand(documentId), cancellationToken);
        return NoContent();
    }
}
