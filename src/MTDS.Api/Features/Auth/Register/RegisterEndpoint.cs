using MediatR;
using Microsoft.AspNetCore.Mvc;
using MTDS.Api.Features.Errors;

namespace MTDS.Api.Features.Auth.Register;

[ApiController]
[Route("api/v1/auth/register")]
public class RegisterEndpoint : ControllerBase
{

    private readonly IMediator _mediator;

    public RegisterEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        var query = new RegisterQuery
        {
            Role = req.Role,
            PhotoUrl = req.PhotoUrl,
            Name = req.Name,
            SurName = req.SurName,
            Email = req.Email,
            Password = req.Password,
            PhoneNumber = req.PhoneNumber,
            DriverLicenseNumber = req.DriverLicenseNumber,

        };
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Request send to handler!");
        Console.ResetColor();
        var result = await _mediator.Send(query);
        if (result == null)
            return ErrorHandler.ForCode(400);
        return StatusCode(201, result);
    }
}