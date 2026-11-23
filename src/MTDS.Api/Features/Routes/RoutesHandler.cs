using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Infrastructure.Entities;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace MTDS.Api.Features.Routes;

public class GetRouteHandler : IRequestHandler<GetRouteQuery, GetRouteResponse?>
{
    private readonly AppDbContext _db;

    public GetRouteHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetRouteResponse?> Handle(GetRouteQuery request, CancellationToken cancellationToken)
    {
        var route = await _db.Routes.Include(r => r.RouteStops).ThenInclude(rs => rs.Stop).FirstOrDefaultAsync(r => r.PathNumber.ToLower() == request.PathId.ToLower(), cancellationToken);

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
                StopId = rs.Stop.Id,
                MinutesFromStart = rs.MinutesFromStart,
                StopName = rs.Stop.Name,
                Lat = rs.Stop.Lat,
                Lon = rs.Stop.Lon,
                ArrivalTime = routeStartTime.AddMinutes(rs.MinutesFromStart)
            });
        }
        return new GetRouteResponse
        {
            Id = route.Id,
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
                Id = rs.Id,
                TransportType = rs.TransportType,
                PathId = rs.PathNumber
            });
        }
        return activeRouteList;
    }
}


public class AddRouteHandler : IRequestHandler<AddRouteCommand>
{
    private readonly AppDbContext _db;
    public AddRouteHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(AddRouteCommand req, CancellationToken cancel)
    {
        _db.Routes.Add(new RouteEntity
        {
            PathNumber = req.PathNumber,
            TransportType = req.TransportType,
            RouteLineJson = JsonSerializer.Serialize(req.RouteLine),
            RouteStops = req.Stops.Select(s => new RouteStop
            {
                StopId = s.StopId,
                StopOrder = s.StopOrder,
                MinutesFromStart = s.MinutesFromStart,
            }).ToList()

        });
        await _db.SaveChangesAsync(cancel);
    }
}
public class PutRouteHandler : IRequestHandler<PutRouteCommand, bool>
{
    private readonly AppDbContext _db;
    public PutRouteHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(PutRouteCommand command, CancellationToken cancel)
    {
        var route = await _db.Routes.FirstOrDefaultAsync(r => r.Id == command.Id, cancel);
        if (route == null)
            return false;
        route.PathNumber = command.PathNumber;
        route.RouteLineJson = JsonSerializer.Serialize(command.RouteLine);
        route.TransportType = command.TransportType;

        await _db.RouteStops.Where(r => r.RouteId == route.Id).ExecuteDeleteAsync(cancel);
        var newStops = command.Stops.Select(s => new RouteStop
        {
            RouteId = route.Id,
            StopId = s.StopId,
            StopOrder = s.StopOrder,
            MinutesFromStart = s.MinutesFromStart
        }).ToList();
        await _db.RouteStops.AddRangeAsync(newStops);
        await _db.SaveChangesAsync(cancel);
        return true;
    }
}
public class DeleteRouteHandler : IRequestHandler<DeleteRouteCommand, bool>
{
    private readonly AppDbContext _db;
    public DeleteRouteHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteRouteCommand command, CancellationToken cancel)
    {
        var route = await _db.Routes.Include(r => r.RouteStops).FirstOrDefaultAsync(r => r.Id == command.Id, cancel);
        if (route == null)
            return false;
        bool transport = await _db.TransportAssigments.AnyAsync(a => a.Route.Id == command.Id, cancel);
        // if (transport)
        //     return false;
        if (route.RouteStops.Any())
            _db.RouteStops.RemoveRange(route.RouteStops);
        _db.Routes.Remove(route);
        await _db.SaveChangesAsync(cancel);
        return true;
    }
}