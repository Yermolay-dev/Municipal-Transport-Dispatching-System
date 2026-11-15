using MediatR;
using MTDS.Api.Infrastructure.Entities;
using System.Collections.Generic;

namespace MTDS.Api.Features.GetPark;

public class GetParkQuery : IRequest<GetParkResponse?>
{
}

public class GetParkResponse
{
    public List<TransportEntity> Park = new();
}

