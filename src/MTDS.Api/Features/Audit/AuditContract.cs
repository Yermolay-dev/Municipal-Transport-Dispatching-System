using MediatR;
namespace MTDS.Api.Features.Audit;

public class TransportEmergencyCommandSentEvent : INotification
{
    public int TransportId { get; set; }
    public Guid DispatcherId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

