using MediatR;
using System.Collections.Generic;
using MTDS.Api.Infrastructure.Entities;

namespace MTDS.Api.Features.Audit.GetAuditLogs;

public class GetAuditLogsQuery : IRequest<GetAuditLogsResponse?>
{
}

public class GetAuditLogsResponse
{
    public List<AuditLog> Logs { get; set; } = new();
}