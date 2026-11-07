using MediatR;
using Microsoft.AspNetCore.Mvc;
namespace MTDS.Api.Features.Telemetry;


public class ReceiveGpsQuery : IRequest<bool>
{
    public string TransportId { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double SpeedKmh { get; set; }

}