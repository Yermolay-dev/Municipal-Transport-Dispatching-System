using MediatR;
using Microsoft.EntityFrameworkCore;
using MTDS.Api.Infrastructure.Data;
namespace MTDS.Api.Features.GetPark;

public class GetParkHandler : IRequestHandler<GetParkQuery, GetParkResponse?>
{
    private readonly AppDbContext _db;

    public GetParkHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetParkResponse?> Handle(GetParkQuery req, CancellationToken cancel)
    {
        var data = await _db.Transports.ToListAsync(cancel);
        var res = new GetParkResponse
        {
            Park = data
        };
        return res;
    }
}