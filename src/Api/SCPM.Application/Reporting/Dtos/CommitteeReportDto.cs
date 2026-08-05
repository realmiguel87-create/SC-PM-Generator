namespace SCPM.Application.Reporting.Dtos;

public class CommitteeReportDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = default!;
    public string ProjectRef { get; set; } = default!;
    public string ReportType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateOnly? MeetingDate { get; set; }
    public string Status { get; set; } = default!;
    public DateTime CreatedDate { get; set; }

    public string ExecutiveSummary { get; set; } = default!;
    public string? Background { get; set; }
    public string? CurrentPosition { get; set; }
    public string? FinanceCommentary { get; set; }
    public string? ProgrammeCommentary { get; set; }
    public string? RiskCommentary { get; set; }
    public string? StakeholderCommentary { get; set; }
    public string? SustainabilityCommentary { get; set; }
    public string? EqualityImpactCommentary { get; set; }
    public string? Recommendations { get; set; }
}

public class CommitteeReportListItemDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = default!;
    public string ProjectRef { get; set; } = default!;
    public string ReportType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateOnly? MeetingDate { get; set; }
    public string Status { get; set; } = default!;
}
