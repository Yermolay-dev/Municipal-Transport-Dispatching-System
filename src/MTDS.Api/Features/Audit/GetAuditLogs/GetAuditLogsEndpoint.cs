using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;

namespace MTDS.Api.Features.Audit.GetAuditLogs;

[ApiController]
[Route("api/v1/audit")]
[Authorize(Roles = "Admin")]
public class GetAuditLogs : ControllerBase
{
    private readonly IMediator _mediator;

    public GetAuditLogs(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs()
    {
        var query = new GetAuditLogsQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}