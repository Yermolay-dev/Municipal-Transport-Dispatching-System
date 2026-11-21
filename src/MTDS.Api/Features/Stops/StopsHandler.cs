using MediatR;
using System;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
namespace MTDS.Api.Features.Stops;

public class AddStopHandler : IRequestHandler<AddStopQuery>
{
    private readonly AppDbContext _db;
    public AddStopHandler(AppDbContext db)
    {
        _db = db;
    }


    public async Task Handle(AddStopQuery req, CancellationToken cancel)
    {
        _db.Stops.Add(new Stop
        {
            Name = req.Name,
            Lat = req.Lat,
            Lon = req.Lon
        });
        await _db.SaveChangesAsync(cancel);
    }
}

public class GetStopHandler : IRequestHandler<GetStopQuery, GetStopResponse>
{
    private readonly AppDbContext _db;

    public GetStopHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetStopResponse> Handle(GetStopQuery req, CancellationToken cancel)
    {
        var stops = await _db.Stops.ToListAsync(cancel);
        return new GetStopResponse
        {
            Stops = stops
        };
    }
}
public class PutStopHandler : IRequestHandler<PutStopCommand, bool>
{
    private readonly AppDbContext _db;

    public PutStopHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(PutStopCommand command, CancellationToken cancel)
    {
        var stop = await _db.Stops.FirstOrDefaultAsync(r => r.Id == command.Id, cancel);
        if (stop == null)
            return false;
        stop.Name = command.Name;
        stop.Lat = command.Lat;
        stop.Lon = command.Lon;

        await _db.SaveChangesAsync(cancel);
        return true;
    }
}
public class DeleteStopHandler : IRequestHandler<DeleteStopCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteStopHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteStopCommand command, CancellationToken cancel)
    {
        bool isUsedInRoutes = await _db.RouteStops.AnyAsync(r => r.StopId == command.Id, cancel);
        if (isUsedInRoutes)
            return false;
        var stop = await _db.Stops.FirstOrDefaultAsync(r => r.Id == command.Id, cancel);
        if (stop == null)
            return false;

        _db.Stops.Remove(stop);
        await _db.SaveChangesAsync(cancel);
        return true;
    }
}