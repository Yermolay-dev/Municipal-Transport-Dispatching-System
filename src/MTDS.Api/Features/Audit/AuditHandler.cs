using MediatR;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Infrastructure.Entities;

namespace MTDS.Api.Features.Audit;

public class AuditHandler : INotificationHandler<TransportEmergencyCommandSentEvent>
{
    private readonly AppDbContext _db;
    public AuditHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(TransportEmergencyCommandSentEvent notification, CancellationToken cancel)
    {
        var log = new AuditLog
        {
            DispatcherId = notification.DispatcherId.ToString(),
            TransportId = notification.TransportId,
            Action = $"КОМАНДА: {notification.Reason}. ПРИЧИНА: {notification.Reason}",
            CreatedAt = DateTime.UtcNow
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(cancel);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[AUDIT] Действие диспетчера {notification.DispatcherId} записано в БД!");
        Console.ResetColor();
    }
}