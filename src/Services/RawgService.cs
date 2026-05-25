using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace VnHub.Services;

public class RawgService : IMetadataProvider
{
    public static readonly RawgService Instance = new();

    public string Id => "rawg";
    public string DisplayName => "RAWG";

    private HttpClient _http;
    private string _currentProxy = "";

    private RawgService()
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

        var settings = SettingsService.Load();
        var apiKey = settings.RawgApiKey?.Trim() ?? "";
        if (string.IsNullOrEmpty(apiKey))
        {
            LogService.Info("RAWG API key not configured.");
            return null;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            var searchUrl = $"https://api.rawg.io/api/games?search={Uri.EscapeDataString(title)}&key={Uri.EscapeDataString(apiKey)}&page_size=1";
            var searchResponse = await _http.GetAsync(searchUrl, cts.Token);
            if (!searchResponse.IsSuccessStatusCode) return null;

            var searchJson = await searchResponse.Content.ReadAsStringAsync(cts.Token);
            using var searchDoc = JsonDocument.Parse(searchJson);

            if (!searchDoc.RootElement.TryGetProperty("results", out var results) ||
                results.ValueKind != JsonValueKind.Array ||
                results.GetArrayLength() == 0)
                return null;

            var first = results[0];
            var id = first.GetProperty("id").GetRawText();
            var gameTitle = first.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? title : title;
            var imageUrl = first.TryGetProperty("background_image", out var imgEl) && imgEl.ValueKind != JsonValueKind.Null
                ? imgEl.GetString() : null;

            var detailsUrl = $"https://api.rawg.io/api/games/{id}?key={Uri.EscapeDataString(apiKey)}";
            var detailsResponse = await _http.GetAsync(detailsUrl, cts.Token);

            string? description = null;
            var tags = new List<string>();
            double? rating = null;

            if (detailsResponse.IsSuccessStatusCode)
            {
                var detailsJson = await detailsResponse.Content.ReadAsStringAsync(cts.Token);
                using var detailsDoc = JsonDocument.Parse(detailsJson);
                var d = detailsDoc.RootElement;

                if (d.TryGetProperty("description_raw", out var descEl) && descEl.ValueKind != JsonValueKind.Null)
                    description = descEl.GetString();

                if (d.TryGetProperty("rating", out var ratingEl))
                    rating = ratingEl.GetDouble() * 20.0;

                if (d.TryGetProperty("genres", out var genres))
                    foreach (var g in genres.EnumerateArray())
                        if (g.TryGetProperty("name", out var gn) && gn.GetString() is { } gs)
                            tags.Add(gs);

                if (d.TryGetProperty("tags", out var rawgTags))
                    foreach (var tag in rawgTags.EnumerateArray().Take(10))
                        if (tag.TryGetProperty("name", out var tn) && tn.GetString() is { } ts)
                            tags.Add(ts);
            }
            else
            {
                if (first.TryGetProperty("rating", out var ratingEl))
                    rating = ratingEl.GetDouble() * 20.0;

                if (first.TryGetProperty("genres", out var genres))
                    foreach (var g in genres.EnumerateArray())
                        if (g.TryGetProperty("name", out var gn) && gn.GetString() is { } gs)
                            tags.Add(gs);
            }

            return new MetadataResult
            {
                ExternalId = id,
                Title = gameTitle,
                ImageUrl = imageUrl,
                Description = description,
                Tags = tags,
                Rating = rating
            };
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex)
        {
            LogService.Error("RAWG search failed", ex);
            return null;
        }
    }
}
