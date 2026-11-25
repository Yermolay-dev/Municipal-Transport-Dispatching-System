using System.Collections.Generic;
using MediatR;
using System.Diagnostics.CodeAnalysis;
using MTDS.Api.Infrastructure.Entities;
namespace MTDS.Api.Features.Profile;


public class GetProfileQuery : IRequest<GetProfileResponse>
{
    public Guid Id { get; set; }
}

public class GetProfileResponse
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastEntranceAt { get; set; }
}
public class PutProfileCommand : IRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SurName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class PatchProfileCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public string CurrPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
