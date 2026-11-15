using System.Reflection.Metadata;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Data;

namespace MTDS.Api.Features.Audit.GetAuditLogs;

public class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, GetAuditLogsResponse?>
{
    private readonly AppDbContext _db;

    public GetAuditLogsHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetAuditLogsResponse?> Handle(GetAuditLogsQuery req, CancellationToken cancel)
    {
        var data = await _db.AuditLogs.ToListAsync(cancel);
        var response = new GetAuditLogsResponse
        {
            Logs = data
        };
        return response;
    }
}