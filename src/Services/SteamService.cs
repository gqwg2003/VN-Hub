using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace VnHub.Services;

public class SteamService : IMetadataProvider
{
    public static readonly SteamService Instance = new();

    public string Id => "steam";
    public string DisplayName => "Steam";

    private HttpClient _http;
    private string _currentProxy = "";

    private SteamService()
    {
        _http = CreateClient(null);
    }

    public void ConfigureProxy(string? proxyAddress)
    {
        var addr = proxyAddress?.Trim() ?? "";
        if (addr == _currentProxy) return;
        _currentProxy = addr;
        _http = CreateClient(addr);
    }

    private static HttpClient CreateClient(string? proxyAddress)
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
        var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("VNHub", "1.0"));
        return client;
    }

    public async Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(20));

            var searchUrl = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(title)}&cc=us&l=en";
            var searchResponse = await _http.GetAsync(searchUrl, cts.Token);
            if (!searchResponse.IsSuccessStatusCode) return null;

            var searchJson = await searchResponse.Content.ReadAsStringAsync(cts.Token);
            using var searchDoc = JsonDocument.Parse(searchJson);

            if (!searchDoc.RootElement.TryGetProperty("items", out var items) ||
                items.ValueKind != JsonValueKind.Array ||
                items.GetArrayLength() == 0)
                return null;

            var appId = items[0].GetProperty("id").GetRawText();

            var detailsUrl = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=us&l=en";
            var detailsResponse = await _http.GetAsync(detailsUrl, cts.Token);
            if (!detailsResponse.IsSuccessStatusCode) return null;

            var detailsJson = await detailsResponse.Content.ReadAsStringAsync(cts.Token);
            using var detailsDoc = JsonDocument.Parse(detailsJson);

            if (!detailsDoc.RootElement.TryGetProperty(appId, out var appEntry) ||
                !appEntry.TryGetProperty("success", out var successEl) ||
                !successEl.GetBoolean() ||
                !appEntry.TryGetProperty("data", out var appData))
                return null;

            var appTitle = appData.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? title : title;
            var description = appData.TryGetProperty("short_description", out var descEl) ? descEl.GetString() : null;
            var imageUrl = appData.TryGetProperty("header_image", out var imgEl) ? imgEl.GetString() : null;

            double? rating = null;
            if (appData.TryGetProperty("metacritic", out var metacritic) &&
                metacritic.TryGetProperty("score", out var scoreEl))
                rating = scoreEl.GetDouble();

            var tags = new List<string>();
            if (appData.TryGetProperty("genres", out var genres))
                foreach (var g in genres.EnumerateArray())
                    if (g.TryGetProperty("description", out var gDesc) && gDesc.GetString() is { } gs)
                        tags.Add(gs);

            return new MetadataResult
            {
                ExternalId = appId,
                Title = appTitle,
                ImageUrl = imageUrl,
                Description = description,
                Tags = tags,
                Rating = rating
            };
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex)
        {
            LogService.Error("Steam search failed", ex);
            return null;
        }
    }
}
