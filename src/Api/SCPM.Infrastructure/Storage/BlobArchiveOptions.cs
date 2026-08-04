namespace SCPM.Infrastructure.Storage;

/// <summary>Bound from configuration section "BlobArchive" — see appsettings.json.</summary>
public class BlobArchiveOptions
{
    public string ConnectionString { get; set; } = default!;
    public string ContainerName { get; set; } = "document-archive";
}
