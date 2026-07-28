using DocumentAssistant.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentAssistant.API.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "Admin")]
public class AdminController(ISender sender) : ControllerBase
{
    [HttpGet("statistics")]
    public async Task<ActionResult<StatisticsDto>> GetStatistics(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetStatisticsQuery(), cancellationToken));
    }
}
