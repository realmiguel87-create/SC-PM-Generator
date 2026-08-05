using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.SBCC.Dtos;

namespace SCPM.Application.SBCC.Queries.GetArchitectsInstructions;

public record GetArchitectsInstructionsQuery(Guid ProjectId) : IRequest<List<ArchitectsInstructionDto>>;

public class GetArchitectsInstructionsQueryHandler : IRequestHandler<GetArchitectsInstructionsQuery, List<ArchitectsInstructionDto>>
{
    private readonly IAppDbContext _db;

    public GetArchitectsInstructionsQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<ArchitectsInstructionDto>> Handle(GetArchitectsInstructionsQuery request, CancellationToken cancellationToken)
    {
        return await _db.ArchitectsInstructions
            .Where(a => a.ProjectId == request.ProjectId)
            .OrderByDescending(a => a.InstructionNumber)
            .Select(a => new ArchitectsInstructionDto
            {
                Id = a.Id,
                InstructionNumber = a.InstructionNumber,
                Description = a.Description,
                IssuedDate = a.IssuedDate,
                Status = a.Status.ToString()
            })
            .ToListAsync(cancellationToken);
    }
}
