using System;
namespace MTDS.Api.Infrastructure.Entities;

public class User
{
    public Guid Id { get; set; }
    public string Role { get; set; } = "Passenger";
    public string? PhotoUrl { get; set; } = string.Empty;
    public string? Name { get; set; } = string.Empty;
    public string? SurName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; } = string.Empty;
    public string? DriverLicenseNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastEntranceAt { get; set; } = DateTime.UtcNow;
}