using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.SBCC.Commands.UpdateLossAndExpenseStatus;
using SCPM.Domain.Entities;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.UnitTests.SBCC;

/// <summary>
/// Determining a loss and expense claim.
///
/// The only real logic here is the nullable award, and it is the part worth testing: a claim
/// moved to Under review carries no agreed amount, and that is a different fact from an award of
/// zero. Passing null must therefore leave the stored value alone rather than clearing it —
/// otherwise a claim agreed at £40,000 and later reopened would silently lose the figure it was
/// agreed at.
/// </summary>
public class UpdateLossAndExpenseStatusTests
{
    private static readonly Guid ProjectId = Guid.NewGuid();

    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static async Task<LossAndExpenseClaim> SeedAsync(
        AppDbContext db, LossAndExpenseStatus status, decimal? awarded = null)
    {
        var claim = new LossAndExpenseClaim
        {
            Id = Guid.NewGuid(),
            ProjectId = ProjectId,
            Reference = "LE-001",
            Description = "Prolongation costs",
            ClaimedAmount = 120_000m,
            AwardedAmount = awarded,
            Status = status,
        };

        db.LossAndExpenseClaims.Add(claim);
        await db.SaveChangesAsync();
        return claim;
    }

    private static UpdateLossAndExpenseStatusCommandHandler Handler(AppDbContext db)
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        return new UpdateLossAndExpenseStatusCommandHandler(db, currentUser);
    }

    [Fact]
    public async Task Records_the_agreed_amount_when_one_is_given()
    {
        using var db = NewContext();
        var claim = await SeedAsync(db, LossAndExpenseStatus.Claimed);

        await Handler(db).Handle(
            new UpdateLossAndExpenseStatusCommand(claim.Id, LossAndExpenseStatus.Agreed, 75_000m),
            CancellationToken.None);

        var updated = await db.LossAndExpenseClaims.SingleAsync();
        updated.Status.Should().Be(LossAndExpenseStatus.Agreed);
        updated.AwardedAmount.Should().Be(75_000m, "a partial agreement is not the claimed amount");
    }

    [Fact]
    public async Task Leaves_an_existing_award_alone_when_no_amount_is_given()
    {
        using var db = NewContext();
        var claim = await SeedAsync(db, LossAndExpenseStatus.Agreed, awarded: 40_000m);

        await Handler(db).Handle(
            new UpdateLossAndExpenseStatusCommand(claim.Id, LossAndExpenseStatus.UnderReview, null),
            CancellationToken.None);

        var updated = await db.LossAndExpenseClaims.SingleAsync();
        updated.Status.Should().Be(LossAndExpenseStatus.UnderReview);
        // Null means "no figure supplied", not "no figure agreed". Treating it as a clear would
        // lose the record of what was agreed the first time.
        updated.AwardedAmount.Should().Be(40_000m);
    }

    [Fact]
    public async Task Records_an_award_of_zero_as_a_determination()
    {
        using var db = NewContext();
        var claim = await SeedAsync(db, LossAndExpenseStatus.UnderReview);

        await Handler(db).Handle(
            new UpdateLossAndExpenseStatusCommand(claim.Id, LossAndExpenseStatus.Agreed, 0m),
            CancellationToken.None);

        var updated = await db.LossAndExpenseClaims.SingleAsync();
        // Zero is a decision — the claim was considered and nothing was allowed. It must be
        // distinguishable from null, which is the absence of a decision.
        updated.AwardedAmount.Should().Be(0m);
        updated.AwardedAmount.Should().NotBeNull();
    }

    [Fact]
    public async Task Rejecting_a_claim_does_not_invent_an_award()
    {
        using var db = NewContext();
        var claim = await SeedAsync(db, LossAndExpenseStatus.UnderReview);

        await Handler(db).Handle(
            new UpdateLossAndExpenseStatusCommand(claim.Id, LossAndExpenseStatus.Rejected, null),
            CancellationToken.None);

        var updated = await db.LossAndExpenseClaims.SingleAsync();
        updated.Status.Should().Be(LossAndExpenseStatus.Rejected);
        updated.AwardedAmount.Should().BeNull();
    }

    [Fact]
    public async Task Unknown_claim_is_reported_rather_than_silently_ignored()
    {
        using var db = NewContext();

        var act = () => Handler(db).Handle(
            new UpdateLossAndExpenseStatusCommand(Guid.NewGuid(), LossAndExpenseStatus.Agreed, 1m),
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
