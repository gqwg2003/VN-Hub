using System.Text.RegularExpressions;
using VnHub.Models;

namespace VnHub.Common;

public static class Validation
{
    private static readonly Regex VndbIdRegex = new(@"^v\d{1,9}$", RegexOptions.Compiled);
    private const int MaxTitleLength = 500;
    private const int MaxNotesLength = 50_000;
    private const int MaxIdLength = 200;

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
        var trimmed = title.Trim();
        if (trimmed.Length > MaxTitleLength) trimmed = trimmed.Substring(0, MaxTitleLength);
        return trimmed;
    }

    public static string? SanitizeNotes(string? notes)
    {
        if (notes == null) return null;
        if (notes.Length > MaxNotesLength) return notes.Substring(0, MaxNotesLength);
        return notes;
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
        if (!IsValidVnStatus((int)entry.Status))
            entry.Status = VnStatus.PlanToRead;
    }
}
