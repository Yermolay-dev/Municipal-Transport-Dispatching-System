using MediatR;
using MTDS.Api.Infrastructure.Entities;
using System.Collections.Generic;

namespace MTDS.Api.Features.GetPark;

public class GetParkQuery : IRequest<GetParkResponse?>
{
}

public class GetParkResponse
{
    public List<TransportEntity> Park { get; set; } = new();
}


public class AddVehicleCommand : IRequest<bool>
{
    public string NumberPlate { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}
public class DeleteVehicleCommand : IRequest<bool>
{
    public int Id { get; set; }
}

public class PutVehicleCommand : IRequest<bool>
{
    public int Id { get; set; }
    public string NumberPlate { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class PatchVehicleCommand : IRequest<bool>
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
}