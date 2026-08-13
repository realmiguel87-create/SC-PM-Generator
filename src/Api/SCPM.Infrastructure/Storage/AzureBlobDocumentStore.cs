using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using SCPM.Application.Common.Interfaces;

namespace SCPM.Infrastructure.Storage;

/// <summary>
/// The active-tier document store: uploads project document files straight to Azure Blob
/// Storage. Originally this was SharePoint Online via Microsoft Graph, but Graph's app-only
/// access needs a tenant admin to grant application-permission consent, which was not available
/// here — a storage account connection string needs no such consent, so this replaced it.
/// </summary>
public class AzureBlobDocumentStore : IDocumentStore
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobDocumentStore(IOptions<BlobStorageOptions> options)
    {
        var opts = options.Value;
        var serviceClient = new BlobServiceClient(opts.ConnectionString);
        _containerClient = serviceClient.GetBlobContainerClient(opts.ActiveContainerName);
    }

    public async Task<string> UploadAsync(string projectRef, string fileName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        // GUID-prefixed so two uploads for the same project/file name can never collide — "never
        // overwrite files" (docs/erd.md) applies here the same way it did under SharePoint.
        var blobName = $"{projectRef}/{Guid.NewGuid():N}-{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        // BlobClient.UploadAsync(Stream, BlobUploadOptions, ...) overwrites by default — the SDK
        // docs are explicit that Conditions is the only thing that prevents it. IfNoneMatch =
        // ETag.All ("*") means "only create if no blob exists at this path yet", preserving the
        // GUID-prefixed path's "never overwrite" guarantee rather than relying on it alone.
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
            Conditions = new BlobRequestConditions { IfNoneMatch = ETag.All }
        };
        await blobClient.UploadAsync(content, uploadOptions, cancellationToken);

        return blobClient.Uri.ToString();
    }
}
