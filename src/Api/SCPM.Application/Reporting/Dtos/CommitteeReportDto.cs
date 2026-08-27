namespace SCPM.Application.Reporting.Dtos;

/// <summary>
/// One section of a report as it should be presented: its heading, its content, and the stable key
/// the content is stored against.
/// </summary>
public class ReportSectionDto
{
    /// <summary>Stable identifier. Never shown to a reader; used when saving edits.</summary>
    public string Key { get; set; } = default!;

    /// <summary>The heading as it appears in the document.</summary>
    public string Heading { get; set; } = default!;

    public string? Content { get; set; }
}

public class CommitteeReportDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = default!;
    public string ProjectRef { get; set; } = default!;
    public string ReportType { get; set; } = default!;
    public string Title { get; set; } = default!;
    public DateOnly? MeetingDate { get; set; }

    /// <summary>The date the position is reported as at. See CommitteeReport.ReportDate.</summary>
    public DateOnly? ReportDate { get; set; }

    public string Status { get; set; } = default!;
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// The header block on the council's status report: sponsor, manager, budget.
    ///
    /// Read from the project rather than typed into the report. These are facts the platform
    /// already holds, and retyping a budget into a document is how a report comes to disagree with
    /// the register it is reporting on.
    /// </summary>
    public string? SponsorName { get; set; }
    public string? ProjectManagerName { get; set; }
    public decimal ApprovedBudget { get; set; }

    /// <summary>
    /// The report's narrative, in the order its type defines. Always the full set of sections for
    /// the type, including any not yet written — a heading with nothing under it is how an author
    /// sees what is still to do.
    /// </summary>
    public List<ReportSectionDto> Sections { get; set; } = [];
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
    public DateOnly? ReportDate { get; set; }
    public string Status { get; set; } = default!;
}
