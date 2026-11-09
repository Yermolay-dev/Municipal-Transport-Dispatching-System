namespace MTDS.Api.Infrastructure.Entities;


public class TransportAssigment
{
    public int Id { get; set; }
    public string DriverId { get; set; } = string.Empty;
    public int TransportId { get; set; }
    public DateTime ShiftStart { get; set; }
    public DateTime? ShiftEnd { get; set; }
    public RouteEntity Route { get; set; } = new();
    public TransportEntity Transport { get; set; } = new();
}