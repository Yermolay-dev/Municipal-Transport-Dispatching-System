using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt;
using MediatR;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Infrastructure.Jwt;


namespace MTDS.Api.Features.Auth.Login;

public class LoginHandler : IRequestHandler<LoginQuery, LoginResponse?>
{
    private readonly AppDbContext _db;
    private readonly JwtProvider _jwtProvider;


    public LoginHandler(AppDbContext db, JwtProvider jwtProvider)
    {
        _db = db;
        _jwtProvider = jwtProvider;
    }

    public async Task<LoginResponse?> Handle(LoginQuery request, CancellationToken cancel)
    {
        var user = await _db.Users.FirstOrDefaultAsync(r => r.Email == request.Email, cancel);
        if (user == null)
            return null;

        bool isPasswordCorrect = BCrypt.Net.BCrypt.EnhancedVerify(request.Password, user.PasswordHash);

        if (!isPasswordCorrect)
            return null;

        string jwtToken = _jwtProvider.GenerateToken(user);
        return new LoginResponse
        {
            Status = "Successfully",
            Message = "Доступ разрешен",
            Token = jwtToken
        };
    }

}