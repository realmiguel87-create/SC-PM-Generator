namespace SCPM.Infrastructure.SharePoint;

/// <summary>Bound from configuration section "SharePoint" — see appsettings.json.</summary>
public class SharePointOptions
{
    public string TenantId { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string SiteId { get; set; } = default!;
}
