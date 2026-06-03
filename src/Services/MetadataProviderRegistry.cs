namespace VnHub.Services;

public interface IMetadataProviderRegistry
{
    IReadOnlyList<IMetadataProvider> All { get; }
    IMetadataProvider Resolve(string? id);
    void ConfigureProxies(string? proxyAddress);
}

public sealed class MetadataProviderRegistry : IMetadataProviderRegistry
{
    private const string DefaultId = "vndb";
    private readonly IReadOnlyList<IMetadataProvider> _providers;

    public MetadataProviderRegistry(IEnumerable<IMetadataProvider> providers)
    {
        _providers = providers.ToList();
    }

    public IReadOnlyList<IMetadataProvider> All => _providers;

    public IMetadataProvider Resolve(string? id)
        => _providers.FirstOrDefault(p => p.Id == id)
           ?? _providers.First(p => p.Id == DefaultId);

    public void ConfigureProxies(string? proxyAddress)
    {
        foreach (var provider in _providers)
            provider.ConfigureProxy(proxyAddress);
    }
}
