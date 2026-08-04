using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using SCPM.Application.Common.Interfaces;

namespace SCPM.Infrastructure.SharePoint;

/// <summary>
/// Uploads files to the project's SharePoint Online document library via Microsoft Graph.
/// Requires a real Entra ID app registration (client credentials flow, Sites.ReadWrite.All or a
/// site-scoped equivalent) and SharePoint site — this environment has neither, so this class
/// compiles against the real Microsoft.Graph SDK surface but has not been exercised against a
/// live tenant. See docs/roadmap.md.
/// </summary>
public class GraphSharePointDocumentStore : ISharePointDocumentStore
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _siteId;

    public GraphSharePointDocumentStore(IOptions<SharePointOptions> options)
    {
        var opts = options.Value;
        var credential = new ClientSecretCredential(opts.TenantId, opts.ClientId, opts.ClientSecret);
        _graphClient = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
        _siteId = opts.SiteId;
    }

    public async Task<string> UploadAsync(string projectRef, string fileName, Stream content, string contentType, CancellationToken cancellationToken)
    {
        // GUID-prefixed so two uploads for the same document/filename can never collide —
        // "never overwrite files" (docs/erd.md) applies at the SharePoint layer too, not just SQL.
        var path = $"{projectRef}/{Guid.NewGuid():N}-{fileName}";

        // Sites[id].Drive only exposes GetAsync (it's the site's drive *reference*, not the
        // drive's own item tree) — the Root/ItemWithPath navigation lives under Drives[driveId].
        var drive = await _graphClient.Sites[_siteId].Drive.GetAsync(cancellationToken: cancellationToken);
        var driveId = drive?.Id
            ?? throw new InvalidOperationException($"Could not resolve the document library drive for site '{_siteId}'.");

        var driveItem = await _graphClient.Drives[driveId].Root
            .ItemWithPath(path)
            .Content
            .PutAsync(content, cancellationToken: cancellationToken);

        return driveItem?.WebUrl
            ?? throw new InvalidOperationException($"SharePoint upload of '{path}' did not return a file URL.");
    }
}
