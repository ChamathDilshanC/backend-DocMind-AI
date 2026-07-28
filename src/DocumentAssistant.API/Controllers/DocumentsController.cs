using DocumentAssistant.Application.Features.Documents.Commands;
using DocumentAssistant.Application.Features.Documents.Common;
using DocumentAssistant.Application.Features.Documents.Queries;
using DocumentAssistant.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentAssistant.API.Controllers;

public record RenameDocumentRequest(string NewName);

[ApiController]
[Route("api/documents")]
[Authorize]
public class DocumentsController(ISender sender) : ControllerBase
{
    [HttpPost("upload")]
    [RequestSizeLimit(60_000_000)]
    public async Task<ActionResult<DocumentDto>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        await using var stream = file.OpenReadStream();
        var result = await sender.Send(new UploadDocumentCommand(file.FileName, file.Length, stream), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedList<DocumentDto>>> GetAll(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        return Ok(await sender.Send(new GetDocumentsQuery(pageNumber, pageSize), cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetDocumentByIdQuery(id), cancellationToken));
    }

    [HttpGet("{id:guid}/pages")]
    public async Task<ActionResult<IReadOnlyList<DocumentPageDto>>> GetPages(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetDocumentPagesQuery(id), cancellationToken));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DownloadDocumentQuery(id), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> Rename(Guid id, RenameDocumentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new RenameDocumentCommand(id, request.NewName), cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteDocumentCommand(id), cancellationToken);
        return NoContent();
    }
}
