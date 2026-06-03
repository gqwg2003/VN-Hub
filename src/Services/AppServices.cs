using Microsoft.Extensions.DependencyInjection;

namespace VnHub.Services;

public static class AppServices
{
    private static IServiceProvider? _provider;

    public static IServiceProvider Provider =>
        _provider ?? throw new InvalidOperationException("AppServices not initialized. Call Initialize() first.");

    public static IMetadataProviderRegistry Metadata =>
        Provider.GetRequiredService<IMetadataProviderRegistry>();

    public static void Initialize()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IMetadataProvider, VndbProvider>();
        services.AddSingleton<IMetadataProvider, IgdbService>();
        services.AddSingleton<IMetadataProvider, AniListService>();
        services.AddSingleton<IMetadataProvider, BangumiService>();
        services.AddSingleton<IMetadataProvider, SteamService>();
        services.AddSingleton<IMetadataProvider, RawgService>();

        services.AddSingleton<IMetadataProviderRegistry, MetadataProviderRegistry>();

        _provider = services.BuildServiceProvider();
    }
}
