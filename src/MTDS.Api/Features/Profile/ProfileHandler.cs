using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Infrastructure.Entities;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Abstractions;

namespace MTDS.Api.Features.Profile;

public class GetProfileHandler : IRequestHandler<GetProfileQuery, GetProfileResponse>
{
    private readonly AppDbContext _db;

    public GetProfileHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetProfileResponse> Handle(GetProfileQuery query, CancellationToken cancel)
    {
        var user = await _db.Users.FirstOrDefaultAsync(r => r.Id == query.Id);

        return new GetProfileResponse
        {
            Id = user.Id,
            Role = user.Role,
            Name = user.Name,
            SurName = user.SurName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt,
            LastEntranceAt = user.LastEntranceAt
        };
    }
}

public class PutProfileHandler : IRequestHandler<PutProfileCommand>
{
    private readonly AppDbContext _db;

    public PutProfileHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(PutProfileCommand command, CancellationToken cancel)
    {
        var user = await _db.Users.FirstOrDefaultAsync(r => r.Id == command.Id);
        user.Name = command.Name;
        user.SurName = command.SurName;
        user.PhoneNumber = command.PhoneNumber;
        await _db.SaveChangesAsync(cancel);
    }
}

public class PatchProfileHandler : IRequestHandler<PatchProfileCommand, bool>
{
    private readonly AppDbContext _db;

    public PatchProfileHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> Handle(PatchProfileCommand command, CancellationToken cancel)
    {
        var user = await _db.Users.FirstOrDefaultAsync(r => r.Id == command.Id);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.NewPassword);
        await _db.SaveChangesAsync(cancel);
        return true;
    }
}
