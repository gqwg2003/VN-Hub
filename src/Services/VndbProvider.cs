namespace VnHub.Services;

public class VndbProvider : IMetadataProvider
{
    public string Id => "vndb";
    public string DisplayName => "VNDB";

    public void ConfigureProxy(string? proxyAddress)
        => VndbService.ConfigureProxy(proxyAddress);

    public Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default)
        => VndbService.SearchAsync(title, ct);
}
