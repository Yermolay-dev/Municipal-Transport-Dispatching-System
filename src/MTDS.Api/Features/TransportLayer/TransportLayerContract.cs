using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
namespace MTDS.Api.Features.TransportLayer;

class TransportLayerResponse
{
    List<TransportPath> TransportPaths { get; set; } = new();
}

class TransportLayerSummary
{
    public string PathId { get; set; } = string.Empty;
    public string TransportType { get; set; } = string.Empty;
}

class TransportPath
{
    public string TransportType { get; set; } = string.Empty;
    public string PathId { get; set; } = string.Empty;

    public double[][] RouteLine { get; set; } = Array.Empty<double[]>();
    public List<StopStruct> Schedule { get; set; } = new();

}

class StopStruct
{
    public string StopName { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public DateTime ArrivalTime { get; set; } = DateTime.UtcNow;
}
