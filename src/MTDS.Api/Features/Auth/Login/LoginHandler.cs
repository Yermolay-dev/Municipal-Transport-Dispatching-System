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
        {
            Console.WriteLine("Пользователя такого нету в БД");
            return null;

        }

        Console.WriteLine($"Пользователь имеет пароль {request.Password}, login: {request.Email}, Length: {user.PasswordHash.Length}");
        Console.WriteLine($"Length.Trim:  {user.PasswordHash.Trim().Length}");

        bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);

        if (!isPasswordCorrect)
        {
            Console.WriteLine("Пользователь имеет неверный пароль или хэш не совпал");
            return null;

        }

        string jwtToken = _jwtProvider.GenerateToken(user);
        return new LoginResponse
        {
            Status = "Successfully",
            Message = "Доступ разрешен",
            Token = jwtToken
        };
    }

}