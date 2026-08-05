namespace SCPM.Application.Common;

/// <summary>
/// Strips any directory component from a client-supplied file name. IFormFile.FileName (the
/// only caller today — AddDocumentFileCommand) is attacker-controlled: a crafted multipart
/// upload can set it to "../../other-project/secret.pdf" or similar. Sanitising here, at the
/// point the name is first accepted and persisted, means every downstream consumer of
/// DocumentFile.FileName (the SharePoint upload path, the Azure Blob archive path built in
/// ArchiveVersionCommand) inherits the safe value rather than each needing its own defence.
/// </summary>
public static class FileNameSanitizer
{
    public static string Sanitise(string fileName)
    {
        var name = fileName.Replace('\\', '/');
        var lastSlash = name.LastIndexOf('/');
        if (lastSlash >= 0)
            name = name[(lastSlash + 1)..];

        return string.IsNullOrWhiteSpace(name) ? "unnamed-file" : name;
    }
}
