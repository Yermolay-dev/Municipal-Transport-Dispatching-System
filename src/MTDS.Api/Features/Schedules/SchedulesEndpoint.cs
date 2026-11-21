using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace MTDS.Api.Features.Schedules;

[ApiController]
[Route("api/v1/assignments")]
[Authorize]
public class SchedulesEndpoint : ControllerBase
{
    private readonly IMediator _mediator;

    public SchedulesEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedules(CancellationToken cancel)
    {
        var query = new GetSchedulesQuery { };
        var res = await _mediator.Send(query, cancel);
        return Ok(res);
    }
    [HttpPost]
    public async Task<IActionResult> AddSchedules([FromBody] AddSchedulesCommand command, CancellationToken cancel)
    {
        bool res = await _mediator.Send(command, cancel);
        if (!res)
            return NotFound();
        return Ok();
    }
    [HttpPost("{id}/end")]
    public async Task<IActionResult> Add(int id, CancellationToken cancel)
    {
        var command = new AddSchedulesEndCommand { Id = id };
        bool res = await _mediator.Send(command, cancel);
        if (!res)
            return NotFound();
        return Ok();
    }
}