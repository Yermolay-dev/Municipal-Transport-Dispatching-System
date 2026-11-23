using Microsoft.AspNetCore.Mvc;
using System;
using MediatR;
using MTDS.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Features.Errors;
using MTDS.Api.Infrastructure.Entities;
using MTDS.Api.Infrastructure.Jwt;

namespace MTDS.Api.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterQuery, bool>
{
    private readonly AppDbContext _db;

    public RegisterHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(RegisterQuery req, CancellationToken cancel)
    {
        var user = await _db.Users.FirstOrDefaultAsync(r => r.Email == req.Email && !r.IsDeleted, cancel);
        if (user != null)
            return false;

        var newUser = new User
        {
            Id = Guid.CreateVersion7(),
            Role = req.Role,
            PhotoUrl = req.PhotoUrl,
            Name = req.Name,
            SurName = req.SurName,
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            PhoneNumber = req.PhoneNumber,
        };
        _db.Users.Add(newUser);

        await _db.SaveChangesAsync(cancel);
        return true;
    }
}