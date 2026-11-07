using MediatR;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using Microsoft.AspNetCore.Mvc.Localization;
namespace MTDS.Api.Features.Telemetry.GetActiveTransport;

public class GetActiveTransportHandler : IRequestHandler<GetActiveTransportQuery, List<GetActiveTransportResponse?>>
{
    private readonly IConnectionMultiplexer _cache;
    public GetActiveTransportHandler(IConnectionMultiplexer cache)
    {
        _cache = cache;
    }

    public async Task<List<GetActiveTransportResponse?>> Handle(GetActiveTransportQuery req, CancellationToken cancel)
    {
        var db = _cache.GetDatabase();
        var server = _cache.GetServer("localhost:6379");
        var resultList = new List<GetActiveTransportResponse?>();
        var keys = server.Keys(pattern: "MTDS_transport_location_*").ToArray();
        if (keys.Length == 0)
            return resultList;

        var values = await db.StringGetAsync(keys);
        foreach (var val in values)
        {
            if (val.HasValue)
            {
                var dto = JsonSerializer.Deserialize<GetActiveTransportResponse?>((string)val!);
                resultList.Add(dto);
            }
        }
        return resultList;
    }
}