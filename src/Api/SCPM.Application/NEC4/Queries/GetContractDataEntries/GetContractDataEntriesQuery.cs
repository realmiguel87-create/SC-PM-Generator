using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.NEC4.Dtos;

namespace SCPM.Application.NEC4.Queries.GetContractDataEntries;

public record GetContractDataEntriesQuery(Guid ProjectId) : IRequest<List<ContractDataEntryDto>>;

public class GetContractDataEntriesQueryHandler : IRequestHandler<GetContractDataEntriesQuery, List<ContractDataEntryDto>>
{
    private readonly IAppDbContext _db;

    public GetContractDataEntriesQueryHandler(IAppDbContext db) => _db = db;

    public async Task<List<ContractDataEntryDto>> Handle(GetContractDataEntriesQuery request, CancellationToken cancellationToken)
    {
        return await _db.ContractDataEntries
            .Where(c => c.ProjectId == request.ProjectId)
            .OrderBy(c => c.Part).ThenBy(c => c.ClauseReference)
            .Select(c => new ContractDataEntryDto
            {
                Id = c.Id,
                Part = c.Part.ToString(),
                ClauseReference = c.ClauseReference,
                Description = c.Description,
                Value = c.Value
            })
            .ToListAsync(cancellationToken);
    }
}
