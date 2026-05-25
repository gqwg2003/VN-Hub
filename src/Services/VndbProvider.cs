namespace VnHub.Services;

public class VndbProvider : IMetadataProvider
{
    public static readonly VndbProvider Instance = new();

    public string Id => "vndb";
    public string DisplayName => "VNDB";

    private VndbProvider() { }

    public Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default)
        => VndbService.SearchAsync(title, ct);
}
