using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace VnHub.Services;

public class AniListService : MetadataServiceBase
{
    public override string Id => "anilist";
    public override string DisplayName => "AniList";

    protected override string UserAgent => "VNHub/1.0";

    public override Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default)
        => ExecuteSearchAsync(title, SearchCoreAsync, ct);

    private async Task<MetadataResult?> SearchCoreAsync(string title, CancellationToken ct)
    {
        const string gqlQuery = @"query ($search: String) {
  Media(search: $search, type: MANGA, sort: SEARCH_MATCH) {
    id
    title { romaji english }
    description(asHtml: false)
    coverImage { extraLarge }
    genres
    averageScore
  }
}";

        var body = JsonSerializer.Serialize(new { query = gqlQuery, variables = new { search = title } });
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var cts = LinkedTimeout(ct, TimeSpan.FromSeconds(15));

        var response = await Http.PostAsync("https://graphql.anilist.co", content, cts.Token);
        if (!response.IsSuccessStatusCode) return null;

        var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
        using var doc = JsonDocument.Parse(responseJson);

        if (!doc.RootElement.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("Media", out var media) ||
            media.ValueKind == JsonValueKind.Null)
            return null;

        var id = media.GetProperty("id").GetRawText();

        string? itemTitle = null;
        if (media.TryGetProperty("title", out var titleEl))
        {
            if (titleEl.TryGetProperty("english", out var eng) && eng.ValueKind != JsonValueKind.Null)
                itemTitle = eng.GetString();
            if (string.IsNullOrEmpty(itemTitle) && titleEl.TryGetProperty("romaji", out var rom))
                itemTitle = rom.GetString();
        }
        itemTitle ??= title;

        var description = media.StringOrNull("description");

        double? rating = null;
        if (media.TryGetProperty("averageScore", out var scoreEl) && scoreEl.ValueKind != JsonValueKind.Null)
            rating = scoreEl.GetDouble();

        string? imageUrl = null;
        if (media.TryGetProperty("coverImage", out var cover))
            imageUrl = cover.StringOrNull("extraLarge");

        return new MetadataResult
        {
            ExternalId = id,
            Title = itemTitle,
            ImageUrl = imageUrl,
            Description = description,
            Tags = media.StringArray("genres"),
            Rating = rating
        };
    }
}
