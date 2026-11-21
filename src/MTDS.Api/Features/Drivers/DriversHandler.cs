using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Infrastructure.Entities;

namespace MTDS.Api.Features.Drivers;


public class GetDriversHandler : IRequestHandler<GetDriversQuery, List<DriverDto>>
{
    private readonly AppDbContext _db;

    public GetDriversHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<DriverDto>> Handle(GetDriversQuery query, CancellationToken cancel)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(r => r.Role == "Driver" && !r.IsDeleted)
            .OrderBy(r => r.SurName)
            .Select(r => new DriverDto
            {
                Id = r.Id,
                Name = r.Name!,
                SurName = r.SurName!,
                FullName = r.Name + " " + r.SurName,
                Email = r.Email,
                PhoneNumber = r.PhoneNumber!,
                DriverLicenseNumber = r.DriverLicenseNumber
            })
            .ToListAsync(cancel);
    }
}


public class CreateDriverHandler : IRequestHandler<CreateDriverCommand, bool>
{
    private readonly AppDbContext _db;

    public CreateDriverHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(CreateDriverCommand command, CancellationToken cancel)
    {
        var email = command.Email.Trim().ToLower();
        var phone = command.PhoneNumber.Trim();
        var license = string.IsNullOrWhiteSpace(command.DriverLicenseNumber)
            ? null
            : command.DriverLicenseNumber.Trim();

        if (await _db.Users.AnyAsync(r => r.Email.ToLower() == email, cancel))
            return false;
        if (await _db.Users.AnyAsync(r => r.PhoneNumber == phone, cancel))
            return false;
        if (license != null && await _db.Users.AnyAsync(r => r.DriverLicenseNumber == license, cancel))
            return false;
        var now = DateTime.UtcNow;
        var driver = new User
        {
            Id = Guid.CreateVersion7(),
            Role = "Driver",
            Name = command.Name.Trim(),
            SurName = command.SurName.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password),
            PhoneNumber = phone,
            DriverLicenseNumber = license,
            CreatedAt = now,
            LastEntranceAt = now
        };

        _db.Users.Add(driver);
        await _db.SaveChangesAsync(cancel);

        return true;
    }
}

public class UpdateDriverHandler : IRequestHandler<UpdateDriverCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateDriverHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(UpdateDriverCommand command, CancellationToken cancel)
    {
        var driver = await _db.Users.FirstOrDefaultAsync(r => r.Id == command.Id && r.Role == "Driver", cancel);
        if (driver == null)
            return false;

        var email = command.Email.Trim().ToLower();
        var phone = command.PhoneNumber.Trim();
        var license = string.IsNullOrWhiteSpace(command.DriverLicenseNumber)
            ? null
            : command.DriverLicenseNumber.Trim();

        if (await _db.Users.AnyAsync(r => r.Id != command.Id && r.Email.ToLower() == email, cancel))
            return false;

        if (await _db.Users.AnyAsync(r => r.Id != command.Id && r.PhoneNumber == phone, cancel))
            return false;

        if (license != null && await _db.Users.AnyAsync(r => r.Id != command.Id && r.DriverLicenseNumber == license, cancel))
            return false;

        driver.Name = command.Name.Trim();
        driver.SurName = command.SurName.Trim();
        driver.Email = email;
        driver.PhoneNumber = phone;
        driver.DriverLicenseNumber = license;

        if (!string.IsNullOrWhiteSpace(command.Password))
            driver.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password);

        await _db.SaveChangesAsync(cancel);
        return true;

    }
}


public class DeleteDriverHandler : IRequestHandler<DeleteDriverCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteDriverHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(DeleteDriverCommand command, CancellationToken cancel)
    {
        var driver = await _db.Users.FirstOrDefaultAsync(r => r.Id == command.Id && r.Role == "Driver", cancel);
        if (driver == null)
            return false;

        bool hasAssignments = await _db.TransportAssigments.AnyAsync(a => a.DriverId == command.Id.ToString(), cancel);
        if (hasAssignments)
        {
            driver.IsDeleted = true;
        }
        else
        {
            _db.Users.Remove(driver);
        }

        await _db.SaveChangesAsync(cancel);
        return true;
    }

}
