using System.Collections.Generic;
using MediatR;
using System.Diagnostics.CodeAnalysis;
using MTDS.Api.Infrastructure.Entities;
namespace MTDS.Api.Features.Routes;


public class GetRouteQuery : IRequest<GetRouteResponse?>
{
    public string PathId { get; set; } = string.Empty;
}

public class GetActiveRouteQuery : IRequest<List<GetRouteSummary>>
{

}

public class GetRouteResponse
{
    public int Id { get; set; }
    public string TransportType { get; set; } = string.Empty;
    public string PathId { get; set; } = string.Empty;
    public double[][] RouteLine { get; set; } = Array.Empty<double[]>();
    public List<StopStruct> Schedule { get; set; } = new();
}

public class GetRouteSummary
{
    public int Id { get; set; }
    public string PathId { get; set; } = string.Empty;
    public string TransportType { get; set; } = string.Empty;
}

public class TransportPath
{
    public string TransportType { get; set; } = string.Empty;
    public string PathId { get; set; } = string.Empty;

    public double[][] RouteLine { get; set; } = Array.Empty<double[]>();
    public List<StopStruct> Schedule { get; set; } = new();

}

public class StopStruct
{
    public int StopId { get; set; }
    public int MinutesFromStart { get; set; }
    public string StopName { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public DateTime ArrivalTime { get; set; } = DateTime.UtcNow;
}


public class AddRouteCommand : IRequest
{
    public string PathNumber { get; set; } = string.Empty;
    public string TransportType { get; set; } = string.Empty;
    public double[][] RouteLine { get; set; } = Array.Empty<double[]>();
    public List<RouteStop> Stops { get; set; } = new();
}

public class PutRouteCommand : IRequest<bool>
{
    public int Id { get; set; }
    public string PathNumber { get; set; } = string.Empty;
    public string TransportType { get; set; } = string.Empty;
    public double[][] RouteLine { get; set; } = Array.Empty<double[]>();
    public List<PutRouteDto> Stops { get; set; } = new();
}

public class PutRouteDto
{
    public int StopId { get; set; }
    public int StopOrder { get; set; }
    public int MinutesFromStart { get; set; }
}
public class DeleteRouteCommand : IRequest<bool>
{
    public int Id { get; set; }

}
