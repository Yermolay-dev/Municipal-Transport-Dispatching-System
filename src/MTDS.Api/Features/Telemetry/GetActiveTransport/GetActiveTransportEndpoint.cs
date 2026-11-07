using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace MTDS.Api.Features.Telemetry.GetActiveTransport;

[ApiController]
[Route("api/v1/transport/active")]
public class GetActiveTransportEndpoint : ControllerBase
{
    private readonly IMediator _mediator;

    public GetActiveTransportEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetTransport()
    {
        var query = new GetActiveTransportQuery { };
        var result = await _mediator.Send(query);
        if (result.Count == 0)
            return NoContent();
        return Ok(result);
    }
}