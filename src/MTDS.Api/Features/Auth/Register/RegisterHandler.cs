using Microsoft.AspNetCore.Mvc;
using System;
using MediatR;
using MTDS.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Features.Errors;
using MTDS.Api.Infrastructure.Entities;

namespace MTDS.Api.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterQuery, RegisterResponse?>
{
    private readonly AppDbContext _db;

    public RegisterHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<RegisterResponse?> Handle(RegisterQuery req, CancellationToken cancel)
    {
        var user = await _db.Users.FirstOrDefaultAsync(r => r.Email == req.Email, cancel);

        if (user != null)
            return null;

        _db.Users.Add(new User
        {
            Id = Guid.CreateVersion7(),
            Role = req.Role ?? "Passenger",
            PhotoUrl = req.PhotoUrl,
            Name = req.Name,
            SurName = req.SurName,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            PhoneNumber = req.PhoneNumber,
            DriverLicenseNumber = req.DriverLicenseNumber
        });

        await _db.SaveChangesAsync(cancel);

        return new RegisterResponse
        {
            Status = "Successfully",
            Message = "Пользователь успешно зарегестрирован",
            Token = ""
        };
    }
}