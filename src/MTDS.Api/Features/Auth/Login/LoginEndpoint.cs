using Microsoft.AspNetCore.Mvc;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Features.Errors;

namespace MTDS.Api.Features.Auth.Login;

[ApiController]
[Route("api/v1/auth/login")]
public class LoginEndpoint : ControllerBase
{
    private readonly IMediator _mediator;

    public LoginEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        var query = new LoginQuery
        {
            Email = req.Email,
            Password = req.Password
        };

        var result = _mediator.Send(query);
        if (result == null)
            return ErrorHandler.ForCode(401);

        return Ok(result);
    }
}