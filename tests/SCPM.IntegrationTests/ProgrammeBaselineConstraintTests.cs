using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SCPM.Domain.Entities;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.IntegrationTests;

/// <summary>
/// The database constraints behind programme baselines, against real SQL Server.
///
/// These cannot be tested any other way. The EF Core InMemory provider does not enforce unique
/// indexes at all, and it ignores index filters entirely — so a unit test asserting that two
/// current baselines are rejected would pass against a database that happily accepted them, which
/// is worse than having no test.
///
/// What is being protected is a single invariant: "which programme are we measured against?" must
/// have exactly one answer per project. The rebaseline command maintains that, but a command is
/// one write path and there is nothing stopping a second one being added later. If two rows both
/// claim to be current, every slip figure in the application silently depends on which one a query
/// happens to return first — a wrong number that looks entirely plausible.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public class ProgrammeBaselineConstraintTests : IAsyncLifetime
{
    private readonly ScpmWebApplicationFactory _factory;
    private Guid _projectId;

    public ProgrammeBaselineConstraintTests(ScpmWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        await _factory.ResetDatabaseAsync();

        _projectId = Guid.NewGuid();
        await WithDbAsync(async db =>
        {
            db.Projects.Add(new Project
            {
                Id = _projectId,
                ProjectRef = "BASE-001",
                Name = "Programme baseline constraint test project",
                ApprovedBudget = 1_000_000m,
            });
            await db.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    private ProgrammeBaseline BaselineWith(int revision, bool isCurrent, bool isDeleted = false) => new()
    {
        Id = Guid.NewGuid(),
        ProjectId = _projectId,
        Revision = revision,
        Name = $"Baseline {revision}",
        Reason = "Created by an integration test to exercise the database constraints.",
        IsCurrent = isCurrent,
        IsDeleted = isDeleted,
    };

    [Fact]
    public async Task Refuses_a_second_current_baseline_for_the_same_project()
    {
        await WithDbAsync(async db =>
        {
            db.ProgrammeBaselines.Add(BaselineWith(1, isCurrent: true));
            await db.SaveChangesAsync();
        });

        var act = async () => await WithDbAsync(async db =>
        {
            db.ProgrammeBaselines.Add(BaselineWith(2, isCurrent: true));
            await db.SaveChangesAsync();
        });

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Allows_many_superseded_baselines_alongside_one_current()
    {
        await WithDbAsync(async db =>
        {
            // The filter is what makes this work: superseded rows have IsCurrent = 0 and fall
            // outside the index entirely, so there is no limit on how many a project accumulates.
            // Without the filter the index would allow exactly one baseline per project full stop,
            // which would defeat the purpose of keeping a history at all.
            db.ProgrammeBaselines.Add(BaselineWith(1, isCurrent: false));
            db.ProgrammeBaselines.Add(BaselineWith(2, isCurrent: false));
            db.ProgrammeBaselines.Add(BaselineWith(3, isCurrent: true));
            await db.SaveChangesAsync();
        });

        await WithDbAsync(async db =>
        {
            var baselines = await db.ProgrammeBaselines
                .Where(b => b.ProjectId == _projectId)
                .ToListAsync();

            baselines.Should().HaveCount(3);
            baselines.Where(b => b.IsCurrent).Should().ContainSingle();
        });
    }

    [Fact]
    public async Task Lets_a_new_current_baseline_replace_a_soft_deleted_one()
    {
        await WithDbAsync(async db =>
        {
            db.ProgrammeBaselines.Add(BaselineWith(1, isCurrent: true, isDeleted: true));
            await db.SaveChangesAsync();
        });

        // The IsDeleted clause in the index filter exists for exactly this: a soft-deleted row
        // still holds IsCurrent = 1 in the database, and without that clause it would block its
        // own replacement — with an error naming a row the application can no longer even see.
        var act = async () => await WithDbAsync(async db =>
        {
            db.ProgrammeBaselines.Add(BaselineWith(2, isCurrent: true));
            await db.SaveChangesAsync();
        });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Refuses_two_baselines_claiming_the_same_revision()
    {
        await WithDbAsync(async db =>
        {
            db.ProgrammeBaselines.Add(BaselineWith(1, isCurrent: false));
            await db.SaveChangesAsync();
        });

        // Two rows claiming revision 1 makes "measured against revision 1" ambiguous. Two
        // concurrent rebaselines reading the same highest revision is the obvious way to get
        // there, and the index turns that race into a failed write rather than a corrupt record.
        var act = async () => await WithDbAsync(async db =>
        {
            db.ProgrammeBaselines.Add(BaselineWith(1, isCurrent: true));
            await db.SaveChangesAsync();
        });

        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
