using MediatR;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Data;
using MTDS.Api.Infrastructure.Entities;

namespace MTDS.Api.Features.Audit;

public class AuditHandler : INotificationHandler<TransportEmergencyCommandSentEvent>
{
    private readonly IDbContextFactory<AppDbContext> _db;
    public AuditHandler(IDbContextFactory<AppDbContext> db)
    {
        _db = db;
    }

    public async Task Handle(TransportEmergencyCommandSentEvent notification, CancellationToken cancel)
    {
        using var db = await _db.CreateDbContextAsync(cancel);
        var log = new AuditLog
        {
            DispatcherId = notification.DispatcherId.ToString(),
            TransportId = notification.TransportId,
            Action = $"КОМАНДА: {notification.Reason}. ПРИЧИНА: {notification.Reason}",
            CreatedAt = DateTime.UtcNow
        };
        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(cancel);
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[AUDIT] Действие диспетчера {notification.DispatcherId} записано в БД!");
        Console.ResetColor();
    }
}