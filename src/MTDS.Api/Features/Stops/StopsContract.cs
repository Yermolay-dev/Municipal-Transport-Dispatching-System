using MediatR;
using MTDS.Api.Infrastructure.Entities;
namespace MTDS.Api.Features.Stops;

public class AddStopQuery : IRequest
{
    public string Name { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
}

public class AddStopResponse
{
    public string Name { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }

}

public class GetStopQuery : IRequest<GetStopResponse>
{
}

public class GetStopResponse
{
    public List<Stop> Stops { get; set; } = new();
}
public class PutStopCommand : IRequest<bool>
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }

}
public class DeleteStopCommand : IRequest<bool>
{
    public int Id { get; set; }
}
