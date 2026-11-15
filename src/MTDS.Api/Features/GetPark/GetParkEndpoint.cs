using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
}