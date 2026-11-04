

namespace MTDS.Api.Infrastructure.Entities;

public class Stop
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
}