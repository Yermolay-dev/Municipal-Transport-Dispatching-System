using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace MTDS.Api.Features.Telemetry.ReceiveGps;



[ApiController]
[Route("api/v1/telemetry")]
public class ReceiveGpsEndpoint : ControllerBase
{
    private readonly IMediator _mediator;

    public ReceiveGpsEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpPost]
    public async Task<IActionResult> GetTelemetry([FromBody] ReceiveGpsQuery query)
    {
        var result = await _mediator.Send(query);
        if (!result)
            return StatusCode(500);
        return Ok();

    }
}