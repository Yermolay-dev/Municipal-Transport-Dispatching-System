using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTDS.Api.Features.Errors;

namespace MTDS.Api.Features.Auth.Register;

[ApiController]
[Route("api/v1/auth/register")]
[Authorize(Roles = "Admin")]
public class RegisterEndpoint : ControllerBase
{

    private readonly IMediator _mediator;

    public RegisterEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterQuery command)
    {
        var result = await _mediator.Send(command);
        if (!result)
            return ErrorHandler.ForCode(400);
        return Created();
    }
}