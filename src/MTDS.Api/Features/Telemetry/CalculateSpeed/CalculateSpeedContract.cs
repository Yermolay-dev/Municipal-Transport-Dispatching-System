using MediatR;
namespace MTDS.Api.Features.Telemetry.CalculateSpeed;

public class GetOptimalSpeedQuery : IRequest<OptimalSpeedResponse>
{
    public int TransportId { get; set; }
    public double CurrentLat { get; set; }
    public double CurrentLon { get; set; }
}


public class OptimalSpeedResponse
{
    public double RecommendedSpeedKmh { get; set; }
    public string Status { get; set; } = string.Empty;
}