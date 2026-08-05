using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.RiskManagement.Commands.CreateIssue;

public record CreateIssueCommand(
    Guid ProjectId,
    string Title,
    string? Description,
    IssueSeverity Severity,
    DateOnly RaisedDate) : IRequest<Guid>;

public class CreateIssueCommandHandler : IRequestHandler<CreateIssueCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateIssueCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateIssueCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var issue = new Issue
        {
            ProjectId = request.ProjectId,
            Title = request.Title,
            Description = request.Description,
            Severity = request.Severity,
            RaisedDate = request.RaisedDate,
            OwnerUserId = actorId,
            CreatedBy = actorId
        };

        _db.Issues.Add(issue);
        await _db.SaveChangesAsync(cancellationToken);

        return issue.Id;
    }
}
