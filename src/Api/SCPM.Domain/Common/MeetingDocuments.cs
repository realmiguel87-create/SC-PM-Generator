using SCPM.Domain.Enums;

namespace SCPM.Domain.Common;

/// <summary>One numbered item on an agenda, with the points that fall under it.</summary>
/// <param name="Points">
/// On an agenda these are the matters to be covered. On the minutes of the same meeting they
/// become the numbered entries beneath the item — 2.1, 2.2, 2.3 under item 2.0 — each with its
/// own action. That is how the council's own documents work, and it is why one definition serves
/// both forms rather than two definitions drifting apart.
/// </param>
public record MeetingItem(string Number, string Title, IReadOnlyList<string> Points);

/// <param name="IsMinutes">
/// Minutes rather than an agenda. The difference is not only the title: minutes carry an
/// attendees and apologies block, and their items are set out as numbered entries with an action
/// against each, where an agenda lists what is to be discussed.
/// </param>
public record MeetingDefinition(
    string Eyebrow,
    string Title,
    string Kind,
    IReadOnlyList<MeetingItem> Items,
    bool IsMinutes);

/// <summary>
/// The council's meeting agendas and minutes, restyled.
///
/// Taken from the service's own documents for the Allan Centre Phase 2 — the item numbering, the
/// three-column Ref / Item / Action layout and the item lists are theirs and are reproduced
/// exactly. What changes is the look, and three things about how it is put together:
///
///   The agenda and the minutes of a meeting share one definition. In the originals they are
///   separate files whose item lists have already begun to differ — the Progress agenda numbers
///   thirteen items where the minutes number twelve — which is how a meeting comes to be minuted
///   against a structure nobody agreed.
///
///   An action is recorded against every minute entry, including "NOTE" where there is none. A
///   blank action column cannot be distinguished from an action nobody wrote down.
///
///   Attendees are recorded with their organisation, as the service already does, because six
///   months later "WW" means nothing without it — and the originals contain two people with the
///   initials WW, which is precisely the case initials alone cannot resolve.
/// </summary>
public static class MeetingDocuments
{
    private const string Eyebrow = "Infrastructure Delivery · Project Meetings";

    /// <summary>
    /// The design team meeting, held through design to coordinate the consultants.
    ///
    /// Item order is the service's own: the disciplines in sequence, then the matters that cut
    /// across all of them. A discipline with nothing to report says so, which is itself worth
    /// minuting — an item quietly dropped from an agenda is a discipline nobody is chasing.
    /// </summary>
    public static readonly IReadOnlyList<MeetingItem> DesignTeamItems =
    [
        new("1.0", "Introductions, Present and Apologies",
            ["Attendees and apologies", "Project directory updates"]),
        new("2.0", "Programme Update",
            ["Design programme against baseline", "Information required dates", "Critical path items"]),
        new("3.0", "Architectural Matters", ["Design development", "Outstanding information"]),
        new("4.0", "Civil and Structural Matters", ["Design development", "Outstanding information"]),
        new("5.0", "M&E Matters", ["Design development", "Outstanding information", "Plant space and builders work"]),
        new("6.0", "Commercial and QS Matters",
            ["Cost position against budget", "Cost of design development", "Value engineering"]),
        new("7.0", "CDM and Health and Safety Matters",
            ["Designers' risk assessments", "Principal designer matters", "Residual risks to be carried forward"]),
        new("8.0", "Surveys and Investigations",
            ["Surveys instructed", "Results received", "Surveys outstanding and their lead times"]),
        new("9.0", "Information and Document Management",
            ["Information release schedule", "Drawing status and revisions", "Common data environment"]),
        new("10.0", "Statutory Consents",
            ["Planning", "Building warrant", "Roads, utilities and other consents"]),
        new("11.0", "Any Other Business", []),
        new("12.0", "Date of Next Meeting", []),
    ];

    /// <summary>
    /// The pre-start meeting, held once between contract award and possession of the site.
    ///
    /// The one meeting whose record is reached for most often afterwards: what was agreed about
    /// hours, access, insurances and sub-contractors is what later disputes turn on. Its items
    /// are the service's own, with the contract particulars kept as a block because a reader
    /// looking for the contract sum should find it in one place.
    /// </summary>
    public static readonly IReadOnlyList<MeetingItem> PreStartItems =
    [
        new("1.0", "Introductions",
            [
                "Council personnel — roles and responsibilities",
                "Principal designer, CDM and consultant roles",
                "Contractor's team — roles and responsibilities",
                "Contact details and project directory",
            ]),
        new("2.0", "Contract Particulars",
            [
                "Contract award date",
                "Contract sum",
                "Contract period and completion date",
                "Contract documents and framework reference",
                "Insurances — submitted or outstanding",
                "Sub-contractors — appointed and to be appointed",
            ]),
        new("3.0", "Programme",
            [
                "Construction programme — submitted or outstanding",
                "Procurement and lead-in times",
                "Key dates and sectional completion",
            ]),
        new("4.0", "Site",
            [
                "Working hours",
                "Site compound and establishment",
                "Access and deliveries",
                "Site signage",
                "Site security",
                "Protection works and areas remaining in use",
            ]),
        new("5.0", "Quality",
            [
                "Quality control procedures",
                "Pre-start site survey and photographic record",
                "Samples and benchmarks",
            ]),
        new("6.0", "Statutory Consents",
            ["Building warrant", "Planning approval and conditions", "Other consents"]),
        new("7.0", "Health and Safety and CDM",
            [
                "F10 notification",
                "Construction phase plan",
                "Health and safety file requirements",
                "Arrangements where the building remains in use",
            ]),
        new("8.0", "Communications",
            [
                "Information required by the contractor",
                "Contract instructions — who may issue and how",
                "Contractor queries and response times",
            ]),
        new("9.0", "Commercial",
            [
                "Valuations and payment dates",
                "Provisional sums",
                "Cash flow forecast",
                "Change procedure",
            ]),
        new("10.0", "Meetings and Progress Reporting",
            [
                "Technical and progress meetings — frequency and attendance",
                "Contractor's progress report — content and cut-off",
                "Photographic records",
            ]),
        new("11.0", "Any Other Business", []),
        new("12.0", "Date of Next Meeting", []),
    ];

