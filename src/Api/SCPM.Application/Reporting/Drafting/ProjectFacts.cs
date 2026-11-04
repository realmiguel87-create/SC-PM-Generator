using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SCPM.Application.Common.Interfaces;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;

namespace SCPM.Application.Reporting.Drafting;

/// <summary>
/// The figures a report would otherwise be retyped from.
///
/// Read once from the registers and handed to <see cref="ReportDrafts"/>. The point is not
/// convenience: a document that retypes the cost position is a document that can disagree with the
/// cost register, and when the two disagree there is nothing to say which is right. This is the
/// same reasoning that linked the gate report to its gateway, applied to the figures rather than
/// to a decision.
/// </summary>
public record ProjectFacts
{
    public required string ProjectName { get; init; }
    public required string ProjectRef { get; init; }
    public required byte CurrentRibaStage { get; init; }

    public required decimal ApprovedBudget { get; init; }
    public required decimal ForecastCost { get; init; }

    public required int OutstandingMilestones { get; init; }
    public required int MilestonesBehindBaseline { get; init; }
    public required int WorstDelayDays { get; init; }

    public required int OpenRisks { get; init; }
    public required int HighestRiskScore { get; init; }
    public required int OpenIssues { get; init; }
    public required int Stakeholders { get; init; }

    public required int OpenCompensationEvents { get; init; }
    public required decimal CompensationEventValue { get; init; }
    public required int OpenVariations { get; init; }
    public required decimal VariationValue { get; init; }

    /// <summary>Sterling explicitly — see OpenXmlReportBuilder for why the server's culture is not trusted.</summary>
    public static readonly CultureInfo Culture = new("en-GB");

    public decimal Variance => ForecastCost - ApprovedBudget;

    public static async Task<ProjectFacts> ReadAsync(
        IAppDbContext db, Project project, CancellationToken cancellationToken)
    {
        var projectId = project.Id;

        // DelayDays is computed on the entity and cannot be translated into SQL, so the dates
        // come back and SnapshotMetrics does the arithmetic — the same function the snapshots
        // use, rather than a second copy of it here.
        var outstanding = await db.Milestones
            .Where(m => m.ProjectId == projectId && m.Status != MilestoneStatus.Complete)
            .ToListAsync(cancellationToken);
        var delays = outstanding.Select(SnapshotMetrics.DelayDays).ToList();

        var highestRiskScore = await db.Risks
            .Where(r => r.ProjectId == projectId)
            .Where(SnapshotMetrics.IsOpenRisk)
            .Select(r => (int?)(r.Probability * r.Impact))
            .OrderByDescending(s => s)
            .FirstOrDefaultAsync(cancellationToken);

        // Openness is defined once, in SnapshotMetrics, and reused rather than restated. A second
        // definition here is precisely the disagreement this class exists to prevent: a report and
        // a snapshot of the same project counting different compensation events as open, with
        // nothing to say which was right.
        var compensationEvents = await db.CompensationEvents
            .Where(c => c.ProjectId == projectId)
            .Where(SnapshotMetrics.IsOpenCompensationEvent)
            .Select(c => c.EstimatedValue)
            .ToListAsync(cancellationToken);

        var variations = await db.Variations
            .Where(v => v.ProjectId == projectId)
            .Where(SnapshotMetrics.IsOpenVariation)
            .Select(v => v.ValueImpact)
            .ToListAsync(cancellationToken);

        return new ProjectFacts
        {
            ProjectName = project.Name,
            ProjectRef = project.ProjectRef,
            CurrentRibaStage = project.CurrentRibaStage,
            ApprovedBudget = project.ApprovedBudget,
            ForecastCost = project.ForecastCost,

            OutstandingMilestones = outstanding.Count,
            MilestonesBehindBaseline = delays.Count(d => d > 0),
            WorstDelayDays = delays.Count == 0 ? 0 : Math.Max(0, delays.Max()),

            OpenRisks = await db.Risks
                .Where(r => r.ProjectId == projectId)
                .CountAsync(SnapshotMetrics.IsOpenRisk, cancellationToken),
            HighestRiskScore = highestRiskScore ?? 0,
            OpenIssues = await db.Issues
                .Where(i => i.ProjectId == projectId)
                .CountAsync(SnapshotMetrics.IsOpenIssue, cancellationToken),
            Stakeholders = await db.Stakeholders.CountAsync(
                s => s.ProjectId == projectId, cancellationToken),

            OpenCompensationEvents = compensationEvents.Count,
            CompensationEventValue = compensationEvents.Sum(),
            OpenVariations = variations.Count,
            VariationValue = variations.Sum(),
        };
    }

    // --- The sentences. Written once here so every document says the same thing the same way. ---

    public string Cost() =>
        $"Approved budget: {Money(ApprovedBudget)}. Current forecast: {Money(ForecastCost)}. "
        + $"Variance: {Money(Math.Abs(Variance))} "
        + $"({(Variance > 0 ? "over" : Variance < 0 ? "under" : "on")} budget).";

    public string Programme() =>
        OutstandingMilestones == 0
            ? "No outstanding milestones recorded."
            : $"{OutstandingMilestones} milestone(s) outstanding, of which {MilestonesBehindBaseline} "
              + $"are behind baseline"
              + (WorstDelayDays > 0 ? $", the worst by {WorstDelayDays} day(s)." : ".");

    public string Risk() =>
        OpenRisks == 0
            ? "No open risks recorded."
            : $"{OpenRisks} open risk(s) on the register, highest current score {HighestRiskScore}/25.";

    public string RisksAndIssues() =>
        OpenRisks == 0 && OpenIssues == 0
            ? "No open risks or issues recorded."
            : $"{Risk()} {(OpenIssues == 0 ? "No open issues recorded." : $"{OpenIssues} open issue(s) on the log.")}";

    public string Change() =>
        OpenCompensationEvents == 0 && OpenVariations == 0
            ? "No open compensation events or variations recorded."
            : $"{OpenCompensationEvents} open compensation event(s) totalling {Money(CompensationEventValue)}. "
              + $"{OpenVariations} variation(s) not yet agreed, totalling {Money(VariationValue)}.";

    public string Summary() =>
        $"{ProjectName} ({ProjectRef}), currently at RIBA Stage {CurrentRibaStage}. {Cost()}";

    private static string Money(decimal value) => value.ToString("C0", Culture);
}
