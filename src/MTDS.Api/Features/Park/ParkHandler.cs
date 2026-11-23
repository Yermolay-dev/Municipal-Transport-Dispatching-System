using MediatR;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Infrastructure.Entities;
namespace MTDS.Api.Features.GetPark;

public class GetParkHandler : IRequestHandler<GetParkQuery, GetParkResponse?>
{
    private readonly AppDbContext _db;

    public GetParkHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetParkResponse?> Handle(GetParkQuery req, CancellationToken cancel)
    {
        var data = await _db.Transports.Where(r => !r.IsDeleted).ToListAsync(cancel);
        var res = new GetParkResponse
        {
            Park = data
        };
        return res;
    }
}



public class AddVehicleHandler : IRequestHandler<AddVehicleCommand, bool>
{
    private readonly AppDbContext _db;

    public AddVehicleHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(AddVehicleCommand command, CancellationToken cancel)
    {
        var isAdded = await _db.Transports.AnyAsync(r => r.NumberPlate == command.NumberPlate, cancel);
        if (isAdded)
            return false;
        _db.Transports.Add(new TransportEntity
        {
            NumberPlate = command.NumberPlate,
            Capacity = command.Capacity,
            Status = command.Status,
            Type = command.Type
        });
        await _db.SaveChangesAsync(cancel);
        return true;
    }
}
public class DeleteVehicleHandler : IRequestHandler<DeleteVehicleCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteVehicleHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteVehicleCommand command, CancellationToken cancel)
    {
        var vehicle = await _db.Transports.FirstOrDefaultAsync(r => r.Id == command.Id && !r.IsDeleted, cancel);
        if (vehicle == null)
            return false;
        bool isActive = await _db.TransportAssigments.AnyAsync(r => r.Transport.Id == command.Id, cancel);
        // if (isActive)
        //     return false;
        vehicle.IsDeleted = true;
        vehicle.Status = "archived";
        await _db.SaveChangesAsync(cancel);
        return true;
    }
}
public class PutVehicleHandler : IRequestHandler<PutVehicleCommand, bool>
{
    private readonly AppDbContext _db;

    public PutVehicleHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(PutVehicleCommand command, CancellationToken cancel)
    {
        var vehicle = await _db.Transports.FirstOrDefaultAsync(r => r.Id == command.Id && !r.IsDeleted, cancel);
        if (vehicle == null)
            return false;
        vehicle.NumberPlate = command.NumberPlate;
        vehicle.Capacity = command.Capacity;
        vehicle.Status = command.Status;
        vehicle.Type = command.Type;
        await _db.SaveChangesAsync(cancel);
        return true;
    }
}
public class PatchVehicleHandler : IRequestHandler<PatchVehicleCommand, bool>
{
    private readonly AppDbContext _db;

    public PatchVehicleHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(PatchVehicleCommand command, CancellationToken cancel)
    {
        var vehicle = await _db.Transports.FirstOrDefaultAsync(r => r.Id == command.Id && !r.IsDeleted, cancel);
        if (vehicle == null)
            return false;
        vehicle.Status = command.Status;
        await _db.SaveChangesAsync(cancel);
        return true;
    }
}