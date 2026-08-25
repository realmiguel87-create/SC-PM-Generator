namespace SCPM.Application.Reporting.Export;

/// <summary>One table in an exported document: a heading, column names, and rows of cells.</summary>
public sealed record ExportTable(
    string Heading,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<string>> Rows)
{
    /// <summary>Shown in place of the rows when there are none, so an empty section reads as an
    /// answer ("nothing changed") rather than as a rendering failure.</summary>
    public string? EmptyMessage { get; init; }
}

/// <summary>
/// A document to be exported, described independently of any format.
///
/// The committee report exporter renders prose: a heading and a paragraph per section. A snapshot
/// comparison is tabular, and rebuilding six format-specific renderers around that difference
/// would have meant six places to change every time a column moved. This is the shared shape
/// instead — build it once, render it six ways.
/// </summary>
public sealed record ExportDocument(
    string Title,
    string Subtitle,
    IReadOnlyList<string> MetaLines,
    IReadOnlyList<ExportTable> Tables);
