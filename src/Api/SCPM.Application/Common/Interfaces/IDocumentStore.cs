namespace SCPM.Application.Common.Interfaces;

/// <summary>
/// The primary document store (docs/architecture.md §9) — Azure Blob Storage's active-tier
/// container. Implemented by SCPM.Infrastructure/Storage/AzureBlobDocumentStore. Originally
/// backed by SharePoint Online via Microsoft Graph, which needed a tenant admin to grant
/// application-permission consent (Sites.ReadWrite.All or a site-scoped equivalent); switched to
/// Blob Storage because that consent was not obtainable, and a storage account connection string
/// needs no tenant admin at all. Kept as its own interface, separate from IBlobArchiveStore, so
/// the rest of the system (and its tests) never has to know or care which backend is behind it.
/// </summary>
public interface IDocumentStore
{
    /// <summary>Uploads a file into the project's document store and returns its URL.
    /// Every call creates a new file — this store never overwrites an existing one.</summary>
    Task<string> UploadAsync(string projectRef, string fileName, Stream content, string contentType, CancellationToken cancellationToken);
}
