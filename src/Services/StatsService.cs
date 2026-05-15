using System.Globalization;
using System.Text.Json;
using VnHub.Models;

namespace VnHub.Services;

public static class StatsService
{
    public static object Compute(List<VnEntry> entries)
    {
        var totalVn = entries.Count;
        var byStatus = new Dictionary<int, int>();
        for (int i = 0; i <= 4; i++) byStatus[i] = 0;
        long totalPlayTime = 0;
        int favCount = 0;
        double ratingSum = 0;
        int ratingCount = 0;
        var monthlyAdds = new Dictionary<string, int>();
        var byRating = new Dictionary<int, int>();
        var tagFreq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        long completionTimeSum = 0;
        int completionCount = 0;
        double storySum = 0; int storyCount = 0;
        double artSum = 0;   int artCount = 0;
        double musicSum = 0; int musicCount = 0;
        double charSum = 0;  int charCount = 0;

        foreach (var e in entries)
        {
            byStatus[(int)e.Status]++;
            totalPlayTime += e.PlayTimeSeconds;
            if (e.IsFavorite) favCount++;
            if (e.UserRating.HasValue)
            {
                ratingSum += e.UserRating.Value;
                ratingCount++;
                int r = Math.Clamp((int)Math.Round(e.UserRating.Value), 1, 10);
                byRating.TryGetValue(r, out int rc);
                byRating[r] = rc + 1;
            }
            if (!string.IsNullOrEmpty(e.DateAdded))
            {
                try
                {
                    var d = DateTime.Parse(e.DateAdded, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
                    var key = d.ToString("yyyy-MM");
                    monthlyAdds.TryGetValue(key, out int c);
                    monthlyAdds[key] = c + 1;
                }
                catch { }
            }
            if (!string.IsNullOrEmpty(e.Tags) && e.Tags != "[]")
            {
                try
                {
                    var tags = JsonSerializer.Deserialize<List<string>>(e.Tags);
                    if (tags != null)
                        foreach (var tag in tags)
                        {
                            if (string.IsNullOrWhiteSpace(tag)) continue;
                            tagFreq.TryGetValue(tag, out int tc);
                            tagFreq[tag] = tc + 1;
                        }
                }
                catch { }
            }
            if (e.Status == VnStatus.Completed && e.PlayTimeSeconds > 0)
            {
                completionTimeSum += e.PlayTimeSeconds;
                completionCount++;
            }
            if (e.StoryRating.HasValue)     { storySum += e.StoryRating.Value;     storyCount++; }
            if (e.ArtRating.HasValue)       { artSum   += e.ArtRating.Value;       artCount++;   }
            if (e.MusicRating.HasValue)     { musicSum += e.MusicRating.Value;     musicCount++; }
            if (e.CharacterRating.HasValue) { charSum  += e.CharacterRating.Value; charCount++;  }
        }

        var topPlayed = entries
            .Where(e => e.PlayTimeSeconds > 0)
            .OrderByDescending(e => e.PlayTimeSeconds)
            .Take(10)
            .Select(e => new { id = e.Id, title = e.Title, cover = e.CoverPath, playTime = e.PlayTimeSeconds, userRating = e.UserRating })
            .ToList();
        var topRated = entries
            .Where(e => e.UserRating.HasValue)
            .OrderByDescending(e => e.UserRating!.Value)
            .Take(10)
            .Select(e => new { id = e.Id, title = e.Title, cover = e.CoverPath, playTime = e.PlayTimeSeconds, userRating = e.UserRating })
            .ToList();
        var avgRating = ratingCount > 0 ? Math.Round(ratingSum / ratingCount, 1) : 0.0;

        var topTags = tagFreq
            .OrderByDescending(kv => kv.Value)
            .Take(12)
            .Select(kv => new { tag = kv.Key, count = kv.Value })
            .ToList();
        var avgCompletionTime = completionCount > 0 ? completionTimeSum / completionCount : 0L;
        var categoryAvgs = new
        {
            story     = storyCount > 0 ? (double?)Math.Round(storySum / storyCount, 1) : null,
            art       = artCount   > 0 ? (double?)Math.Round(artSum   / artCount,   1) : null,
            music     = musicCount > 0 ? (double?)Math.Round(musicSum / musicCount, 1) : null,
            character = charCount  > 0 ? (double?)Math.Round(charSum  / charCount,  1) : null,
        };

        var ctx = new AchievementService.StatsContext
        {
            TotalVn = totalVn,
            Completed = byStatus[(int)VnStatus.Completed],
            FavCount = favCount,
            HasRating = topRated.Count > 0,
            TotalHours = totalPlayTime / 3600.0,
            TagCounts = tagFreq
        };
        var (achievements, newlyUnlocked) = AchievementService.Evaluate(ctx);

        return new
        {
            totalVn,
            byStatus,
            totalPlayTime,
            monthlyAdds,
            topPlayed,
            topRated,
            favCount,
            avgRating,
            byRating,
            topTags,
            avgCompletionTime,
            categoryAvgs,
            achievements,
            newlyUnlocked
        };
    }
}
