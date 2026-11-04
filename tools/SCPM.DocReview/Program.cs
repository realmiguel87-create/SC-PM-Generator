using SCPM.DocReview;

// Reads Word comments and tracked changes out of generated documents the council has marked up.
//
//   dotnet run --project tools/SCPM.DocReview -- <file-or-directory> [...]
//
// A directory is read recursively, which is the normal case: a reviewer marks up several
// documents and sends a folder or a zip of them.

if (args.Length == 0)
{
    Console.Error.WriteLine("usage: scpm-docreview <file-or-directory> [...]");
    Console.Error.WriteLine();
    Console.Error.WriteLine("Reads comments and tracked changes from .docx files.");
    return 2;
}

var files = new List<string>();

foreach (var argument in args)
{
    if (Directory.Exists(argument))
    {
        // Word's lock files start with "~$" and are not documents; including them produces a
        // confusing failure on a folder somebody has open.
        files.AddRange(Directory
            .EnumerateFiles(argument, "*.docx", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith("~$", StringComparison.Ordinal)));
    }
    else if (File.Exists(argument))
    {
        files.Add(argument);
    }
    else
    {
        Console.Error.WriteLine($"not found: {argument}");
        return 2;
    }
}

files.Sort(StringComparer.OrdinalIgnoreCase);

var reviewed = 0;
var totalComments = 0;
var totalChanges = 0;
var failed = 0;

foreach (var file in files)
{
    DocxReviewResult result;

    try
    {
        result = DocxReview.Read(file);
    }
    catch (Exception exception)
    {
        // One unreadable file should not stop a folder of thirty being reviewed.
        Console.Error.WriteLine($"could not read {Path.GetFileName(file)}: {exception.Message}");
        failed++;
        continue;
    }

    reviewed++;

    // Files nobody marked up are the majority of any folder and are not worth printing.
    if (result.Comments.Count == 0 && result.TrackedChanges.Count == 0) continue;

    totalComments += result.Comments.Count;
    totalChanges += result.TrackedChanges.Count;

    Console.WriteLine();
    Console.WriteLine($"=== {result.FileName} ===");
    Console.WriteLine($"{result.Comments.Count} comment(s), {result.TrackedChanges.Count} tracked change(s)");

    foreach (var comment in result.Comments)
    {
        var lead = $"  [{comment.Id}]";
        if (comment.ReplyToId is not null) lead += $" reply to [{comment.ReplyToId}]";
        if (comment.Resolved) lead += " (resolved)";

        Console.WriteLine($"{lead} {comment.Author} — {comment.Date}");

        if (comment.On.Length > 0)
        {
            Console.WriteLine($"       on: “{Shorten(comment.On)}”");
        }

        Console.WriteLine($"       {comment.Text}");
    }

    foreach (var change in result.TrackedChanges)
    {
        Console.WriteLine($"  <{change.Kind}> {change.Author}: {Shorten(change.Text)}");
    }
}

Console.WriteLine();
Console.WriteLine(
    $"{reviewed} file(s) read, {totalComments} comment(s), {totalChanges} tracked change(s)"
    + (failed > 0 ? $", {failed} unreadable" : string.Empty));

return failed > 0 ? 1 : 0;

static string Shorten(string value) =>
    value.Length <= 120 ? value : value[..117] + "…";
