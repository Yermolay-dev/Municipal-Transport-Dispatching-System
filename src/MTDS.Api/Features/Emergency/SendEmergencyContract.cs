using MediatR;

namespace MTDS.Api.Features.Emergency;

public class SendEmergencyCommand : IRequest<bool>
{
    public int TransportId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid DispatcherId { get; set; }

}