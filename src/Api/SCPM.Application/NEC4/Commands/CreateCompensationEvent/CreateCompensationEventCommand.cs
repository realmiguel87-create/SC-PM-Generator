using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.NEC4.Commands.CreateCompensationEvent;

public record CreateCompensationEventCommand(
    Guid ProjectId, string Reference, string Title, string? ClauseReference, decimal EstimatedValue, DateOnly NotifiedDate) : IRequest<Guid>;

public class CreateCompensationEventCommandHandler : IRequestHandler<CreateCompensationEventCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCompensationEventCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCompensationEventCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var ce = new CompensationEvent
        {
            ProjectId = request.ProjectId,
            Reference = request.Reference,
            Title = request.Title,
            ClauseReference = request.ClauseReference,
            EstimatedValue = request.EstimatedValue,
            NotifiedDate = request.NotifiedDate,
            CreatedBy = actorId
        };

        _db.CompensationEvents.Add(ce);
        await _db.SaveChangesAsync(cancellationToken);

        return ce.Id;
    }
}
