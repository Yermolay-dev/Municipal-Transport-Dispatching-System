using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;


namespace MTDS.Api.Features.Drivers;


[ApiController]
[Route("api/v1/drivers")]
public class DriversEndpoint : ControllerBase
{
    private readonly IMediator _mediator;

    public DriversEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Roles = "Dispatcher, Admin")]
    public async Task<IActionResult> GetDrivers(CancellationToken cancel)
    {
        var query = new GetDriversQuery();
        var result = await _mediator.Send(query, cancel);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateDriver([FromBody] CreateDriverCommand command, CancellationToken cancel)
    {
        var result = await _mediator.Send(command, cancel);
        if (!result)
            return BadRequest();

        return Created("", result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateDriver(Guid id, [FromBody] UpdateDriverCommand command, CancellationToken cancel)
    {
        command.Id = id;
        var result = await _mediator.Send(command, cancel);
        if (!result)
            return BadRequest();

        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteDriver(Guid id, CancellationToken cancel)
    {
        var command = new DeleteDriverCommand { Id = id };
        var result = await _mediator.Send(command, cancel);
        if (!result)
            return BadRequest();

        return Ok(result);
    }
}