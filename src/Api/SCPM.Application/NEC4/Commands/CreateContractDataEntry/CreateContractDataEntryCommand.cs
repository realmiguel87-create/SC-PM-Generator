using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.NEC4.Commands.CreateContractDataEntry;

public record CreateContractDataEntryCommand(Guid ProjectId, ContractDataPart Part, string ClauseReference, string Description, string Value) : IRequest<Guid>;

public class CreateContractDataEntryCommandHandler : IRequestHandler<CreateContractDataEntryCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateContractDataEntryCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateContractDataEntryCommand request, CancellationToken cancellationToken)
    {
        var entry = new ContractDataEntry
        {
            ProjectId = request.ProjectId,
            Part = request.Part,
            ClauseReference = request.ClauseReference,
            Description = request.Description,
            Value = request.Value,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.ContractDataEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);

        return entry.Id;
    }
}
