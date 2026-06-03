using System.Text.Json;
using System.Text.RegularExpressions;
using VnHub.Models;

namespace VnHub.Common;

public static class Validation
{
    private static readonly Regex VndbIdRegex = new(@"^v\d{1,9}$", RegexOptions.Compiled);
    private static readonly Regex ProxyRegex = new(@"^https?://[^/\s]+:\d{1,5}/?$", RegexOptions.Compiled);
    private const int MaxTitleLength = 500;
    private const int MaxNotesLength = 50_000;
    private const int MaxIdLength = 200;
    private const int MaxTagLength = 100;
    private const int MaxTagsPerEntry = 100;
    private const int MaxLinkLabelLength = 200;
    private const int MaxLinkUrlLength = 2048;
    private const int MaxLinksPerEntry = 50;

    public static bool IsValidVnStatus(int status)
        => Enum.IsDefined(typeof(VnStatus), status);

    public static bool IsValidVndbId(string? id)
        => !string.IsNullOrEmpty(id) && VndbIdRegex.IsMatch(id);

    public static bool IsValidVnId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        if (id.Length > MaxIdLength) return false;
        if (Guid.TryParse(id, out _)) return true;
        return IsValidVndbId(id);
    }

    public static string SanitizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return "Untitled";
        return StringHelpers.Truncate(title.Trim(), MaxTitleLength);
    }

    public static string? SanitizeNotes(string? notes)
    {
        if (notes == null) return null;
        return StringHelpers.Truncate(notes, MaxNotesLength);
    }

    public static double? ClampRating(double? value, double min = 0, double max = 10)
    {
        if (!value.HasValue) return null;
        if (double.IsNaN(value.Value) || double.IsInfinity(value.Value)) return null;
        if (value.Value < min) return min;
        if (value.Value > max) return max;
        return value;
    }

    public static int ClampProgress(int value)
    {
        if (value < 0) return 0;
        if (value > 100) return 100;
        return value;
    }

    public static void Normalize(VnEntry entry)
    {
        entry.Title = SanitizeTitle(entry.Title);
        entry.Notes = SanitizeNotes(entry.Notes);
        entry.Rating = ClampRating(entry.Rating);
        entry.UserRating = ClampRating(entry.UserRating);
        entry.StoryRating = ClampRating(entry.StoryRating);
        entry.ArtRating = ClampRating(entry.ArtRating);
        entry.MusicRating = ClampRating(entry.MusicRating);
        entry.CharacterRating = ClampRating(entry.CharacterRating);
        entry.ReadingProgress = ClampProgress(entry.ReadingProgress);
        entry.Tags = NormalizeTags(entry.Tags);
        entry.Links = NormalizeLinks(entry.Links);
        if (!IsValidVnStatus((int)entry.Status))
            entry.Status = VnStatus.PlanToRead;
    }

    public static bool IsValidHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.Length > MaxLinkUrlLength) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }

    public static string SanitizeProxy(string? addr)
    {
        if (string.IsNullOrWhiteSpace(addr)) return "";
        var trimmed = addr.Trim();
        return ProxyRegex.IsMatch(trimmed) ? trimmed : "";
    }

    public static string NormalizeTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson)) return "[]";
        List<string>? tags;
        try { tags = JsonSerializer.Deserialize<List<string>>(tagsJson); }
        catch { return "[]"; }
        if (tags == null) return "[]";

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(Math.Min(tags.Count, MaxTagsPerEntry));
        foreach (var raw in tags)
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var t = StringHelpers.Truncate(raw.Trim(), MaxTagLength);
            if (seen.Add(t)) result.Add(t);
            if (result.Count >= MaxTagsPerEntry) break;
        }
        return JsonSerializer.Serialize(result);
    }

    public static string NormalizeLinks(string? linksJson)
    {
        if (string.IsNullOrWhiteSpace(linksJson)) return "[]";
        List<LinkDto>? links;
        try { links = JsonSerializer.Deserialize<List<LinkDto>>(linksJson, JsonHelpers.CommonOpts); }
        catch { return "[]"; }
        if (links == null) return "[]";

        var result = new List<LinkDto>(Math.Min(links.Count, MaxLinksPerEntry));
        foreach (var l in links)
        {
            if (l == null) continue;
            var url = l.Url?.Trim() ?? "";
            if (!IsValidHttpUrl(url)) continue;
            var label = string.IsNullOrWhiteSpace(l.Label) ? url : StringHelpers.Truncate(l.Label.Trim(), MaxLinkLabelLength);
            result.Add(new LinkDto { Label = label, Url = url });
            if (result.Count >= MaxLinksPerEntry) break;
        }
        return JsonSerializer.Serialize(result, JsonHelpers.CommonOpts);
    }

    private class LinkDto
    {
        public string Label { get; set; } = "";
        public string Url { get; set; } = "";
    }
}
