using System.Collections.Generic;
using MediatR;
using System.Diagnostics.CodeAnalysis;
namespace MTDS.Api.Features.GetRoute;

public class GetRouteQuery : IRequest<GetRouteResponse?>
{
    public string PathId { get; set; } = string.Empty;
}

public class GetActiveRouteQuery : IRequest<List<GetRouteSummary>>
{

}

public class GetRouteResponse
{
    public string TransportType { get; set; } = string.Empty;
    public string PathId { get; set; } = string.Empty;
    public double[][] RouteLine { get; set; } = Array.Empty<double[]>();
    public List<StopStruct> Schedule { get; set; } = new();
}

public class GetRouteSummary
{
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
    public string StopName { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public DateTime ArrivalTime { get; set; } = DateTime.UtcNow;
}
