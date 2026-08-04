using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SCPM.Application.Common.Interfaces;
using SCPM.Application.DocumentManagement.Commands.ApproveVersion;
using SCPM.Application.DocumentManagement.Commands.CreateDocument;
using SCPM.Application.DocumentManagement.Commands.CreateDraftRevision;
using SCPM.Domain.Enums;
using SCPM.Infrastructure.Persistence;
using Xunit;

namespace SCPM.UnitTests.DocumentManagement;

/// <summary>
/// Exercises the version-number bump logic (1.0 Draft -> 1.1 Draft -> 2.0 Approved -> 2.1 Draft
/// -> 3.0 Approved, per docs/erd.md) against a real DbContext (EF Core InMemory provider) rather
/// than hand-rolled fakes, so the LINQ queries in the handlers are actually exercised.
/// </summary>
public class DocumentVersioningTests
{
    private static AppDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static ICurrentUserService FakeCurrentUser()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        return currentUser;
    }

    [Fact]
    public async Task Approving_a_draft_bumps_to_the_next_major_version()
    {
        await using var db = NewContext();
        var currentUser = FakeCurrentUser();
        var projectId = Guid.NewGuid();

        var documentId = await new CreateDocumentCommandHandler(db, currentUser)
            .Handle(new CreateDocumentCommand(projectId, "Project Execution Plan", "Governance", null), CancellationToken.None);

        var initialVersion = await db.DocumentVersions.SingleAsync(v => v.DocumentId == documentId);
        initialVersion.VersionLabel.Should().Be("1.0");
        initialVersion.Status.Should().Be(DocumentVersionStatus.Draft);

        await new ApproveVersionCommandHandler(db, currentUser)
            .Handle(new ApproveVersionCommand(initialVersion.Id), CancellationToken.None);

        var approved = await db.DocumentVersions.SingleAsync(v => v.Id == initialVersion.Id);
        approved.VersionLabel.Should().Be("2.0");
        approved.Status.Should().Be(DocumentVersionStatus.Approved);
    }

    [Fact]
    public async Task Draft_revisions_increment_the_minor_version_within_the_current_major()
    {
        await using var db = NewContext();
        var currentUser = FakeCurrentUser();
        var projectId = Guid.NewGuid();

        var documentId = await new CreateDocumentCommandHandler(db, currentUser)
            .Handle(new CreateDocumentCommand(projectId, "Governance Plan", "Governance", null), CancellationToken.None);

        var revision1Id = await new CreateDraftRevisionCommandHandler(db, currentUser)
            .Handle(new CreateDraftRevisionCommand(documentId), CancellationToken.None);
        var revision2Id = await new CreateDraftRevisionCommandHandler(db, currentUser)
            .Handle(new CreateDraftRevisionCommand(documentId), CancellationToken.None);

        (await db.DocumentVersions.SingleAsync(v => v.Id == revision1Id)).VersionLabel.Should().Be("1.1");
        (await db.DocumentVersions.SingleAsync(v => v.Id == revision2Id)).VersionLabel.Should().Be("1.2");
    }

    [Fact]
    public async Task Approving_a_new_draft_supersedes_the_previously_approved_version()
    {
        await using var db = NewContext();
        var currentUser = FakeCurrentUser();
        var projectId = Guid.NewGuid();

        var documentId = await new CreateDocumentCommandHandler(db, currentUser)
            .Handle(new CreateDocumentCommand(projectId, "Communications Plan", "Stakeholder", null), CancellationToken.None);

        var firstDraftId = (await db.DocumentVersions.SingleAsync(v => v.DocumentId == documentId)).Id;
        await new ApproveVersionCommandHandler(db, currentUser).Handle(new ApproveVersionCommand(firstDraftId), CancellationToken.None);

        var secondDraftId = await new CreateDraftRevisionCommandHandler(db, currentUser)
            .Handle(new CreateDraftRevisionCommand(documentId), CancellationToken.None);
        await new ApproveVersionCommandHandler(db, currentUser).Handle(new ApproveVersionCommand(secondDraftId), CancellationToken.None);

        var firstVersion = await db.DocumentVersions.SingleAsync(v => v.Id == firstDraftId);
        var secondVersion = await db.DocumentVersions.SingleAsync(v => v.Id == secondDraftId);

        firstVersion.Status.Should().Be(DocumentVersionStatus.Superseded);
        firstVersion.VersionLabel.Should().Be("2.0");
        secondVersion.Status.Should().Be(DocumentVersionStatus.Approved);
        secondVersion.VersionLabel.Should().Be("3.0");
    }

    [Fact]
    public async Task Approving_an_already_approved_version_throws()
    {
        await using var db = NewContext();
        var currentUser = FakeCurrentUser();
        var projectId = Guid.NewGuid();

        var documentId = await new CreateDocumentCommandHandler(db, currentUser)
            .Handle(new CreateDocumentCommand(projectId, "Risk Register", "Risk", null), CancellationToken.None);
        var versionId = (await db.DocumentVersions.SingleAsync(v => v.DocumentId == documentId)).Id;

        await new ApproveVersionCommandHandler(db, currentUser).Handle(new ApproveVersionCommand(versionId), CancellationToken.None);

        var act = () => new ApproveVersionCommandHandler(db, currentUser).Handle(new ApproveVersionCommand(versionId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
