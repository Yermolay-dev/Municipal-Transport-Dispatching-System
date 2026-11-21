using MediatR;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Infrastructure.Entities;
namespace MTDS.Api.Features.Schedules;

public class GetSchedulesHandler : IRequestHandler<GetSchedulesQuery, GetSchedulesResponse>
{
    private readonly AppDbContext _db;

    public GetSchedulesHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetSchedulesResponse> Handle(GetSchedulesQuery req, CancellationToken cancel)
    {
        var data = await (from a in _db.TransportAssigments.AsNoTracking()
                          join u in _db.Users.AsNoTracking() on a.DriverId equals u.Id.ToString()
                          orderby a.ShiftEnd != null, a.ShiftStart descending
                          select new GetSchedulesDto
                          {
                              Id = a.Id,
                              DriverId = u.Id.ToString(),
                              DriverName = u.Name + " " + u.SurName,
                              DriverPhone = u.PhoneNumber ?? string.Empty,
                              TransportId = a.Transport.Id,
                              NumberPlate = a.Transport.NumberPlate,
                              RouteId = a.Route.Id,
                              PathNumber = a.Route.PathNumber,
                              ShiftStart = a.ShiftStart,
                              ShiftEnd = a.ShiftEnd
                          }).ToListAsync(cancel);

        return new GetSchedulesResponse
        {
            Schedules = data
        };
    }
}

public class AddSchedulesHandler : IRequestHandler<AddSchedulesCommand, bool>
{
    private readonly AppDbContext _db;

    public AddSchedulesHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(AddSchedulesCommand command, CancellationToken cancel)
    {
        // Надёжный парсинг Guid вместо сравнения строк в SQL
        if (!Guid.TryParse(command.DriverId, out var driverGuid))
            return false;

        var driver = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == driverGuid && u.Role == "Driver" && !u.IsDeleted, cancel);
        if (driver == null)
            return false;

        var transport = await _db.Transports
            .FirstOrDefaultAsync(t => t.Id == command.TransportId && !t.IsDeleted, cancel);
        if (transport == null)
            return false;

        if (transport.Status == "broken" || transport.Status == "EMERGENCY_STOPPED")
            return false;

        var route = await _db.Routes
            .FirstOrDefaultAsync(r => r.Id == command.RouteId, cancel);
        if (route == null)
            return false;

        var normalizedDriverId = driver.Id.ToString();

        bool driverBusy = await _db.TransportAssigments
            .AnyAsync(a => a.DriverId.ToLower() == normalizedDriverId.ToLower() && a.ShiftEnd == null, cancel);
        if (driverBusy)
            return false;

        bool transportBusy = await _db.TransportAssigments
            .AnyAsync(a => a.TransportId == command.TransportId && a.ShiftEnd == null, cancel);
        if (transportBusy)
            return false;

        var assignment = new TransportAssigment
        {
            DriverId = normalizedDriverId,
            Transport = transport,
            Route = route,
            ShiftStart = DateTime.UtcNow,
            ShiftEnd = null
        };

        transport.Status = "active";
        _db.TransportAssigments.Add(assignment);
        await _db.SaveChangesAsync(cancel);
        return true;
    }
} // <-- закрываем AddSchedulesHandler здесь!

public class AddSchedulesEndHandler : IRequestHandler<AddSchedulesEndCommand, bool>
{
    private readonly AppDbContext _db;
    public AddSchedulesEndHandler(AppDbContext db) => _db = db;

    public async Task<bool> Handle(AddSchedulesEndCommand command, CancellationToken cancel)
    {
        var assignment = await _db.TransportAssigments
            .Include(a => a.Transport)
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancel);

        if (assignment == null)
            return false;
        if (assignment.ShiftEnd != null)
            return false;

        assignment.ShiftEnd = DateTime.UtcNow;
        assignment.Transport.Status = "in_garage";

        await _db.SaveChangesAsync(cancel);
        return true;
    }
}
