using VnHub.Database;

namespace VnHub.Services;

public static class AchievementService
{
    public class Definition
    {
        public string Key { get; init; } = "";
        public Func<StatsContext, bool> Check { get; init; } = _ => false;
    }

    public class StatsContext
    {
        public int TotalVn { get; init; }
        public int Completed { get; init; }
        public int FavCount { get; init; }
        public bool HasRating { get; init; }
        public double TotalHours { get; init; }
    }

    public class UnlockedAchievement
    {
        public string Key { get; set; } = "";
        public string? UnlockedAt { get; set; }
        public bool Unlocked { get; set; }
    }

    private static readonly List<Definition> Defs = new()
    {
        new() { Key = "achFirstVn",       Check = c => c.TotalVn >= 1 },
        new() { Key = "ach10Vn",          Check = c => c.TotalVn >= 10 },
        new() { Key = "ach50Vn",          Check = c => c.TotalVn >= 50 },
        new() { Key = "ach100Vn",         Check = c => c.TotalVn >= 100 },
        new() { Key = "achFirstComplete", Check = c => c.Completed >= 1 },
        new() { Key = "ach10Complete",    Check = c => c.Completed >= 10 },
        new() { Key = "ach25Complete",    Check = c => c.Completed >= 25 },
        new() { Key = "ach10Hours",       Check = c => c.TotalHours >= 10 },
        new() { Key = "ach100Hours",      Check = c => c.TotalHours >= 100 },
        new() { Key = "ach500Hours",      Check = c => c.TotalHours >= 500 },
        new() { Key = "achFirstFav",      Check = c => c.FavCount >= 1 },
        new() { Key = "achFirstRating",   Check = c => c.HasRating },
    };

    public static (List<UnlockedAchievement> All, List<string> NewlyUnlocked) Evaluate(StatsContext ctx)
    {
        var stored = AchievementRepository.GetAll();
        var newlyUnlocked = new List<string>();
        var nowIso = DateTime.UtcNow.ToString("o");

        foreach (var def in Defs)
        {
            if (def.Check(ctx) && !stored.ContainsKey(def.Key))
            {
                AchievementRepository.Unlock(def.Key, nowIso);
                stored[def.Key] = nowIso;
                newlyUnlocked.Add(def.Key);
            }
        }

        var all = Defs.Select(d => new UnlockedAchievement
        {
            Key = d.Key,
            Unlocked = stored.ContainsKey(d.Key),
            UnlockedAt = stored.TryGetValue(d.Key, out var ts) ? ts : null
        }).ToList();

        return (all, newlyUnlocked);
    }
}
