using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.NEC4.Commands.CreatePaymentAssessment;

public record CreatePaymentAssessmentCommand(Guid ProjectId, int AssessmentNumber, DateOnly AssessmentDate, decimal AmountDue) : IRequest<Guid>;

public class CreatePaymentAssessmentCommandHandler : IRequestHandler<CreatePaymentAssessmentCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreatePaymentAssessmentCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreatePaymentAssessmentCommand request, CancellationToken cancellationToken)
    {
        var assessment = new PaymentAssessment
        {
            ProjectId = request.ProjectId,
            AssessmentNumber = request.AssessmentNumber,
            AssessmentDate = request.AssessmentDate,
            AmountDue = request.AmountDue,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.PaymentAssessments.Add(assessment);
        await _db.SaveChangesAsync(cancellationToken);

        return assessment.Id;
    }
}
