using System.Globalization;
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

        foreach (var e in entries)
        {
            byStatus[(int)e.Status]++;
            totalPlayTime += e.PlayTimeSeconds;
            if (e.IsFavorite) favCount++;
            if (e.UserRating.HasValue) { ratingSum += e.UserRating.Value; ratingCount++; }
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

        var ctx = new AchievementService.StatsContext
        {
            TotalVn = totalVn,
            Completed = byStatus[(int)VnStatus.Completed],
            FavCount = favCount,
            HasRating = topRated.Count > 0,
            TotalHours = totalPlayTime / 3600.0
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
            achievements,
            newlyUnlocked
        };
    }
}
