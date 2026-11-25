using MTDS.Api.Features.Errors;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;
using System.Security.Claims;

namespace MTDS.Api.Features.Profile;

[ApiController]
[Route("api/v1/profile")]
[Authorize(Roles = "Dispatcher, Admin")]
public class ProfileEndpoint : ControllerBase
{

    private readonly IMediator _mediator;

    private Guid? GetUserIdFromToken()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(userIdStr, out var userId) ? userId : null;
    }

    public ProfileEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }


    [HttpGet]
    public async Task<IActionResult> GetProfile(CancellationToken cancel)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();
        var query = new GetProfileQuery { Id = userId.Value };
        var result = await _mediator.Send(query, cancel);
        return Ok(result);
    }
    [HttpPut]
    public async Task<IActionResult> PutProfile([FromBody] PutProfileCommand command, CancellationToken cancel)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();
        command.Id = userId.Value;
        await _mediator.Send(command, cancel);
        return Ok();
    }
    [HttpPatch("password")]
    public async Task<IActionResult> PatchProfile([FromBody] PatchProfileCommand command, CancellationToken cancel)
    {
        var userId = GetUserIdFromToken();
        if (userId == null)
            return Unauthorized();
        command.Id = userId.Value;
        var res = await _mediator.Send(command, cancel);
        if (!res)
            return BadRequest();
        return Ok();
    }
}