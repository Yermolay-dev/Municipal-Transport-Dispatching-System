using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace MTDS.Api.Features.Telemetry.CalculateSpeed;

[ApiController]
[Route("api/v1/telemetry/optimal-speed")]
public class CalculateSpeedEndpoint : ControllerBase
{
    private readonly IMediator _mediator;

    public CalculateSpeedEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }
    [HttpGet]
    public async Task<IActionResult> GetSpeed([FromQuery] GetOptimalSpeedQuery query)
    {
        var result = await _mediator.Send(query);
        return Ok(result);
    }
}