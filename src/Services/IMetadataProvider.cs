namespace VnHub.Services;

public interface IMetadataProvider
{
    string Id { get; }
    string DisplayName { get; }
    void ConfigureProxy(string? proxyAddress);
    Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default);
}
