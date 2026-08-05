using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.SBCC.Commands.CreateVariation;

public record CreateVariationCommand(Guid ProjectId, string Reference, string Description, decimal ValueImpact) : IRequest<Guid>;

public class CreateVariationCommandHandler : IRequestHandler<CreateVariationCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateVariationCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateVariationCommand request, CancellationToken cancellationToken)
    {
        var variation = new Variation
        {
            ProjectId = request.ProjectId,
            Reference = request.Reference,
            Description = request.Description,
            ValueImpact = request.ValueImpact,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.Variations.Add(variation);
        await _db.SaveChangesAsync(cancellationToken);

        return variation.Id;
    }
}
