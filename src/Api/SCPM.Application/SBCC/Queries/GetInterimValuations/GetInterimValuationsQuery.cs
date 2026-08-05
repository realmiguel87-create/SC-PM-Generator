using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.SBCC.Dtos;

namespace SCPM.Application.SBCC.Queries.GetInterimValuations;

public record GetInterimValuationsQuery(Guid ProjectId) : IRequest<List<InterimValuationDto>>;

public class GetInterimValuationsQueryHandler : IRequestHandler<GetInterimValuationsQuery, List<InterimValuationDto>>
{
    private readonly IAppDbContext _db;

    public GetInterimValuationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<InterimValuationDto>> Handle(GetInterimValuationsQuery request, CancellationToken cancellationToken)
    {
        return await _db.InterimValuations
            .Where(i => i.ProjectId == request.ProjectId)
            .OrderByDescending(i => i.ValuationNumber)
            .Select(i => new InterimValuationDto
            {
                Id = i.Id,
                ValuationNumber = i.ValuationNumber,
                ValuationDate = i.ValuationDate,
                GrossValuation = i.GrossValuation,
                NetPayment = i.NetPayment,
                Status = i.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
