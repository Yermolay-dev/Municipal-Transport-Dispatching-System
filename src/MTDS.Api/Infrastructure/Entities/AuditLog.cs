
namespace MTDS.Api.Infrastructure.Entities;


public class AuditLog
{
    public Int64 Id { get; set; }
    public string DispatcherId { get; set; } = string.Empty;
    public int TransportId { get; set; }
    public string Action { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}