using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using VnHub.Common;

namespace VnHub.Services;

public static class VndbService
{
    private static HttpClient Http;
    private static string _currentProxy = "";

    static VndbService()
    {
        Http = CreateClient(null);
    }

    public static void ConfigureProxy(string? proxyAddress)
    {
        var addr = proxyAddress?.Trim() ?? "";
        if (addr == _currentProxy) return;
        _currentProxy = addr;
        Http = CreateClient(addr);
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
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("VNHub", "1.0"));
        client.DefaultRequestHeaders.UserAgent.Add(
            new ProductInfoHeaderValue("(visual-novel-library-manager)"));
        return client;
    }

    private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan DownloadTimeout = TimeSpan.FromSeconds(60);

    public static async Task<MetadataResult?> SearchAsync(string title, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        try
        {
            var body = new
            {
                filters = new object[] { "search", "=", title },
                fields = "title, image.url, description, tags.name, tags.rating, rating, length_minutes",
                sort = "searchrank",
                results = 5
            };

            var json = JsonSerializer.Serialize(body, Bridge.JsonOpts);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var apiCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            apiCts.CancelAfter(ApiTimeout);
            var response = await Http.PostAsync("https://api.vndb.org/kana/vn", content, apiCts.Token);
            if (!response.IsSuccessStatusCode) return null;

            var responseJson = await response.Content.ReadAsStringAsync(apiCts.Token);
            var result = JsonSerializer.Deserialize<VndbApiResponse>(responseJson, Bridge.JsonOpts);

            if (result?.Results == null || result.Results.Count == 0) return null;

            var best = result.Results[0];

            var tags = new List<string>();
            if (best.Tags != null)
            {
                foreach (var tag in best.Tags
                    .OrderByDescending(t => t.Rating)
                    .Take(10))
                {
                    if (!string.IsNullOrEmpty(tag.Name))
                        tags.Add(tag.Name);
                }
            }

            return new MetadataResult
            {
                ExternalId = best.Id ?? "",
                Title = best.Title ?? "",
                ImageUrl = best.Image?.Url,
                Description = CleanDescription(best.Description),
                Tags = tags,
                Rating = best.Rating,
                LengthMinutes = best.LengthMinutes
            };
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            LogService.Error("VNDB search failed", ex);
            return null;
        }
    }

    public static async Task<(string? FileName, string? Error)> DownloadCoverAsync(string imageUrl, string vnId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(imageUrl)) return (null, "No image URL");

        try
        {
            using var dlCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            dlCts.CancelAfter(DownloadTimeout);
            using var response = await Http.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, dlCts.Token);
            if (!response.IsSuccessStatusCode)
                return (null, $"HTTP {(int)response.StatusCode} from {imageUrl}");

            var bytes = await response.Content.ReadAsByteArrayAsync(dlCts.Token);
            if (bytes.Length < 100)
                return (null, $"Image too small ({bytes.Length} bytes)");

            var coversDir = VnHub.Common.AppPaths.EnsureCoversDir();

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            var ext = contentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };

            var fileName = $"{vnId}{ext}";
            var filePath = Path.Combine(coversDir, fileName);
            await File.WriteAllBytesAsync(filePath, bytes);

            return (fileName, null);
        }
        catch (OperationCanceledException)
        {
            return (null, "Download cancelled");
        }
        catch (Exception ex)
        {
            LogService.Error("Cover download failed", ex);
            return (null, ex.Message);
        }
    }

    private static string? CleanDescription(string? desc)
    {
        if (string.IsNullOrEmpty(desc)) return null;

        desc = System.Text.RegularExpressions.Regex.Replace(desc, @"\[url=[^\]]*\]([^\[]*)\[/url\]", "$1");
        desc = System.Text.RegularExpressions.Regex.Replace(desc, @"\[/?[a-zA-Z]+[^\]]*\]", "");

        return desc.Trim();
    }

    private class VndbApiResponse
    {
        public List<VndbVnItem> Results { get; set; } = new();
    }

    private class VndbVnItem
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public VndbImage? Image { get; set; }
        public string? Description { get; set; }
        public List<VndbTag>? Tags { get; set; }
        public double? Rating { get; set; }
        public int? LengthMinutes { get; set; }
    }

    private class VndbImage
    {
        public string? Url { get; set; }
    }

    private class VndbTag
    {
        public string? Name { get; set; }
        public double Rating { get; set; }
    }
}
