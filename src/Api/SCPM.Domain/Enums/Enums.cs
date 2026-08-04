namespace SCPM.Domain.Enums;

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
