using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace VnHub.Services;

public class AniListService : MetadataServiceBase
{
    public static readonly AniListService Instance = new();

    public override string Id => "anilist";
    public override string DisplayName => "AniList";

    protected override string UserAgent => "VNHub/1.0";

    private AniListService() { }

    public override async Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

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

        try
        {
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

            var description = media.TryGetProperty("description", out var descEl) && descEl.ValueKind != JsonValueKind.Null
                ? descEl.GetString() : null;

            double? rating = null;
            if (media.TryGetProperty("averageScore", out var scoreEl) && scoreEl.ValueKind != JsonValueKind.Null)
                rating = scoreEl.GetDouble();

            string? imageUrl = null;
            if (media.TryGetProperty("coverImage", out var cover) &&
                cover.TryGetProperty("extraLarge", out var imgEl) &&
                imgEl.ValueKind != JsonValueKind.Null)
                imageUrl = imgEl.GetString();

            var tags = new List<string>();
            if (media.TryGetProperty("genres", out var genres))
                foreach (var g in genres.EnumerateArray())
                    if (g.GetString() is { } gs)
                        tags.Add(gs);

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
            LogService.Error("AniList search failed", ex);
            return null;
        }
    }
}
