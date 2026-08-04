using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.Cost.Commands.CreateCostPlan;

public record CreateCostPlanLineInput(string CostCategory, string? Description, decimal Amount);

public record CreateCostPlanCommand(
    Guid ProjectId,
    string Name,
    bool IsBaseline,
    List<CreateCostPlanLineInput> Lines) : IRequest<Guid>;

public class CreateCostPlanCommandHandler : IRequestHandler<CreateCostPlanCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CreateCostPlanCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(CreateCostPlanCommand request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? Guid.Empty;

        var previousVersionCount = await _db.CostPlans.CountAsync(c => c.ProjectId == request.ProjectId, cancellationToken);

        var costPlan = new CostPlan
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            VersionNumber = previousVersionCount + 1,
            IsBaseline = request.IsBaseline,
            CreatedBy = actorId
        };

        foreach (var line in request.Lines)
        {
            costPlan.Lines.Add(new CostPlanLine
            {
                CostCategory = line.CostCategory,
                Description = line.Description,
                Amount = line.Amount,
                CreatedBy = actorId
            });
        }

        _db.CostPlans.Add(costPlan);
        await _db.SaveChangesAsync(cancellationToken);

        return costPlan.Id;
    }
}
