using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Features.Schedules;
namespace MTDS.Api.Features.Auth.Staff;

[ApiController]
[Route("api/v1/auth/staff")]
public class GetStaffEndpoint : ControllerBase
{
    private readonly IMediator _mediator;

    public GetStaffEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetStaff(CancellationToken cancel)
    {
        var query = new GetStaffQuery { };
        var res = await _mediator.Send(query, cancel);
        return Ok(res);
    }

    [HttpPatch("{id:guid}/password")]
    public async Task<IActionResult> PatchStaff(Guid id, [FromBody] PatchStaffCommand command, CancellationToken cancel)
    {
        command.Id = id;
        var res = await _mediator.Send(command, cancel);
        if (!res)
            return NotFound();
        return Ok();
    }
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteStaff(Guid id, CancellationToken cancel)
    {
        var command = new DeleteStaffCommand { Id = id };
        var res = await _mediator.Send(command, cancel);
        if (!res)
            return NotFound();
        return Ok();
    }

}