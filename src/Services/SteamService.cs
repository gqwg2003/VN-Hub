using System.Text.Json;

namespace VnHub.Services;

public class SteamService : MetadataServiceBase
{
    public override string Id => "steam";
    public override string DisplayName => "Steam";

    protected override string UserAgent => "VNHub/1.0";

    public override Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default)
        => ExecuteSearchAsync(title, SearchCoreAsync, ct);

    private async Task<MetadataResult?> SearchCoreAsync(string title, CancellationToken ct)
    {
        using var cts = LinkedTimeout(ct, TimeSpan.FromSeconds(20));

        var searchUrl = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(title)}&cc=us&l=en";
        var searchResponse = await Http.GetAsync(searchUrl, cts.Token);
        if (!searchResponse.IsSuccessStatusCode) return null;

        var searchJson = await searchResponse.Content.ReadAsStringAsync(cts.Token);
        using var searchDoc = JsonDocument.Parse(searchJson);

        if (!searchDoc.RootElement.TryGetProperty("items", out var items) ||
            items.ValueKind != JsonValueKind.Array ||
            items.GetArrayLength() == 0)
            return null;

        var appId = items[0].GetProperty("id").GetRawText();

        var detailsUrl = $"https://store.steampowered.com/api/appdetails?appids={appId}&cc=us&l=en";
        var detailsResponse = await Http.GetAsync(detailsUrl, cts.Token);
        if (!detailsResponse.IsSuccessStatusCode) return null;

        var detailsJson = await detailsResponse.Content.ReadAsStringAsync(cts.Token);
        using var detailsDoc = JsonDocument.Parse(detailsJson);

        if (!detailsDoc.RootElement.TryGetProperty(appId, out var appEntry) ||
            !appEntry.TryGetProperty("success", out var successEl) ||
            !successEl.GetBoolean() ||
            !appEntry.TryGetProperty("data", out var appData))
            return null;

        var appTitle = appData.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? title : title;
        var description = appData.StringOrNull("short_description");
        var imageUrl = appData.StringOrNull("header_image");

        double? rating = null;
        if (appData.TryGetProperty("metacritic", out var metacritic) &&
            metacritic.TryGetProperty("score", out var scoreEl))
            rating = scoreEl.GetDouble();

        return new MetadataResult
        {
            ExternalId = appId,
            Title = appTitle,
            ImageUrl = imageUrl,
            Description = description,
            Tags = appData.NamedArray("genres", "description"),
            Rating = rating
        };
    }
}
