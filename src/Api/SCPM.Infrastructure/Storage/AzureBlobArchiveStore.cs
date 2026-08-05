using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using SCPM.Application.Common.Interfaces;

namespace SCPM.Infrastructure.Storage;

/// <summary>
/// The archive tier: pulls a file from its current SharePoint URL and copies it into Azure Blob
/// Storage. Downloading via HttpClient rather than Graph's drive-item content API keeps this
/// class decoupled from ISharePointDocumentStore's implementation — it only needs a URL a
/// bearer-token-authenticated request can read, which is what SharePoint's WebUrl gives it via
/// the same Graph-issued access already covering the site.
/// </summary>
public class AzureBlobArchiveStore : IBlobArchiveStore
{
    private readonly BlobContainerClient _containerClient;
    private readonly HttpClient _httpClient;

    public AzureBlobArchiveStore(IOptions<BlobArchiveOptions> options, IHttpClientFactory httpClientFactory)
    {
        var opts = options.Value;
        var serviceClient = new BlobServiceClient(opts.ConnectionString);
        _containerClient = serviceClient.GetBlobContainerClient(opts.ContainerName);
        _httpClient = httpClientFactory.CreateClient(nameof(AzureBlobArchiveStore));
    }

    public async Task<string> ArchiveAsync(string sourceSharePointUrl, string blobPath, CancellationToken cancellationToken)
    {
        await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        var blobClient = _containerClient.GetBlobClient(blobPath);

        using var response = await _httpClient.GetAsync(sourceSharePointUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await blobClient.UploadAsync(sourceStream, overwrite: false, cancellationToken);

        return blobClient.Uri.ToString();
    }
}
