namespace SCPM.Domain.Enums;

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
