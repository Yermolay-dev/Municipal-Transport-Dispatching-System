namespace MTDS.Api.Infrastructure.Entities;

public class RouteStop
{
    public int Id { get; set; }
    public int RouteId { get; set; }
    public int StopId { get; set; }
    public int StopOrder { get; set; }
    public int MinutesFromStart { get; set; }
    public RouteEntity Route { get; set; } = new();
    public Stop Stop { get; set; } = null!;
}