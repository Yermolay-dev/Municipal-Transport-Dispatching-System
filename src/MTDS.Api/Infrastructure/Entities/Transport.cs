namespace MTDS.Api.Infrastructure.Entities;

public class TransportEntity
{
    public int Id { get; set; }
    public string NumberPlate { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}