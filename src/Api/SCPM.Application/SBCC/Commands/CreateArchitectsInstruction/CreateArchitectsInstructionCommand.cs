using MediatR;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.SBCC.Commands.CreateArchitectsInstruction;

public record CreateArchitectsInstructionCommand(Guid ProjectId, int InstructionNumber, string Description, DateOnly IssuedDate) : IRequest<Guid>;

public class CreateArchitectsInstructionCommandHandler : IRequestHandler<CreateArchitectsInstructionCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateArchitectsInstructionCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateArchitectsInstructionCommand request, CancellationToken cancellationToken)
    {
        var instruction = new ArchitectsInstruction
        {
            ProjectId = request.ProjectId,
            InstructionNumber = request.InstructionNumber,
            Description = request.Description,
            IssuedDate = request.IssuedDate,
            CreatedBy = _currentUser.UserId ?? Guid.Empty
        };

        _db.ArchitectsInstructions.Add(instruction);
        await _db.SaveChangesAsync(cancellationToken);

        return instruction.Id;
    }
}
