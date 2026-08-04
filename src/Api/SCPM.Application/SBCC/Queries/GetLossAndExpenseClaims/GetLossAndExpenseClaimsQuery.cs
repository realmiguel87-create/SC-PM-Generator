using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.SBCC.Dtos;

namespace SCPM.Application.SBCC.Queries.GetLossAndExpenseClaims;

public record GetLossAndExpenseClaimsQuery(Guid ProjectId) : IRequest<List<LossAndExpenseClaimDto>>;

public class GetLossAndExpenseClaimsQueryHandler : IRequestHandler<GetLossAndExpenseClaimsQuery, List<LossAndExpenseClaimDto>>
{
    private readonly IAppDbContext _db;

    public GetLossAndExpenseClaimsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<LossAndExpenseClaimDto>> Handle(GetLossAndExpenseClaimsQuery request, CancellationToken cancellationToken)
    {
        return await _db.LossAndExpenseClaims
            .Where(l => l.ProjectId == request.ProjectId)
            .Select(l => new LossAndExpenseClaimDto
            {
                Id = l.Id,
                Reference = l.Reference,
                Description = l.Description,
                ClaimedAmount = l.ClaimedAmount,
                AwardedAmount = l.AwardedAmount,
                Status = l.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
