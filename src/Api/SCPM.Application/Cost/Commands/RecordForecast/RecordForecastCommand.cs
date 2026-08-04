using MediatR;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;

namespace SCPM.Application.Cost.Commands.RecordForecast;

/// <summary>
/// Records a new forecast point and keeps Project.ForecastCost (used by the dashboard/portfolio
/// views) in sync with the latest figure, rather than requiring a separate update.
/// </summary>
public record RecordForecastCommand(Guid ProjectId, DateOnly ForecastDate, decimal ForecastCost, string? CommentaryNotes)
    : IRequest<Guid>;

public class RecordForecastCommandHandler : IRequestHandler<RecordForecastCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RecordForecastCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Guid> Handle(RecordForecastCommand request, CancellationToken cancellationToken)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found.");

        var actorId = _currentUser.UserId ?? Guid.Empty;

        var forecast = new Forecast
        {
            ProjectId = request.ProjectId,
            ForecastDate = request.ForecastDate,
            ForecastCost = request.ForecastCost,
            ApprovedBudgetAtForecast = project.ApprovedBudget,
            CommentaryNotes = request.CommentaryNotes,
            CreatedBy = actorId
        };

        _db.Forecasts.Add(forecast);

        project.ForecastCost = request.ForecastCost;
        project.ModifiedBy = actorId;
        project.ModifiedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return forecast.Id;
    }
}
