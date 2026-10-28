using System;

namespace MTDS.Api.Infrastructure.Entities;

class User
{
    public string Id { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DriverLicenseNumber { get; set; } = string.Empty;
    public string NumberPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastEntranceAt { get; set; } = DateTime.UtcNow;
}