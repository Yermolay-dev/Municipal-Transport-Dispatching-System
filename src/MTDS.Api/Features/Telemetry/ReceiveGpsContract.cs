

namespace MTDS.Api.Features.Telemetry;

public class ReceiveGpsRequest
{
    public string BusId { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double SpeedKmh { get; set; }
}


public class ReceiveGpsResponse
{
    public int Status { get; set; }
    public string Message { get; set; } = string.Empty;

}