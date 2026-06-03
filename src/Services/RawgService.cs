using System.Text.Json;

namespace VnHub.Services;

public class RawgService : MetadataServiceBase
{
    public override string Id => "rawg";
    public override string DisplayName => "RAWG";

    protected override string UserAgent => "VNHub/1.0";

    public override Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default)
        => ExecuteSearchAsync(title, SearchCoreAsync, ct);

    private async Task<MetadataResult?> SearchCoreAsync(string title, CancellationToken ct)
    {
        var settings = SettingsService.Load();
        var apiKey = settings.RawgApiKey?.Trim() ?? "";
        if (string.IsNullOrEmpty(apiKey))
        {
            LogService.Info("RAWG API key not configured.");
            return null;
        }

        using var cts = LinkedTimeout(ct, TimeSpan.FromSeconds(15));

        var searchUrl = $"https://api.rawg.io/api/games?search={Uri.EscapeDataString(title)}&key={Uri.EscapeDataString(apiKey)}&page_size=1";
        var searchResponse = await Http.GetAsync(searchUrl, cts.Token);
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
        var imageUrl = first.StringOrNull("background_image");

        var detailsUrl = $"https://api.rawg.io/api/games/{id}?key={Uri.EscapeDataString(apiKey)}";
        var detailsResponse = await Http.GetAsync(detailsUrl, cts.Token);

        string? description = null;
        var tags = new List<string>();
        double? rating = null;

        if (detailsResponse.IsSuccessStatusCode)
        {
            var detailsJson = await detailsResponse.Content.ReadAsStringAsync(cts.Token);
            using var detailsDoc = JsonDocument.Parse(detailsJson);
            var d = detailsDoc.RootElement;

            description = d.StringOrNull("description_raw");

            if (d.TryGetProperty("rating", out var ratingEl))
                rating = ratingEl.GetDouble() * 20.0;

            tags.AddRange(d.NamedArray("genres", "name"));
            tags.AddRange(d.NamedArray("tags", "name").Take(10));
        }
        else
        {
            if (first.TryGetProperty("rating", out var ratingEl))
                rating = ratingEl.GetDouble() * 20.0;

            tags.AddRange(first.NamedArray("genres", "name"));
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
}
