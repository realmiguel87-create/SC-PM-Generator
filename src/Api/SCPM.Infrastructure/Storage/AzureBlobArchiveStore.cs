using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using SCPM.Application.Common.Interfaces;

namespace SCPM.Infrastructure.Storage;

/// <summary>
/// The archive tier: copies a file from the active-tier container (AzureBlobDocumentStore) into
/// the archive container, both in the same storage account. Uses the Blob SDK's own
/// authenticated container/blob clients on both ends rather than an anonymous HTTP GET on the
/// source URL — the active-tier container is not public, so a plain HttpClient request against
/// its blob URL would 403 rather than actually read the file.
/// </summary>
public class AzureBlobArchiveStore : IBlobArchiveStore
{
    private readonly BlobServiceClient _serviceClient;
    private readonly BlobContainerClient _archiveContainerClient;

    public AzureBlobArchiveStore(IOptions<BlobStorageOptions> options)
    {
        var opts = options.Value;
        _serviceClient = new BlobServiceClient(opts.ConnectionString);
        _archiveContainerClient = _serviceClient.GetBlobContainerClient(opts.ArchiveContainerName);
    }

    public async Task<string> ArchiveAsync(string sourceUrl, string blobPath, CancellationToken cancellationToken)
    {
        await _archiveContainerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        // sourceUrl is one of our own active-tier blob URLs (see AzureBlobDocumentStore) —
        // "https://{account}.blob.core.windows.net/{container}/{blobName...}". Parsed back into
        // container + blob name so the copy can go through our own authenticated
        // BlobServiceClient rather than treating the URL as a public link.
        var path = new Uri(sourceUrl).AbsolutePath.TrimStart('/');
        var separatorIndex = path.IndexOf('/');
        var sourceContainerName = Uri.UnescapeDataString(path[..separatorIndex]);
        var sourceBlobName = Uri.UnescapeDataString(path[(separatorIndex + 1)..]);

        var sourceBlobClient = _serviceClient.GetBlobContainerClient(sourceContainerName).GetBlobClient(sourceBlobName);
        var destinationBlobClient = _archiveContainerClient.GetBlobClient(blobPath);

        await using var sourceStream = await sourceBlobClient.OpenReadAsync(cancellationToken: cancellationToken);
        await destinationBlobClient.UploadAsync(sourceStream, overwrite: false, cancellationToken);

        return destinationBlobClient.Uri.ToString();
    }
}
