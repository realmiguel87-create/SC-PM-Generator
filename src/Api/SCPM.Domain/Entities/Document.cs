using SCPM.Domain.Common;
using SCPM.Domain.Enums;

namespace SCPM.Domain.Entities;

/// <summary>The logical document record (e.g. "Project Execution Plan") — stable across every
/// version it ever has. The version history lives in DocumentVersion.</summary>
public class Document : SoftDeletableEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = default!;

    public byte? RibaStageNumber { get; set; }
    public string Title { get; set; } = default!;
    public string Category { get; set; } = default!; // e.g. Governance, Cost, Programme, Risk, Stakeholder, Handover

    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
}

/// <summary>
/// One version in a document's history. Versions are never overwritten or deleted — approving a
/// draft transitions *that row* to Approved and bumps its version number (see
/// ApproveVersionCommand), and any previously Approved version for the same document moves to
/// Superseded. The physical files backing a version live in DocumentFile, one row per exported
/// format, so a version can carry a PDF and a DOCX side by side without either overwriting the
/// other.
/// </summary>
public class DocumentVersion : SoftDeletableEntity
{
    public Guid DocumentId { get; set; }
    public Document Document { get; set; } = default!;

    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public DocumentVersionStatus Status { get; set; } = DocumentVersionStatus.Draft;

    /// <summary>Set when this version was captured as part of a snapshot (e.g. a committee submission pack).</summary>
    public Guid? SnapshotId { get; set; }
    public Snapshot? Snapshot { get; set; }

    public string VersionLabel => $"{MajorVersion}.{MinorVersion}";

    public ICollection<DocumentFile> Files { get; set; } = new List<DocumentFile>();
}

/// <summary>
/// A physical exported file backing a DocumentVersion — metadata lives here in SQL Server, the
/// file itself in Azure Blob Storage (the active-tier container, or once superseded/archived, the
/// separate archive-tier container). See docs/architecture.md §9 and the IDocumentStore /
/// IBlobArchiveStore interfaces.
/// </summary>
public class DocumentFile : BaseEntity
{
    public Guid DocumentVersionId { get; set; }
    public DocumentVersion DocumentVersion { get; set; } = default!;

    public string FileType { get; set; } = default!; // pdf, docx, xlsx, pptx, csv, json
    public string Category { get; set; } = default!;
    public string FileName { get; set; } = default!;

    public string? StorageUrl { get; set; }
    public string? BlobArchiveUrl { get; set; }
    public long SizeBytes { get; set; }
}
