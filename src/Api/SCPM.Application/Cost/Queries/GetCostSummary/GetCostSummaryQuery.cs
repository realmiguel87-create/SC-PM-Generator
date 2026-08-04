using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.Cost.Dtos;

namespace SCPM.Application.Cost.Queries.GetCostSummary;

public record GetCostSummaryQuery(Guid ProjectId) : IRequest<CostSummaryDto?>;

public class GetCostSummaryQueryHandler : IRequestHandler<GetCostSummaryQuery, CostSummaryDto?>
{
    private readonly IAppDbContext _db;

    public GetCostSummaryQueryHandler(IAppDbContext db) => _db = db;

    public async Task<CostSummaryDto?> Handle(GetCostSummaryQuery request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && !p.IsDeleted, cancellationToken);

        if (project is null)
            return null;

        var baseline = await _db.CostPlans
            .Include(c => c.Lines)
            .Where(c => c.ProjectId == request.ProjectId && c.IsBaseline)
            .OrderByDescending(c => c.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var forecasts = await _db.Forecasts
            .Where(f => f.ProjectId == request.ProjectId)
            .OrderByDescending(f => f.ForecastDate)
            .Select(f => new ForecastDto
            {
                Id = f.Id,
                ForecastDate = f.ForecastDate,
                ForecastCost = f.ForecastCost,
                ApprovedBudgetAtForecast = f.ApprovedBudgetAtForecast,
                Variance = f.ForecastCost - f.ApprovedBudgetAtForecast,
                CommentaryNotes = f.CommentaryNotes
            })
            .ToListAsync(cancellationToken);

        return new CostSummaryDto
        {
            ProjectId = project.Id,
            ApprovedBudget = project.ApprovedBudget,
            CurrentForecastCost = project.ForecastCost,
            CurrentVariance = project.ForecastCost - project.ApprovedBudget,
            BaselineCostPlan = baseline is null ? null : new CostPlanDto
            {
                Id = baseline.Id,
                Name = baseline.Name,
                VersionNumber = baseline.VersionNumber,
                IsBaseline = baseline.IsBaseline,
                TotalAmount = baseline.Lines.Sum(l => l.Amount),
                Lines = baseline.Lines.Select(l => new CostPlanLineDto
                {
                    CostCategory = l.CostCategory,
                    Description = l.Description,
                    Amount = l.Amount
                }).ToList()
            },
            ForecastHistory = forecasts
        };
    }
}
