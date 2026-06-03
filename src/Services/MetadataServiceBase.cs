using System.Net.Http;

namespace VnHub.Services;

public abstract class MetadataServiceBase : IMetadataProvider
{
    protected HttpClient Http;
    protected string CurrentProxy = "";

    protected abstract string UserAgent { get; }

    public abstract string Id { get; }
    public abstract string DisplayName { get; }

    protected MetadataServiceBase()
    {
        Http = BuildClient(null);
    }

    public void ConfigureProxy(string? proxyAddress)
    {
        var addr = proxyAddress?.Trim() ?? "";
        if (addr == CurrentProxy) return;
        CurrentProxy = addr;
        Http = BuildClient(addr);
        OnProxyChanged();
    }

    private HttpClient BuildClient(string? proxyAddress)
    {
        var client = CreateClient(proxyAddress, UserAgent);
        ConfigureClient(client);
        return client;
    }

    protected virtual void ConfigureClient(HttpClient client) { }

    protected virtual void OnProxyChanged() { }

    protected static HttpClient CreateClient(string? proxyAddress, string userAgent)
    {
        HttpClientHandler handler;
        if (!string.IsNullOrWhiteSpace(proxyAddress))
        {
            handler = new HttpClientHandler
            {
                Proxy = new System.Net.WebProxy(proxyAddress),
                UseProxy = true
            };
        }
        else
        {
            handler = new HttpClientHandler();
        }
        var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        return client;
    }

    protected static CancellationTokenSource LinkedTimeout(CancellationToken ct, TimeSpan timeout)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        return cts;
    }

    protected async Task<MetadataResult?> ExecuteSearchAsync(
        string title,
        Func<string, CancellationToken, Task<MetadataResult?>> core,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;
        try { return await core(title, ct); }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex)
        {
            LogService.Error($"{DisplayName} search failed", ex);
            return null;
        }
    }

    public abstract Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default);
}
