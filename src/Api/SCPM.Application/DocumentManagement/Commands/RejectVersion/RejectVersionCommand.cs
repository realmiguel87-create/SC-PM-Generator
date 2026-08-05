using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Enums;

namespace SCPM.Application.DocumentManagement.Commands.RejectVersion;

public record RejectVersionCommand(Guid DocumentVersionId) : IRequest<Unit>;

public class RejectVersionCommandHandler : IRequestHandler<RejectVersionCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RejectVersionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(RejectVersionCommand request, CancellationToken cancellationToken)
    {
        var version = await _db.DocumentVersions.FirstOrDefaultAsync(v => v.Id == request.DocumentVersionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Document version {request.DocumentVersionId} not found.");

        version.Status = DocumentVersionStatus.Rejected;
        version.ModifiedBy = _currentUser.UserId ?? Guid.Empty;
        version.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
