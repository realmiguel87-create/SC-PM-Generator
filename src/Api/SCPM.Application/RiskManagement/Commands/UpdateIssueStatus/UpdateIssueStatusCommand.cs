using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.RiskManagement.Commands.UpdateIssueStatus;

public record UpdateIssueStatusCommand(Guid IssueId, IssueStatus Status, string? ResolutionNotes) : IRequest<Unit>;

public class UpdateIssueStatusCommandHandler : IRequestHandler<UpdateIssueStatusCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UpdateIssueStatusCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateIssueStatusCommand request, CancellationToken cancellationToken)
    {
        var issue = await _db.Issues.FirstOrDefaultAsync(i => i.Id == request.IssueId, cancellationToken)
            ?? throw new KeyNotFoundException($"Issue {request.IssueId} not found.");

        issue.Status = request.Status;
        if (request.ResolutionNotes is not null)
            issue.ResolutionNotes = request.ResolutionNotes;

        if (request.Status is IssueStatus.Resolved or IssueStatus.Closed)
            issue.ResolvedDate ??= DateOnly.FromDateTime(DateTime.UtcNow);

        issue.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        issue.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
