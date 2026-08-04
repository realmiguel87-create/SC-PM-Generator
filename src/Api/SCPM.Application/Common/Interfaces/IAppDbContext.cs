using Microsoft.EntityFrameworkCore;
using SCPM.Domain.Entities;

namespace SCPM.Application.Common.Interfaces;

/// <summary>
/// Application-facing view of the persistence context. Infrastructure's AppDbContext
/// implements this so handlers depend on an abstraction, not EF Core directly.
/// </summary>
public interface IAppDbContext
{
    DbSet<Programme> Programmes { get; }
    DbSet<Project> Projects { get; }
    DbSet<RibaStageDefinition> RibaStageDefinitions { get; }
    DbSet<RibaStageInstance> RibaStageInstances { get; }
    DbSet<Gateway> Gateways { get; }
    DbSet<Approval> Approvals { get; }
    DbSet<User> Users { get; }
    DbSet<CostPlan> CostPlans { get; }
    DbSet<CostPlanLine> CostPlanLines { get; }
    DbSet<Forecast> Forecasts { get; }
    DbSet<Milestone> Milestones { get; }
    DbSet<DecisionRegisterEntry> DecisionRegisterEntries { get; }
    DbSet<Snapshot> Snapshots { get; }
    DbSet<Risk> Risks { get; }
    DbSet<Issue> Issues { get; }
    DbSet<Opportunity> Opportunities { get; }
    DbSet<Escalation> Escalations { get; }
    DbSet<Stakeholder> Stakeholders { get; }
    DbSet<StakeholderEngagement> StakeholderEngagements { get; }
    DbSet<EarlyWarning> EarlyWarnings { get; }
    DbSet<CompensationEvent> CompensationEvents { get; }
    DbSet<ContractDataEntry> ContractDataEntries { get; }
    DbSet<RiskAllocationItem> RiskAllocationItems { get; }
    DbSet<AcceptedProgrammeEntry> AcceptedProgrammeEntries { get; }
    DbSet<PaymentAssessment> PaymentAssessments { get; }
    DbSet<ChangeRegisterItem> ChangeRegisterItems { get; }
    DbSet<Variation> Variations { get; }
    DbSet<ExtensionOfTime> ExtensionsOfTime { get; }
    DbSet<LossAndExpenseClaim> LossAndExpenseClaims { get; }
    DbSet<ArchitectsInstruction> ArchitectsInstructions { get; }
    DbSet<InterimValuation> InterimValuations { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentVersion> DocumentVersions { get; }
    DbSet<DocumentFile> DocumentFiles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
