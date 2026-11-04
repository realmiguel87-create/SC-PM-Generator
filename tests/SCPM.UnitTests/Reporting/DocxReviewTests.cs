using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using FluentAssertions;
using SCPM.DocReview;
using Xunit;
using O13 = DocumentFormat.OpenXml.Office2013.Word;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace SCPM.UnitTests.Reporting;

/// <summary>
/// Reading back the comments and tracked changes a reviewer leaves in a generated document.
///
/// The fixture builds a document the way Word does — a comments part, an extended comments part
/// for threading, and commentRangeStart/End pairs in the body — rather than asserting against
/// something written to match the reader. A reader tested only against its own author's idea of
/// the format passes until a real file arrives.
///
/// All fixture content is invented.
/// </summary>
public class DocxReviewTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), "scpm-docreview-" + Guid.NewGuid().ToString("N"));

    public DocxReviewTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static W.Paragraph Anchored(string text, params int[] commentIds)
    {
        var paragraph = new W.Paragraph();

        foreach (var id in commentIds)
        {
            paragraph.Append(new W.CommentRangeStart { Id = id.ToString() });
        }

        paragraph.Append(new W.Run(new W.Text(text) { Space = SpaceProcessingModeValues.Preserve }));

        foreach (var id in commentIds)
        {
            paragraph.Append(new W.CommentRangeEnd { Id = id.ToString() });
            paragraph.Append(new W.Run(new W.CommentReference { Id = id.ToString() }));
        }

        return paragraph;
    }

    private static W.Comment Comment(int id, string author, string initials, string text, string paraId)
    {
        var paragraph = new W.Paragraph(
            new W.Run(new W.Text(text) { Space = SpaceProcessingModeValues.Preserve }))
        {
            ParagraphId = paraId,
        };

        return new W.Comment(paragraph)
        {
            Id = id.ToString(),
            Author = author,
            Initials = initials,
            Date = new DateTime(2026, 11, 4, 9, 30, 0, DateTimeKind.Utc),
        };
    }

    /// <summary>A marked-up document: two threads, a reply, a resolved thread, and both kinds of edit.</summary>
    private string MarkedUpDocument()
    {
        var path = Path.Combine(_directory, "Gate-Report-marked-up.docx");

        using (var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();

            var body = new W.Body(
                Anchored("Project Sponsor", 1),
                Anchored("Risks and Issues", 2, 3),
                new W.Paragraph(
                    new W.Run(new W.Text("Conditions")),
                    new W.InsertedRun(
                        new W.Run(new W.Text(" and Assumptions") { Space = SpaceProcessingModeValues.Preserve }))
                    {
                        Id = "900",
                        Author = "B. Example",
                        Date = new DateTime(2026, 11, 4, 10, 5, 0, DateTimeKind.Utc),
                    },
                    new W.DeletedRun(
                        new W.Run(new W.DeletedText("Draft only") { Space = SpaceProcessingModeValues.Preserve }))
                    {
                        Id = "901",
                        Author = "M. Rodden",
                        Date = new DateTime(2026, 11, 4, 10, 7, 0, DateTimeKind.Utc),
                    }),
                Anchored("Recommendation", 4));

            main.Document = new W.Document(body);

            var commentsPart = main.AddNewPart<WordprocessingCommentsPart>();
            commentsPart.Comments = new W.Comments(
                Comment(1, "M. Rodden", "MR", "Our wording is \"Project Sponsor\", not \"Sponsor\".", "0A000001"),
                Comment(2, "M. Rodden", "MR", "This section should come after Cost.", "0A000002"),
                Comment(3, "B. Example", "BE", "Agreed, and the clause reference is needed.", "0A000003"),
                Comment(4, "M. Rodden", "MR", "Fine as it stands.", "0A000004"));
            commentsPart.Comments.Save();

            var extendedPart = main.AddNewPart<WordprocessingCommentsExPart>();
            extendedPart.CommentsEx = new O13.CommentsEx(
                new O13.CommentEx { ParaId = "0A000001", Done = false },
                new O13.CommentEx { ParaId = "0A000002", Done = false },
                // Comment 3 replies to comment 2 — recorded against comment 2's paragraph id,
                // not against comment 2 itself, which is the join the reader has to make.
                new O13.CommentEx { ParaId = "0A000003", ParaIdParent = "0A000002", Done = false },
                new O13.CommentEx { ParaId = "0A000004", Done = true });
            extendedPart.CommentsEx.Save();

            main.Document.Save();
        }

        return path;
    }

    [Fact]
    public void Reads_each_comment_with_its_author()
    {
        var result = DocxReview.Read(MarkedUpDocument());

        result.Comments.Should().HaveCount(4);
        result.Comments[0].Author.Should().Be("M. Rodden");
        result.Comments[0].Initials.Should().Be("MR");
        result.Comments[0].Text.Should().Be("Our wording is \"Project Sponsor\", not \"Sponsor\".");
        result.Comments[0].Date.Should().StartWith("2026-11-04");
    }

    [Fact]
    public void Reads_the_text_each_comment_is_anchored_to()
    {
        var result = DocxReview.Read(MarkedUpDocument());

        // The anchor is what makes a marked-up document better than a description: it names the
        // words rather than leaving them to be hunted for across seventy-seven documents.
        result.Comments.Single(c => c.Id == "1").On.Should().Be("Project Sponsor");
        result.Comments.Single(c => c.Id == "4").On.Should().Be("Recommendation");
    }

    [Fact]
    public void Handles_two_comments_on_the_same_words()
    {
        var result = DocxReview.Read(MarkedUpDocument());

        // Overlapping ranges are normal — a reply thread puts two ids over the same text — so
        // several ranges can be open at once and each must collect it.
        result.Comments.Single(c => c.Id == "2").On.Should().Be("Risks and Issues");
        result.Comments.Single(c => c.Id == "3").On.Should().Be("Risks and Issues");
    }

    [Fact]
    public void Reconstructs_who_replied_to_whom()
    {
        var result = DocxReview.Read(MarkedUpDocument());

        // Word records a reply against the parent comment's last paragraph id, in a separate
        // part, so the paragraph-to-comment join is the whole of this feature.
        result.Comments.Single(c => c.Id == "3").ReplyToId.Should().Be("2");
        result.Comments.Single(c => c.Id == "2").ReplyToId.Should().BeNull();
        result.Comments.Single(c => c.Id == "1").ReplyToId.Should().BeNull();
    }

    [Fact]
    public void Reports_which_threads_are_resolved()
    {
        var result = DocxReview.Read(MarkedUpDocument());

        // A resolved thread still appears — it is useful to see what was raised and settled —
        // but it should not be acted on as though it were open.
        result.Comments.Single(c => c.Id == "4").Resolved.Should().BeTrue();
        result.Comments.Single(c => c.Id == "1").Resolved.Should().BeFalse();
    }

    [Fact]
    public void Reads_insertions_and_deletions()
    {
        var result = DocxReview.Read(MarkedUpDocument());

        var inserted = result.TrackedChanges.Single(c => c.Kind == "inserted");
        inserted.Author.Should().Be("B. Example");
        inserted.Text.Should().Be("and Assumptions");

        // Deleted text is in w:delText rather than w:t. Reading only w:t reports every deletion
        // as empty, which is indistinguishable from there being none.
        var deleted = result.TrackedChanges.Single(c => c.Kind == "deleted");
        deleted.Author.Should().Be("M. Rodden");
        deleted.Text.Should().Be("Draft only");
    }

    [Fact]
    public void Orders_comments_the_way_word_numbers_them()
    {
        var result = DocxReview.Read(MarkedUpDocument());

        // Numerically. Ordered as strings, comment 10 would come before comment 2 — which only
        // shows up once a reviewer leaves ten comments, by which point the order looks random.
        result.Comments.Select(c => c.Id).Should().ContainInOrder("1", "2", "3", "4");
    }

    [Fact]
    public void Returns_nothing_for_a_document_nobody_has_marked_up()
    {
        var path = Path.Combine(_directory, "clean.docx");

        using (var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new W.Document(new W.Body(new W.Paragraph(new W.Run(new W.Text("Clean")))));
        }

        var result = DocxReview.Read(path);

        // The ordinary case for most of a folder, and it must not throw: a document with no
        // comments part is not a broken document.
        result.Comments.Should().BeEmpty();
        result.TrackedChanges.Should().BeEmpty();
    }

    [Fact]
    public void Reads_a_real_generated_template_without_comments()
    {
        // The documents this tool exists to read are the ones the platform generates, so it is
        // exercised against one rather than only against a fixture built for it.
        var report = new SCPM.Application.Reporting.Dtos.CommitteeReportDto
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            ProjectName = "Bridgeview Primary School",
            ProjectRef = "ID00001",
            ReportType = nameof(SCPM.Domain.Enums.CommitteeReportType.ProjectMandate),
            Title = "Project Mandate",
            Status = "Draft",
            CreatedDate = DateTime.UtcNow,
            Sections = SCPM.Domain.Common.StageDocuments.ProjectMandate.Sections
                .Select(s => new SCPM.Application.Reporting.Dtos.ReportSectionDto
                {
                    Key = s.Key, Heading = s.Heading, Prompt = s.Prompt,
                })
                .ToList(),
        };

        var bytes = new SCPM.Infrastructure.Reporting.CommitteeReportExporter()
            .ExportAsync(report, SCPM.Application.Common.Interfaces.ReportExportFormat.Docx,
                CancellationToken.None)
            .GetAwaiter().GetResult();

        var path = Path.Combine(_directory, "generated.docx");
        File.WriteAllBytes(path, bytes);

        var result = DocxReview.Read(path);

        result.Comments.Should().BeEmpty();
        result.FileName.Should().Be("generated.docx");
    }
}
