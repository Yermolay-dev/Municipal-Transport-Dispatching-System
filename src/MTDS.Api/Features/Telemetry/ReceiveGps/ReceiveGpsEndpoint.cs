using Microsoft.AspNetCore.Mvc;

namespace MTDS.Api.Features.Telemetry.ReceiveGps;

public class ReceiveGpsRequest
{
    public string BusId { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double SpeedKmh { get; set; }
}


public class ReceiveGpsResponse
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;

}


[ApiController]
[Route("api/v1/telemetry")]
public class ReceiveGpsEndpoint : ControllerBase
{
    [HttpPost]
    public IActionResult Handle([FromBody] ReceiveGpsRequest request)
    {
        if (string.IsNullOrEmpty(request.BusId))
        {
            return BadRequest(new ReceiveGpsResponse
            {
                Status = 400,
                Message = "Ошибка: BusId не может быть пустым!"
            });
        }
        if (request.SpeedKmh < 0 || request.SpeedKmh > 150)
        {
            return BadRequest(new ReceiveGpsResponse
            {
                Status = 400,
                Message = "Ошибка: неадекватная скорость."
            });
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[TELEMETRY] Автобус {request.BusId} едет со скоростью {request.SpeedKmh} км/ч. Координаты: {request.Lat}, {request.Lon}");
        Console.ResetColor();
        return Ok(new ReceiveGpsResponse
        {
            Status = 200,
            Message = "Данные успешно приняты сервером!"
        });
    }
}