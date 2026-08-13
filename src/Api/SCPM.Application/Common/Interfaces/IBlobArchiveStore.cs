namespace SCPM.Application.Common.Interfaces;

/// <summary>
/// The archive tier (docs/architecture.md §9) — a separate Azure Blob Storage container, for
/// superseded/archived document versions moved out of the active-tier container backing
/// IDocumentStore. Implemented by SCPM.Infrastructure/Storage/AzureBlobArchiveStore.
/// </summary>
public interface IBlobArchiveStore
{
    /// <summary>Copies a file from its current IDocumentStore URL into the archive container and
    /// returns its blob URL. The source is not deleted here — that's a deliberate separate step
    /// so a failed archive never leaves a file unreachable from both locations at once.</summary>
    Task<string> ArchiveAsync(string sourceUrl, string blobPath, CancellationToken cancellationToken);
}
