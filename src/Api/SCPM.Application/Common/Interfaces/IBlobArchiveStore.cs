namespace SCPM.Application.Common.Interfaces;

/// <summary>
/// The archive tier (docs/architecture.md §9) — Azure Blob Storage, for superseded/archived
/// document versions moved out of SharePoint's active document libraries. Implemented by
/// SCPM.Infrastructure/Storage/AzureBlobArchiveStore.
/// </summary>
public interface IBlobArchiveStore
{
    /// <summary>Copies a file from its current SharePoint URL into the archive container and
    /// returns its blob URL. The source is not deleted here — that's a deliberate separate step
    /// so a failed archive never leaves a file unreachable from both locations at once.</summary>
    Task<string> ArchiveAsync(string sourceSharePointUrl, string blobPath, CancellationToken cancellationToken);
}
