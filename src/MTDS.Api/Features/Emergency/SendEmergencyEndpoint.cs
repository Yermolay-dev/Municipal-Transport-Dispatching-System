using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using System.Security.Claims;
using MTDS.Api.Features.Errors;

namespace MTDS.Api.Features.Emergency;


[ApiController]
[Route("api/v1/emergency")]
[Authorize(Roles = "Admin")]
public class SendEmergencyEndpoint : ControllerBase
{
    private readonly IMediator _mediator;

    public SendEmergencyEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand([FromBody] SendEmergencyCommand command)
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

        if (Guid.TryParse(userIdString, out Guid dispatcherId))
            command.DispatcherId = dispatcherId;
        else
            return Unauthorized("Не удалось определить ID диспетчера из токена.");

        var result = await _mediator.Send(command);
        if (!result)
            return ErrorHandler.ForCode(404);
        return Ok(new { message = "Транспорт экстренно остановлен, логи записаны." });
    }
}