using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using MTDS.Api.Features.Errors;
using System.Text.Json;


namespace MTDS.Api.Features.Telemetry.ReceiveGps;


public class ReceiveGpsHandler : IRequestHandler<ReceiveGpsQuery, bool>
{
    private readonly IDistributedCache _cache;

    public ReceiveGpsHandler(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<bool> Handle(ReceiveGpsQuery req, CancellationToken cancel)
    {
        if (string.IsNullOrEmpty(req.TransportId))
        {
            return false;
        }
        if (req.SpeedKmh < 0 || req.SpeedKmh > 150)
        {
            return false;
        }
        string cacheKey = $"bus_location_{req.TransportId}";
        var locationData = new
        {
            req.Lat,
            req.Lon,
            req.SpeedKmh,
            UpdatedAt = DateTime.UtcNow
        };
        var json = JsonSerializer.Serialize(locationData);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
        await _cache.SetStringAsync(cacheKey, json, options, cancel);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"[CACHE] Обновлены координаты транспорта {req.TransportId}");
        Console.ResetColor();
        return true;
    }
}