    /// <summary>
    /// The progress meeting, held through construction.
    ///
    /// "Minutes from Previous Meeting" sits second because actions carried forward are the first
    /// thing a progress meeting exists to chase; an agenda that reaches them last reaches them
    /// when the meeting is already over.
    /// </summary>
    public static readonly IReadOnlyList<MeetingItem> ProgressItems =
    [
        new("1.0", "Introductions, Present and Apologies", []),
        new("2.0", "Minutes of Previous Meeting",
            ["Accuracy of the previous minutes", "Actions carried forward and their status"]),
        new("3.0", "Contractor's Report",
            ["Report issued and its date", "Progress this period", "Planned next period"]),
        new("4.0", "Programme",
            [
                "Current forecast against the accepted programme",
                "Delay and its cause",
                "Recovery proposals",
            ]),
        new("5.0", "Architectural Matters", ["Information requests", "Design queries and instructions"]),
        new("6.0", "Civil and Structural Matters", ["Information requests", "Temporary works"]),
        new("7.0", "M&E Matters", ["Information requests", "Commissioning and testing"]),
        new("8.0", "Financial Matters",
            [
                "Valuations and certificates issued",
                "Variations, compensation events and instructions",
                "Forecast final account",
            ]),
        new("9.0", "Health and Safety Matters",
            ["Incidents and near misses", "Inspections", "Site conditions and public interface"]),
        new("10.0", "Quality and Defects",
            ["Inspections and hold points", "Non-conformances raised and closed"]),
        new("11.0", "Client Matters", []),
        new("12.0", "Contractor Matters", []),
        new("13.0", "Any Other Business", []),
        new("14.0", "Date of Next Meeting", []),
    ];

    public static readonly MeetingDefinition DesignTeamMeetingAgenda =
        new(Eyebrow, "Design Team Meeting Agenda", "Design Team Meeting", DesignTeamItems, IsMinutes: false);

    public static readonly MeetingDefinition DesignTeamMeetingMinutes =
        new(Eyebrow, "Design Team Meeting Minutes", "Design Team Meeting", DesignTeamItems, IsMinutes: true);

    public static readonly MeetingDefinition PreStartMeetingAgenda =
        new(Eyebrow, "Pre-Start Meeting Agenda", "Pre-Start Meeting", PreStartItems, IsMinutes: false);

    public static readonly MeetingDefinition PreStartMeetingMinutes =
        new(Eyebrow, "Pre-Start Meeting Minutes", "Pre-Start Meeting", PreStartItems, IsMinutes: true);

    public static readonly MeetingDefinition ProgressMeetingAgenda =
        new(Eyebrow, "Progress Meeting Agenda", "Progress Meeting", ProgressItems, IsMinutes: false);

    public static readonly MeetingDefinition ProgressMeetingMinutes =
        new(Eyebrow, "Progress Meeting Minutes", "Progress Meeting", ProgressItems, IsMinutes: true);

    public static readonly IReadOnlyList<(CommitteeReportType Type, MeetingDefinition Definition)> All =
    [
        (CommitteeReportType.DesignTeamMeetingAgenda, DesignTeamMeetingAgenda),
        (CommitteeReportType.DesignTeamMeetingMinutes, DesignTeamMeetingMinutes),
        (CommitteeReportType.PreStartMeetingAgenda, PreStartMeetingAgenda),
        (CommitteeReportType.PreStartMeetingMinutes, PreStartMeetingMinutes),
        (CommitteeReportType.ProgressMeetingAgenda, ProgressMeetingAgenda),
        (CommitteeReportType.ProgressMeetingMinutes, ProgressMeetingMinutes),
    ];

    /// <summary>
    /// The definition for a meeting document, or null for a report type that is not one.
    ///
    /// Null rather than a throw, as with <see cref="StageDocuments.Find"/>: callers use this to
    /// ask "is this a meeting?", and every other document in the catalogue is a legitimate "no".
    /// </summary>
    public static MeetingDefinition? Find(CommitteeReportType type) =>
        All.FirstOrDefault(m => m.Type == type).Definition;
}
