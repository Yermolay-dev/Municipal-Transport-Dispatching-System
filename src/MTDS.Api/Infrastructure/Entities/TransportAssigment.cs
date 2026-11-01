namespace MTDS.Api.Infrastructure.Entities;


public class TransportAssigment
{
    public int Id { get; set; }
    public string DriverId { get; set; } = string.Empty;
    public string TransportId { get; set; } = string.Empty;
    public DateTime ShiftStart { get; set; } = DateTime.UtcNow;
    public DateTime ShiftEnd { get; set; } = DateTime.UtcNow;
}