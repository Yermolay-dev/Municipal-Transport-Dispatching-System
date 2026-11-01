using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Features.GetRoute;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace MTDS.Api.Features.GetRoute;

public class GetRouteHandler : IRequestHandler<GetRouteQuery, GetRouteResponse?>
{
    private readonly AppDbContext _db;

    public GetRouteHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetRouteResponse?> Handle(GetRouteQuery request, CancellationToken cancellationToken)
    {
        var route = await _db.Routes.Include(r => r.RouteStops).ThenInclude(rs => rs.Stop).FirstOrDefaultAsync(r => r.PathNumber == request.PathId, cancellationToken);

        if (route == null)
            return null;

        var coordinates = Array.Empty<double[]>();
        if (!string.IsNullOrEmpty(route.RouteLineJson))
            coordinates = JsonSerializer.Deserialize<double[][]>(route.RouteLineJson);


        var scheduleList = new List<StopStruct>();

        var orderedStops = route.RouteStops.OrderBy(rs => rs.StopOrder).ToList();

        var routeStartTime = DateTime.UtcNow.Date.AddHours(10);
        foreach (var rs in orderedStops)
        {
            scheduleList.Add(new StopStruct
            {
                StopName = rs.Stop.Name,
                Lat = rs.Stop.Lat,
                Lon = rs.Stop.Lon,
                ArrivalTime = routeStartTime.AddMinutes(rs.MinutesFromStart)
            });
        }
        return new GetRouteResponse
        {
            TransportType = route.TransportType,
            PathId = route.PathNumber,
            RouteLine = coordinates!,
            Schedule = scheduleList
        };
    }

}


public class GetRoutesActiveHandler : IRequestHandler<GetActiveRouteQuery, List<GetRouteSummary>?>
{
    private readonly AppDbContext _db;

    public GetRoutesActiveHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<GetRouteSummary>?> Handle(GetActiveRouteQuery query, CancellationToken cancel)
    {
        var activeRoutes = await _db.Routes.ToListAsync(cancel);

        var activeRouteList = new List<GetRouteSummary>();
        foreach (var rs in activeRoutes)
        {
            activeRouteList.Add(new GetRouteSummary
            {
                TransportType = rs.TransportType,
                PathId = rs.PathNumber
            });
        }
        return activeRouteList;
    }
}