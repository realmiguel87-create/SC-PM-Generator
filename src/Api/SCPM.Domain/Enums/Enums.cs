namespace SCPM.Domain.Enums;

public enum CommitteeReportType
{
    CommitteeReport,
    CabinetReport,
    BoardReport,
    CapitalProgrammeReport,
    DecisionPaper
}

public enum CommitteeReportStatus
{
    Draft,
    Approved,
    Submitted
}

public enum DocumentVersionStatus
{
    Draft,
    Review,
    Approved,
    Superseded,
    Archived,
    Rejected
}

public enum ProjectStatus
{
    Active,
    OnHold,
    Closed,
    Cancelled
}

public enum RibaStageInstanceStatus
{
    NotStarted,
    InProgress,
    Complete,
    Gated
}

public enum GatewayStatus
{
    Pending,
    Approved,
    Rejected,
    Withdrawn
}

public enum ApprovalDecision
{
    Approved,
    Rejected,
    ApprovedWithConditions
}

public enum MilestoneStatus
{
    NotStarted,
    InProgress,
    Complete,
    Delayed
}

public enum SnapshotType
{
    Daily,
    Weekly,
    Monthly,
    Gateway,
    Committee,
    Audit,
    Manual
}

public enum RiskStatus
{
    Open,
    Mitigated,
    Closed,
    Escalated
}

public enum IssueSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum IssueStatus
{
    Open,
    InProgress,
    Resolved,
    Closed
}

public enum OpportunityStatus
{
    Identified,
    BeingPursued,
    Realised,
    NotPursued
}

public enum EscalationStatus
{
    Pending,
    Resolved,
    Withdrawn
}

public enum StakeholderInfluence
{
    Low,
    Medium,
    High
}

public enum StakeholderInterest
{
    Low,
    Medium,
    High
}

// --- NEC4 ---

public enum Nec4RegisterStatus
{
    Open,
    Closed
}

public enum CompensationEventStatus
{
    Notified,
    Quoted,
    Accepted,
    Rejected,
    Implemented
}

public enum ContractDataPart
{
    PartOne,
    PartTwo
}

public enum RiskAllocationParty
{
    Client,
    Contractor,
    Shared
}

public enum PaymentAssessmentStatus
{
    Assessed,
    Certified,
    Paid
}

public enum ChangeRegisterStatus
{
    Proposed,
    Approved,
    Rejected,
    Implemented
}

// --- SBCC ---

public enum VariationStatus
{
    Instructed,
    Priced,
    Agreed
}

public enum ExtensionOfTimeStatus
{
    Claimed,
    UnderReview,
    Awarded,
    Rejected
}

public enum LossAndExpenseStatus
{
    Claimed,
    UnderReview,
    Agreed,
    Rejected
}

public enum ArchitectsInstructionStatus
{
    Issued,
    Complied
}

public enum InterimValuationStatus
{
    Draft,
    Certified,
    Paid
}

public enum RoleName
{
    Administrator,
    Director,
    ProjectSponsor,
    ProgrammeManager,
    ProjectManager,
    CommercialManager,
    QuantitySurveyor,
    GovernanceOfficer,
    CommitteeOfficer,
    ReadOnlyUser
}

/// <summary>
/// Why a milestone slipped, in the terms a construction programme is argued in.
///
/// These are causes, not blame. The distinction that matters contractually is not who was at
/// fault but who carries the time risk — which is why the categories track the standard grounds
/// an extension of time is claimed on rather than a tidier taxonomy.
/// </summary>
public enum DelayCauseCategory
{
    /// <summary>Weather beyond the contractual threshold — exceptionally adverse, not merely bad.</summary>
    Weather,

    /// <summary>Late, incomplete or changed design information.</summary>
    DesignInformation,

    /// <summary>A change instructed by the employer: a variation or a compensation event.</summary>
    EmployerChange,

    /// <summary>Planning, building warrant, statutory undertakers, road authority consent.</summary>
    StatutoryApproval,

    /// <summary>Ground conditions, contamination, archaeology — what the site turned out to be.</summary>
    SiteConditions,

    /// <summary>Contractor resourcing, sequencing, workmanship or subcontractor failure.</summary>
    ContractorPerformance,

    /// <summary>Supply chain: materials, plant, lead times.</summary>
    Procurement,

    /// <summary>
    /// Anything the categories above do not cover. Deliberately last and deliberately vague: a
    /// cause recorded as Other with a narrative is a better record than one forced into a category
    /// that does not fit, which reads as precision that was never there.
    /// </summary>
    Other
}
