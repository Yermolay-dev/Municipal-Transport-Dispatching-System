

namespace MTDS.Api.Infrastructure.Entities;

public class Stop
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Lat { get; set; }
    public decimal Lon { get; set; }
}