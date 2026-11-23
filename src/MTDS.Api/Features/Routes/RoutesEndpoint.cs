using MTDS.Api.Features.Errors;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;

namespace MTDS.Api.Features.Routes;


[ApiController]
[Route("api/v1/routes")]
public class TransportLayerEndpoint : ControllerBase
{

    private readonly IMediator _mediator;

    public TransportLayerEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpGet("active")]
    public async Task<IActionResult> GetRouteActive(CancellationToken cancel)
    {
        var query = new GetActiveRouteQuery();
        var result = await _mediator.Send(query, cancel);
        return Ok(result);
    }

    [HttpGet("active/{id}")]
    public async Task<IActionResult> GetRouteById(string id, CancellationToken cancel)
    {
        var query = new GetRouteQuery { PathId = id };
        var result = await _mediator.Send(query, cancel);
        if (result == null)
        {
            return ErrorHandler.ForCode(404);
        }
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AddRoute([FromBody] AddRouteCommand req, CancellationToken cancel)
    {
        await _mediator.Send(req, cancel);
        return Created();
    }


    [HttpPut("active/{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PutRoute(int id, [FromBody] PutRouteCommand command, CancellationToken cancel)
    {
        command.Id = id;
        bool res = await _mediator.Send(command, cancel);
        if (!res)
            return NotFound();
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRoute(int id, CancellationToken cancel)
    {
        var command = new DeleteRouteCommand { Id = id };
        bool res = await _mediator.Send(command, cancel);
        if (!res)
            return NotFound();
        return Ok();
    }
}