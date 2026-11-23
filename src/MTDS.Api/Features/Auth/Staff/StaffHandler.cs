using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MTDS.Api.Features.Schedules;
using MTDS.Api.Infrastructure.Data;
namespace MTDS.Api.Features.Auth.Staff;


public class GetStaffHandler : IRequestHandler<GetStaffQuery, List<StaffDto>>
{
    private readonly AppDbContext _db;


    public GetStaffHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<StaffDto>> Handle(GetStaffQuery query, CancellationToken cancel)
    {
        var data = await _db.Users.Where(r => (r.Role == "Dispatcher" || r.Role == "Admin") && !r.IsDeleted).ToListAsync(cancel);
        var res = new List<StaffDto>();
        foreach (var dispatcher in data)
        {
            res.Add(new StaffDto
            {
                Id = dispatcher.Id,
                Name = dispatcher.Name!,
                SurName = dispatcher.SurName!,
                Email = dispatcher.Email,
                PhoneNumber = dispatcher.PhoneNumber!,
                LastEntrance = dispatcher.LastEntranceAt,
                CreatedAt = dispatcher.CreatedAt,
                Role = dispatcher.Role
            });
        }
        return res;
    }
}


public class PatchStaffHandler : IRequestHandler<PatchStaffCommand, bool>
{
    private readonly AppDbContext _db;


    public PatchStaffHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(PatchStaffCommand command, CancellationToken cancel)
    {
        var data = await _db.Users.FirstOrDefaultAsync(r => (r.Role == "Dispatcher" || r.Role == "Admin") && !r.IsDeleted && r.Id == command.Id, cancel);
        if (data == null)
            return false;
        data.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
        await _db.SaveChangesAsync(cancel);
        return true;
    }
}
public class DeleteStaffHandler : IRequestHandler<DeleteStaffCommand, bool>
{
    private readonly AppDbContext _db;


    public DeleteStaffHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteStaffCommand command, CancellationToken cancel)
    {
        var data = await _db.Users.FirstOrDefaultAsync(r => (r.Role == "Dispatcher" || r.Role == "Admin") && !r.IsDeleted && r.Id == command.Id, cancel);
        if (data == null)
            return false;
        data.IsDeleted = true;
        await _db.SaveChangesAsync(cancel);
        return true;
    }
}