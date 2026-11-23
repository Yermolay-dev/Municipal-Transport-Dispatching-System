using MediatR;
namespace MTDS.Api.Features.Auth.Staff;

public class GetStaffQuery : IRequest<List<StaffDto>>
{

}

public class StaffDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime LastEntrance { get; set; }
    public DateTime CreatedAt { get; set; }

}

public class PatchStaffCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string NewPassword { get; set; } = string.Empty;

}

public class DeleteStaffCommand : IRequest<bool>
{
    public Guid Id { get; set; }

}