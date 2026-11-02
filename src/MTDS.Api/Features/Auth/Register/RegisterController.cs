using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace MTDS.Api.Features.Auth.Register;

public class RegisterQuery : IRequest<RegisterResponse?>
{
    public string Role { get; set; } = "Passenger";
    public string? PhotoUrl { get; set; } = string.Empty;
    public string? Name { get; set; } = string.Empty;
    public string? SurName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public string? DriverLicenseNumber { get; set; } = string.Empty;
}

public class LoginQuery : IRequest<LoginResponse>
{

}

public class RegisterRequest
{
    public string Role { get; set; } = "Passenger";
    public string? PhotoUrl { get; set; } = string.Empty;
    public string? Name { get; set; } = string.Empty;
    public string? SurName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public string? DriverLicenseNumber { get; set; } = string.Empty;
}

public class RegisterResponse
{
    public string Status { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}