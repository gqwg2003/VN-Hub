using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VnHub.Services;

public class IgdbService : IMetadataProvider
{
    public static readonly IgdbService Instance = new();

    public string Id => "igdb";
    public string DisplayName => "IGDB";

    private HttpClient _http;
    private string _currentProxy = "";
    private string _cachedToken = "";
    private DateTime _tokenExpiry = DateTime.MinValue;

    private IgdbService()
    {
        _http = CreateClient(null);
    }

    public void ConfigureProxy(string? proxyAddress)
    {
        var addr = proxyAddress?.Trim() ?? "";
        if (addr == _currentProxy) return;
        _currentProxy = addr;
        _http = CreateClient(addr);
        _cachedToken = "";
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

    private async Task<string?> GetTokenAsync(string clientId, string clientSecret, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry)
            return _cachedToken;

        try
        {
            var url = $"https://id.twitch.tv/oauth2/token?client_id={Uri.EscapeDataString(clientId)}&client_secret={Uri.EscapeDataString(clientSecret)}&grant_type=client_credentials";
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            var resp = await _http.PostAsync(url, null, cts.Token);
            if (!resp.IsSuccessStatusCode) return null;

            var json = await resp.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var token = root.GetProperty("access_token").GetString() ?? "";
            var expiresIn = root.GetProperty("expires_in").GetInt32();
            _cachedToken = token;
            _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);
            return _cachedToken;
        }
        catch (Exception ex)
        {
            LogService.Error("IGDB token fetch failed", ex);
            return null;
        }
    }

    public async Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var settings = SettingsService.Load();
        var clientId = settings.IgdbClientId?.Trim() ?? "";
        var clientSecret = settings.IgdbClientSecret?.Trim() ?? "";
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            LogService.Info("IGDB credentials not configured.");
            return null;
        }

        try
        {
            var token = await GetTokenAsync(clientId, clientSecret, ct);
            if (string.IsNullOrEmpty(token)) return null;

            var escaped = title.Replace("\\", "\\\\").Replace("\"", "\\\"");
            var query = $"search \"{escaped}\"; fields name,summary,cover.url,genres.name,themes.name,rating; limit 1;";

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(15));

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.igdb.com/v4/games")
            {
                Content = new StringContent(query, Encoding.UTF8, "text/plain")
            };
            req.Headers.Add("Client-ID", clientId);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _http.SendAsync(req, cts.Token);
            if (!response.IsSuccessStatusCode) return null;

            var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(responseJson);
            var arr = doc.RootElement;
            if (arr.ValueKind != JsonValueKind.Array || arr.GetArrayLength() == 0) return null;

            var item = arr[0];
            var id = item.GetProperty("id").GetRawText();
            var itemTitle = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? title : title;
            var description = item.TryGetProperty("summary", out var sumEl) ? sumEl.GetString() : null;

            double? rating = null;
            if (item.TryGetProperty("rating", out var ratingEl))
                rating = ratingEl.GetDouble();

            string? imageUrl = null;
            if (item.TryGetProperty("cover", out var cover) && cover.TryGetProperty("url", out var urlEl))
            {
                var raw = urlEl.GetString() ?? "";
                imageUrl = (raw.StartsWith("//") ? "https:" : "") + raw.Replace("t_thumb", "t_cover_big");
            }

            var tags = new List<string>();
            if (item.TryGetProperty("genres", out var genres))
                foreach (var g in genres.EnumerateArray())
                    if (g.TryGetProperty("name", out var gn) && gn.GetString() is { } gnStr)
                        tags.Add(gnStr);
            if (item.TryGetProperty("themes", out var themes))
                foreach (var th in themes.EnumerateArray())
                    if (th.TryGetProperty("name", out var tn) && tn.GetString() is { } tnStr)
                        tags.Add(tnStr);

            return new MetadataResult
            {
                ExternalId = id,
                Title = itemTitle,
                ImageUrl = imageUrl,
                Description = description,
                Tags = tags,
                Rating = rating
            };
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex)
        {
            LogService.Error("IGDB search failed", ex);
            return null;
        }
    }
}
