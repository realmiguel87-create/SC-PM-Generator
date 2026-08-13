namespace SCPM.Infrastructure.Storage;

/// <summary>Bound from configuration section "BlobStorage" — see appsettings.json. Both the
/// active tier (AzureBlobDocumentStore) and the archive tier (AzureBlobArchiveStore) live in the
/// same storage account, in two separate containers, so they share one connection string.</summary>
public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = default!;
    public string ActiveContainerName { get; set; } = "documents-active";
    public string ArchiveContainerName { get; set; } = "document-archive";
}
