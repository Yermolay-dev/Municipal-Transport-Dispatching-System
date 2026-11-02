using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt;
using MediatR;
using MTDS.Api.Infrastructure.Data;


namespace MTDS.Api.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginQuery, LoginResponse?>
{
    private readonly AppDbContext _db;

    public LoginHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<LoginResponse?> Handle(LoginQuery request, CancellationToken cancel)
    {
        var user = await _db.Users.FirstOrDefaultAsync(r => r.Email == request.Email, cancel);
        if (user == null)
            return null;

        bool isPasswordCorrect = BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash);

        if (!isPasswordCorrect)
            return null;

        return new LoginResponse
        {
            Status = "Successfully",
            Message = "Доступ разрешен",
            Token = ""
        };
    }

}