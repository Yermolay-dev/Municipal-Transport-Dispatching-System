using MediatR;

namespace MTDS.Api.Features.Schedules;

public class GetSchedulesQuery : IRequest<GetSchedulesResponse>
{

}

public class GetSchedulesResponse
{
    public List<GetSchedulesDto> Schedules { get; set; } = new();
}

public class GetSchedulesDto
{
    public int Id { get; set; }
    public string DriverId { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string DriverPhone { get; set; } = string.Empty;
    public int TransportId { get; set; }
    public string NumberPlate { get; set; } = string.Empty;
    public int RouteId { get; set; }
    public string PathNumber { get; set; } = string.Empty;
    public DateTime ShiftStart { get; set; }
    public DateTime? ShiftEnd { get; set; }
}
public class AddSchedulesCommand : IRequest<bool>
{
    public string DriverId { get; set; } = string.Empty;
    public int TransportId { get; set; }
    public int RouteId { get; set; }
}

public class AddSchedulesEndCommand : IRequest<bool>
{
    public int Id { get; set; }
}