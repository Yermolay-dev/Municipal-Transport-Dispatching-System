using MediatR;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace MTDS.Api.Features.Telemetry.GetActiveTransport;

public class GetActiveTransportQuery : IRequest<List<GetActiveTransportResponse?>>
{
}

public class GetActiveTransportResponse
{
    public string TransportId { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lon { get; set; }
    public double SpeedKmh { get; set; }

}