using SCPM.Domain.Entities;

namespace SCPM.Application.Common.Interfaces;

/// <summary>
/// Reads a project's registers as they stood at a past moment.
///
/// This exists as an interface rather than a direct query because reading history means
/// SQL Server temporal tables (`FOR SYSTEM_TIME AS OF`), and the extension methods for those live
/// in the EF Core SqlServer provider. SCPM.Application deliberately references only the EF Core
/// core package and no provider — see the note in SCPM.Application.csproj — so the provider-bound
/// half of this lives in Infrastructure and the Application layer talks to the seam.
///
/// A useful consequence: the diffing logic that consumes this is testable against a substitute,
/// without a database, while the temporal query itself is covered by an integration test against
/// a real SQL Server. Those are two genuinely different risks and they get two different tests.
///
/// Soft-deleted rows are excluded, but at the *as-of* moment rather than now: a risk deleted last
/// week was not deleted a month ago, and a comparison against a month-old snapshot should see it
/// exactly as that snapshot saw it. EF Core's global query filters do this automatically, since
/// they are applied to the historical row's own IsDeleted value.
/// </summary>
public interface IRegisterHistory
{
    Task<IReadOnlyList<Risk>> RisksAsOfAsync(Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<Milestone>> MilestonesAsOfAsync(Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<EarlyWarning>> EarlyWarningsAsOfAsync(Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<CompensationEvent>> CompensationEventsAsOfAsync(Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<Variation>> VariationsAsOfAsync(Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExtensionOfTime>> ExtensionsOfTimeAsOfAsync(Guid projectId, DateTime asOfUtc, CancellationToken cancellationToken);

    // --- Every version within a window, rather than the state at one instant ---
    //
    // The *AsOf reads above answer "what did this register look like then". These answer "what
    // happened in between", and they are not the same question: an item raised and closed
    // entirely between two snapshots is absent from both endpoints, so no comparison of the two
    // endpoints can see it. These return one entry per row *version*, so the same item appears
    // several times when it changed several times.

    Task<IReadOnlyList<Risk>> RiskVersionsBetweenAsync(Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<Milestone>> MilestoneVersionsBetweenAsync(Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<EarlyWarning>> EarlyWarningVersionsBetweenAsync(Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<CompensationEvent>> CompensationEventVersionsBetweenAsync(Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<Variation>> VariationVersionsBetweenAsync(Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<ExtensionOfTime>> ExtensionOfTimeVersionsBetweenAsync(Guid projectId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);
}
