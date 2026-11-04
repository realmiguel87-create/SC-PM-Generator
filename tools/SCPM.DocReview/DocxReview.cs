using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using O13 = DocumentFormat.OpenXml.Office2013.Word;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace SCPM.DocReview;

/// <summary>A comment somebody left in a document, and what they left it on.</summary>
/// <param name="On">
/// The text the comment is anchored to. A comment knows nothing about what it is about — only the
/// document does, through a commentRangeStart/End pair carrying the comment's id — so this is read
/// from the body rather than from the comment. It is also the reason a marked-up document beats a
/// description: "the sponsor label is wrong" has to be hunted for across seventy-seven documents,
/// where an anchor names the words.
/// </param>
/// <param name="ReplyToId">The comment this one replies to, or null when it opens a thread.</param>
/// <param name="Resolved">Whether the thread has been marked resolved in Word.</param>
public record DocxComment(
    string Id,
    string Author,
    string Initials,
    string Date,
    string Text,
    string On,
    string? ReplyToId,
    bool Resolved);

/// <summary>An insertion or deletion made with Track Changes on.</summary>
public record DocxTrackedChange(string Kind, string Author, string Date, string Text);

public record DocxReviewResult(
    string FileName,
    IReadOnlyList<DocxComment> Comments,
    IReadOnlyList<DocxTrackedChange> TrackedChanges);

/// <summary>
/// Reads Word comments and tracked changes out of a .docx.
///
/// The council marks up a generated document and sends it back; this is how that markup is read.
/// Read-only by design: a correction belongs in the document definition that generates every
/// future copy, not in the one file somebody annotated. Editing the returned copy would fix one
/// document and leave the generator producing the same defect for ever.
///
/// Threading is the fiddly part. Word does not record a reply against the comment it answers; it
/// records it against that comment's last <em>paragraph</em> id, in a separate part. So the two
/// have to be joined — paragraph id to comment id — to reconstruct who replied to whom.
/// </summary>
public static class DocxReview
{
    public static DocxReviewResult Read(string path)
    {
        using var document = WordprocessingDocument.Open(path, false);
        var main = document.MainDocumentPart
            ?? throw new InvalidOperationException($"{path} has no document part.");

        return new DocxReviewResult(
            Path.GetFileName(path),
            ReadComments(main),
            ReadTrackedChanges(main));
    }

