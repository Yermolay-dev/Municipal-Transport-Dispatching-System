using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MTDS.Api.Features.Stops;

[ApiController]
[Route("api/v1/stops")]
[Authorize(Roles = "Admin")]
public class AddStopEndpoint : ControllerBase
{
    private readonly IMediator _mediator;

    public AddStopEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> AddStop([FromBody] AddStopQuery query)
    {
        await _mediator.Send(query);
        return Created();
    }

    [HttpGet]
    public async Task<IActionResult> GetStop()
    {
        var query = new GetStopQuery { };
        var res = await _mediator.Send(query);
        return Ok(res);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutStop(int id, [FromBody] PutStopCommand command, CancellationToken cancel)
    {
        command.Id = id;
        bool res = await _mediator.Send(command, cancel);
        if (!res)
            return NotFound();
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStop(int id, CancellationToken cancel)
    {
        var command = new DeleteStopCommand { Id = id };
        bool res = await _mediator.Send(command, cancel);
        if (!res)
            return BadRequest();
        return Ok();

    }
}