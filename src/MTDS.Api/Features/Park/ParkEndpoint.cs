using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;
namespace MTDS.Api.Features.GetPark;

[Authorize(Roles = "Admin, Dispatcher")]
[ApiController]
[Route("api/v1/park")]
public class GetParkEndpoint : ControllerBase
{
    private readonly IMediator _mediator;

    public GetParkEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetPark()
    {
        var query = new GetParkQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddVehicle([FromBody] AddVehicleCommand command, CancellationToken cancel)
    {
        var res = await _mediator.Send(command, cancel);
        if (!res)
            return BadRequest();
        return Created();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteVehicle(int id, CancellationToken cancel)
    {
        var command = new DeleteVehicleCommand
        {
            Id = id
        };
        var res = await _mediator.Send(command, cancel);
        if (!res)
            return NotFound();
        return Ok();
    }
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutVehicle(int id, [FromBody] PutVehicleCommand command, CancellationToken cancel)
    {
        command.Id = id;
        var res = await _mediator.Send(command, cancel);
        if (!res)
            return NotFound();
        return Ok();

    }
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin, Dispatcher")]
    public async Task<IActionResult> PatchVehicle(int id, [FromBody] PatchVehicleCommand command, CancellationToken cancel)
    {
        command.Id = id;
        var res = await _mediator.Send(command, cancel);
        if (!res)
            return NotFound();
        return Ok();

    }
}