    private static IReadOnlyList<DocxComment> ReadComments(MainDocumentPart main)
    {
        var commentsPart = main.WordprocessingCommentsPart;
        if (commentsPart?.Comments is null) return [];

        var anchors = ReadAnchors(main);

        // Each comment's own paragraph ids, so a reply recorded against a paragraph can be traced
        // back to the comment that paragraph belongs to.
        var commentOfParagraph = new Dictionary<string, string>();
        foreach (var comment in commentsPart.Comments.Elements<W.Comment>())
        {
            var id = comment.Id?.Value;
            if (id is null) continue;

            foreach (var paragraph in comment.Descendants<W.Paragraph>())
            {
                var paraId = paragraph.ParagraphId?.Value;
                if (paraId is not null) commentOfParagraph[paraId] = id;
            }
        }

        var parentOfParagraph = new Dictionary<string, string>();
        var resolvedParagraphs = new HashSet<string>();

        if (main.WordprocessingCommentsExPart?.CommentsEx is { } extended)
        {
            foreach (var entry in extended.Elements<O13.CommentEx>())
            {
                var paraId = entry.ParaId?.Value;
                if (paraId is null) continue;

                if (entry.ParaIdParent?.Value is { } parent) parentOfParagraph[paraId] = parent;
                if (entry.Done?.Value == true) resolvedParagraphs.Add(paraId);
            }
        }

        var results = new List<DocxComment>();

        foreach (var comment in commentsPart.Comments.Elements<W.Comment>())
        {
            var id = comment.Id?.Value;
            if (id is null) continue;

            var paragraphIds = comment.Descendants<W.Paragraph>()
                .Select(p => p.ParagraphId?.Value)
                .Where(p => p is not null)
                .Select(p => p!)
                .ToList();

            string? replyTo = null;
            foreach (var paraId in paragraphIds)
            {
                if (parentOfParagraph.TryGetValue(paraId, out var parent)
                    && commentOfParagraph.TryGetValue(parent, out var parentComment))
                {
                    replyTo = parentComment;
                    break;
                }
            }

            results.Add(new DocxComment(
                id,
                comment.Author?.Value ?? string.Empty,
                comment.Initials?.Value ?? string.Empty,
                comment.Date?.Value.ToString("yyyy-MM-dd HH:mm") ?? string.Empty,
                Flatten(comment),
                anchors.GetValueOrDefault(id, string.Empty),
                replyTo,
                paragraphIds.Any(resolvedParagraphs.Contains)));
        }

        // Numerically where the ids are numbers, which they are as Word writes them. Ordering by
        // the string would put comment 10 before comment 2.
        return results
            .OrderBy(c => int.TryParse(c.Id, out var n) ? n : int.MaxValue)
            .ThenBy(c => c.Id, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// The text each comment is attached to, by walking the body in document order and collecting
    /// what falls between a range start and its matching end.
    ///
    /// Ranges overlap: two comments on the same words, or one inside another, are both normal, so
    /// several can be open at once and every open id collects the text.
    /// </summary>
    private static Dictionary<string, string> ReadAnchors(MainDocumentPart main)
    {
        var collected = new Dictionary<string, System.Text.StringBuilder>();
        var open = new HashSet<string>();

        foreach (var element in main.Document.Descendants())
        {
            switch (element)
            {
                case W.CommentRangeStart start when start.Id?.Value is { } startId:
                    open.Add(startId);
                    collected.TryAdd(startId, new System.Text.StringBuilder());
                    break;

                case W.CommentRangeEnd end when end.Id?.Value is { } endId:
                    open.Remove(endId);
                    break;

                case W.Text text when open.Count > 0:
                    foreach (var id in open) collected[id].Append(text.Text);
                    break;

                // A comment anchored to no range at all — Word allows a point comment — leaves an
                // empty entry rather than being dropped, so it still reports.
                case W.CommentReference reference when reference.Id?.Value is { } referenceId:
                    collected.TryAdd(referenceId, new System.Text.StringBuilder());
                    break;
            }
        }

        return collected.ToDictionary(pair => pair.Key, pair => Collapse(pair.Value.ToString()));
    }

    private static IReadOnlyList<DocxTrackedChange> ReadTrackedChanges(MainDocumentPart main)
    {
        var changes = new List<DocxTrackedChange>();

        foreach (var inserted in main.Document.Descendants<W.InsertedRun>())
        {
            var text = Flatten(inserted);
            if (text.Length > 0)
            {
                changes.Add(new DocxTrackedChange(
                    "inserted",
                    inserted.Author?.Value ?? string.Empty,
                    inserted.Date?.Value.ToString("yyyy-MM-dd HH:mm") ?? string.Empty,
                    text));
            }
        }

        foreach (var deleted in main.Document.Descendants<W.DeletedRun>())
        {
            // Deleted text is held in w:delText, not w:t — reading only w:t reports every
            // deletion as empty, which looks like no deletions at all.
            var text = Collapse(string.Concat(
                deleted.Descendants<W.DeletedText>().Select(t => t.Text)));

            if (text.Length > 0)
            {
                changes.Add(new DocxTrackedChange(
                    "deleted",
                    deleted.Author?.Value ?? string.Empty,
                    deleted.Date?.Value.ToString("yyyy-MM-dd HH:mm") ?? string.Empty,
                    text));
            }
        }

        return changes;
    }

    private static string Flatten(OpenXmlElement element)
    {
        var builder = new System.Text.StringBuilder();

        foreach (var node in element.Descendants())
        {
            switch (node)
            {
                case W.Text text: builder.Append(text.Text); break;
                case W.TabChar: builder.Append(' '); break;
                case W.Break: builder.Append('\n'); break;
            }
        }

        return Collapse(builder.ToString());
    }

    /// <summary>Trims and collapses runs of whitespace, so a comment split across runs reads as one line.</summary>
    private static string Collapse(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
