using MediatR;
using MTDS.Api.Infrastructure.Data;
namespace MTDS.Api.Features.Drivers;

public class DriverDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? DriverLicenseNumber { get; set; }
}


public class GetDriversQuery : IRequest<List<DriverDto>>
{
}
public class CreateDriverCommand : IRequest<bool>
{
    public string Name { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? DriverLicenseNumber { get; set; }
}
public class UpdateDriverCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? DriverLicenseNumber { get; set; }
}
public class DeleteDriverCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}