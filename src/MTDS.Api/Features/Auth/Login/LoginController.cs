using MediatR;
namespace MTDS.Api.Features.Auth.Login;

public class LoginQuery : IRequest<LoginResponse?>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}