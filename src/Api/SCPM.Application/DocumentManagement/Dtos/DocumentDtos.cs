namespace SCPM.Application.DocumentManagement.Dtos;

public class DocumentFileDto
{
    public Guid Id { get; set; }
    public string FileType { get; set; } = default!;
    public string Category { get; set; } = default!;
    public string FileName { get; set; } = default!;
    public string? StorageUrl { get; set; }
    public string? BlobArchiveUrl { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class DocumentVersionDto
{
    public Guid Id { get; set; }
    public string VersionLabel { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTime CreatedDate { get; set; }
    public List<DocumentFileDto> Files { get; set; } = new();
}

public class DocumentListItemDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = default!;
    public string Category { get; set; } = default!;
    public byte? RibaStageNumber { get; set; }
    public string LatestVersionLabel { get; set; } = default!;
    public string LatestVersionStatus { get; set; } = default!;
}

public class DocumentDetailDto : DocumentListItemDto
{
    public List<DocumentVersionDto> Versions { get; set; } = new();
}
