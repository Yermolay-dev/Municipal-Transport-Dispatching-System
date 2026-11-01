using MTDS.Api.Features.GetRoute;
using MTDS.Api.Features.Errors;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace MTDS.Api.Features.GetRoute;


[ApiController]
[Route("api/v1/path/active")]
class TransportLayerEndpoint : ControllerBase
{

    private readonly IMediator _mediator;

    public TransportLayerEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpGet]
    public async Task<IActionResult> GetRouteActive()
    {
        var query = new GetActiveRouteQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRouteById(string id)
    {
        var query = new GetRouteQuery { PathId = id };
        var result = await _mediator.Send(query);
        if (result == null)
        {
            return ErrorHandler.ForCode(404);
        }
        return Ok(result);
    }
}