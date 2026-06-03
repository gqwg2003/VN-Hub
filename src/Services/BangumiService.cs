using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace VnHub.Services;

public class BangumiService : MetadataServiceBase
{
    public override string Id => "bangumi";
    public override string DisplayName => "Bangumi";

    protected override string UserAgent => "VNHub/1.0";

    protected override void ConfigureClient(HttpClient client)
    {
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public override Task<MetadataResult?> SearchAsync(string title, CancellationToken ct = default)
        => ExecuteSearchAsync(title, SearchCoreAsync, ct);

    private async Task<MetadataResult?> SearchCoreAsync(string title, CancellationToken ct)
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

        string? itemTitle = item.StringOrNull("name_cn");
        if (string.IsNullOrEmpty(itemTitle)) itemTitle = item.StringOrNull("name");
        if (string.IsNullOrEmpty(itemTitle)) itemTitle = title;

        var summary = item.StringOrNull("summary");

        double? rating = null;
        if (item.TryGetProperty("rating", out var ratingObj) &&
            ratingObj.TryGetProperty("score", out var scoreEl))
            rating = scoreEl.GetDouble() * 10.0;

        string? imageUrl = null;
        if (item.TryGetProperty("images", out var images))
            imageUrl = images.StringOrNull("large");

        return new MetadataResult
        {
            ExternalId = id,
            Title = itemTitle,
            ImageUrl = imageUrl,
            Description = summary,
            Tags = item.NamedArray("tags", "name"),
            Rating = rating
        };
    }
}
