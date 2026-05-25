namespace VnHub.Services;

public interface IMetadataProvider
{
    string Id { get; }
    string DisplayName { get; }
    Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default);
}
