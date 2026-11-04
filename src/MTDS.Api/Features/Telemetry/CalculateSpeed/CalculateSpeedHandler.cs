using System.Security.Cryptography;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Options;
using MTDS.Api.Infrastructure.Configuration;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Infrastructure.Entities;
namespace MTDS.Api.Features.Telemetry.CalculateSpeed;

public class CalculateSpeedHandler : IRequestHandler<GetOptimalSpeedQuery, OptimalSpeedResponse>
{
    private readonly AppDbContext _db;
    private readonly TransportSettings _settings;
    private double _maxAllowedBusSpeed;
    private double _maxAllowedTramSpeed;
    private double _minAllowedSpeed;
    public CalculateSpeedHandler(AppDbContext db, IOptions<TransportSettings> settings)
    {
        _db = db;
        _settings = settings.Value;
        _maxAllowedBusSpeed = _settings.MaxBusSpeedKmh;
        _maxAllowedTramSpeed = _settings.MaxTramSpeedKmh;
        _minAllowedSpeed = _settings.MinComfortSpeedKmh;
    }

    public async Task<OptimalSpeedResponse> Handle(GetOptimalSpeedQuery req, CancellationToken cancel)
    {
        var assigments = await _db.TransportAssigments.Include(a => a.Route).ThenInclude(r => r.RouteStops).ThenInclude(rs => rs.Stop).FirstOrDefaultAsync(a => a.TransportId == req.TransportId && a.ShiftEnd == null, cancel);

        if (assigments == null)
            return new OptimalSpeedResponse
            {
                RecommendedSpeedKmh = 0,
                Status = "НЕ НА МАРШРУТЕ"
            };
        var currentTime = DateTime.UtcNow;
        var shiftStartTime = currentTime.Date.AddHours(10);
        var nextStop = assigments.Route.RouteStops.OrderBy(rs => rs.StopOrder).FirstOrDefault(rs => shiftStartTime.AddMinutes(rs.MinutesFromStart) > currentTime);
        if (nextStop == null)
            return new OptimalSpeedResponse
            {
                RecommendedSpeedKmh = 0,
                Status = "МАРШРУТ ЗАВЕРШЕН"
            };

        double distanceKm = CalculateDistance(req.CurrentLat, req.CurrentLon, nextStop.Stop.Lat, nextStop.Stop.Lon);
        var targetTime = DateTime.UtcNow.Date.AddHours(10).AddMinutes(nextStop.MinutesFromStart);
        double hourseLeft = (targetTime - DateTime.UtcNow).TotalHours;
        if (hourseLeft <= 0)
            return new OptimalSpeedResponse
            {
                RecommendedSpeedKmh = _maxAllowedBusSpeed,
                Status = "СИЛЬНО ОПАЗДЫВАЕТ"
            };

        double optimalSpeed = distanceKm / hourseLeft;
        double currAllowedSpeed = assigments.Transport?.Type == "tram" ? _maxAllowedTramSpeed : _maxAllowedBusSpeed;
        if (optimalSpeed > currAllowedSpeed)
            optimalSpeed = currAllowedSpeed;
        string status = optimalSpeed < _minAllowedSpeed ? "СПЕШИТ (СБАВЬ СКОРОСТЬ)" : "В ГРАФИКЕ";
        return new OptimalSpeedResponse
        {
            RecommendedSpeedKmh = Math.Round(optimalSpeed, 1),
            Status = status
        };
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double angle) => Math.PI * angle / 180.0;
}