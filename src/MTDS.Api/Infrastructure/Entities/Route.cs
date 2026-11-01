namespace MTDS.Api.Infrastructure.Entities;

public class RouteEntity
{
    public int Id { get; set; }
    public string PathNumber { get; set; } = string.Empty;
    public string TransportType { get; set; } = string.Empty;
    public string RouteLineJson { get; set; } = "[]";
    public List<RouteStop> RouteStops { get; set; } = new();
}