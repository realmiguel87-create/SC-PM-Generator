using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.NEC4.Dtos;

namespace SCPM.Application.NEC4.Queries.GetPaymentAssessments;

public record GetPaymentAssessmentsQuery(Guid ProjectId) : IRequest<List<PaymentAssessmentDto>>;

public class GetPaymentAssessmentsQueryHandler : IRequestHandler<GetPaymentAssessmentsQuery, List<PaymentAssessmentDto>>
{
    private readonly IAppDbContext _db;

    public GetPaymentAssessmentsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<PaymentAssessmentDto>> Handle(GetPaymentAssessmentsQuery request, CancellationToken cancellationToken)
    {
        return await _db.PaymentAssessments
            .Where(p => p.ProjectId == request.ProjectId)
            .OrderByDescending(p => p.AssessmentNumber)
            .Select(p => new PaymentAssessmentDto
            {
                Id = p.Id,
                AssessmentNumber = p.AssessmentNumber,
                AssessmentDate = p.AssessmentDate,
                AmountDue = p.AmountDue,
                Status = p.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
