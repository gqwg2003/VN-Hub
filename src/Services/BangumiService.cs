using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VnHub.Services;

public class BangumiService : MetadataServiceBase
{
    public static readonly BangumiService Instance = new();

    public override string Id => "bangumi";
    public override string DisplayName => "Bangumi";

    protected override string UserAgent => "VNHub/1.0";

    private BangumiService() { }

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public override async Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        try
        {
            var body = JsonSerializer.Serialize(new
            {
                keyword = title,
                filter = new { type = new[] { 4 } },
                limit = 1
            });

            using var cts = LinkedTimeout(ct, TimeSpan.FromSeconds(15));

            var response = await Http.PostAsync(
                "https://api.bgm.tv/v0/search/subjects",
                new StringContent(body, Encoding.UTF8, "application/json"),
                cts.Token);
            if (!response.IsSuccessStatusCode) return null;

            var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
            using var doc = JsonDocument.Parse(responseJson);

            if (!doc.RootElement.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array ||
                data.GetArrayLength() == 0)
                return null;

            var item = data[0];
            var id = item.GetProperty("id").GetRawText();

            string? itemTitle = null;
            if (item.TryGetProperty("name_cn", out var nameCn) && nameCn.ValueKind != JsonValueKind.Null)
                itemTitle = nameCn.GetString();
            if (string.IsNullOrEmpty(itemTitle) && item.TryGetProperty("name", out var name))
                itemTitle = name.GetString();
            itemTitle ??= title;

            var summary = item.TryGetProperty("summary", out var sumEl) && sumEl.ValueKind != JsonValueKind.Null
                ? sumEl.GetString() : null;

            double? rating = null;
            if (item.TryGetProperty("rating", out var ratingObj) &&
                ratingObj.TryGetProperty("score", out var scoreEl))
                rating = scoreEl.GetDouble() * 10.0;

            string? imageUrl = null;
            if (item.TryGetProperty("images", out var images) &&
                images.TryGetProperty("large", out var imgEl) &&
                imgEl.ValueKind != JsonValueKind.Null)
                imageUrl = imgEl.GetString();

            var tags = new List<string>();
            if (item.TryGetProperty("tags", out var tagsEl))
                foreach (var tag in tagsEl.EnumerateArray())
                    if (tag.TryGetProperty("name", out var tn) && tn.GetString() is { } ts)
                        tags.Add(ts);

            return new MetadataResult
            {
                ExternalId = id,
                Title = itemTitle,
                ImageUrl = imageUrl,
                Description = summary,
                Tags = tags,
                Rating = rating
            };
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception ex)
        {
            LogService.Error("Bangumi search failed", ex);
            return null;
        }
    }
}
