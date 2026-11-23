using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace MTDS.Api.Features.Auth.Register;

public class RegisterQuery : IRequest<bool>
{
    public string Role { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

