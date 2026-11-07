using Microsoft.AspNetCore.Mvc;
using System;
using MediatR;
using MTDS.Api.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Features.Errors;
using MTDS.Api.Infrastructure.Entities;
using MTDS.Api.Infrastructure.Jwt;

namespace MTDS.Api.Features.Auth.Register;

public class RegisterHandler : IRequestHandler<RegisterQuery, RegisterResponse?>
{
    private readonly AppDbContext _db;
    private readonly JwtProvider _jwtProvider;

    public RegisterHandler(AppDbContext db, JwtProvider jwtProvider)
    {
        _db = db;
        _jwtProvider = jwtProvider;
    }

    public async Task<RegisterResponse?> Handle(RegisterQuery req, CancellationToken cancel)
    {
        var user = await _db.Users.FirstOrDefaultAsync(r => r.Email == req.Email, cancel);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Request received!");
        Console.ResetColor();
        if (user != null)
            return null;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"Request send to handler! email: {req.Email}, password: {req.Password}, name: {req.Name}");
        Console.ResetColor();

        var newUser = new User
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
        };
        _db.Users.Add(newUser);

        await _db.SaveChangesAsync(cancel);

        string jwtToken = _jwtProvider.GenerateToken(newUser);
        return new RegisterResponse
        {
            Status = "Successfully",
            Message = "Пользователь успешно зарегестрирован",
            Token = jwtToken
        };
    }
}