namespace SCPM.Application.Common.Interfaces;

/// <summary>
/// The primary document store (docs/architecture.md §9) — SharePoint Online, via Microsoft
/// Graph. Implemented by SCPM.Infrastructure/SharePoint/GraphSharePointDocumentStore, which
/// needs a real Entra ID app registration and SharePoint site to actually exercise; there is no
/// way to test that path without one, so this interface exists precisely so the rest of the
/// system (and its tests) never has to know or care.
/// </summary>
public interface ISharePointDocumentStore
{
    /// <summary>Uploads a file into the project's document library and returns its SharePoint URL.
    /// Every call creates a new file — this store never overwrites an existing one.</summary>
    Task<string> UploadAsync(string projectRef, string fileName, Stream content, string contentType, CancellationToken cancellationToken);
}
