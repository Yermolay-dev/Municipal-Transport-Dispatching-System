using MTDS.Api.Features.TransportLayer;
using MTDS.Api.Features.Errors;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace MTDS.Api.Features.TransportLayer;


public class GetRouteQuery : IRequest<TransportLayerResponse?>
{
    public string PathId { get; set; } = string.Empty;
}

public class GetActiveRoutesQuery : IRequest<List<TransportLayerSummary>>
{

}


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
    public async Task<IActionResult> GetTransportLayerActive()
    {
        var query = new GetActiveRoutesQuery();
        var result = await _mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTransportLayerById(string id)
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