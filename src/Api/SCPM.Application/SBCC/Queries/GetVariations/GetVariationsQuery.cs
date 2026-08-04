using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.SBCC.Dtos;

namespace SCPM.Application.SBCC.Queries.GetVariations;

public record GetVariationsQuery(Guid ProjectId) : IRequest<List<VariationDto>>;

public class GetVariationsQueryHandler : IRequestHandler<GetVariationsQuery, List<VariationDto>>
{
    private readonly IAppDbContext _db;

    public GetVariationsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<VariationDto>> Handle(GetVariationsQuery request, CancellationToken cancellationToken)
    {
        return await _db.Variations
            .Where(v => v.ProjectId == request.ProjectId)
            .Select(v => new VariationDto
            {
                Id = v.Id,
                Reference = v.Reference,
                Description = v.Description,
                ValueImpact = v.ValueImpact,
                Status = v.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
