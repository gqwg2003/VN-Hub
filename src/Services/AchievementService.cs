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
        public Dictionary<string, int> TagCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }

    public class UnlockedAchievement
    {
        public string Key { get; set; } = "";
        public string? UnlockedAt { get; set; }
        public bool Unlocked { get; set; }
    }

    private static int TagCount(StatsContext ctx, params string[] aliases)
        => aliases.Max(a => ctx.TagCounts.TryGetValue(a, out var c) ? c : 0);

    private static readonly List<Definition> Defs = new()
    {
        // ── Library size ──────────────────────────────────────────────────
        new() { Key = "achFirstVn",       Check = c => c.TotalVn >= 1 },
        new() { Key = "ach10Vn",          Check = c => c.TotalVn >= 10 },
        new() { Key = "ach50Vn",          Check = c => c.TotalVn >= 50 },
        new() { Key = "ach100Vn",         Check = c => c.TotalVn >= 100 },
        // ── Completions ───────────────────────────────────────────────────
        new() { Key = "achFirstComplete", Check = c => c.Completed >= 1 },
        new() { Key = "ach10Complete",    Check = c => c.Completed >= 10 },
        new() { Key = "ach25Complete",    Check = c => c.Completed >= 25 },
        // ── Play time ─────────────────────────────────────────────────────
        new() { Key = "ach10Hours",       Check = c => c.TotalHours >= 10 },
        new() { Key = "ach100Hours",      Check = c => c.TotalHours >= 100 },
        new() { Key = "ach500Hours",      Check = c => c.TotalHours >= 500 },
        // ── Engagement ────────────────────────────────────────────────────
        new() { Key = "achFirstFav",      Check = c => c.FavCount >= 1 },
        new() { Key = "achFirstRating",   Check = c => c.HasRating },
        // ── Tag: milf ─────────────────────────────────────────────────────
        new() { Key = "tagMilf5",         Check = c => TagCount(c, "milf", "mature woman") >= 5 },
        new() { Key = "tagMilf10",        Check = c => TagCount(c, "milf", "mature woman") >= 10 },
        new() { Key = "tagMilf25",        Check = c => TagCount(c, "milf", "mature woman") >= 25 },
        // ── Tag: harem ────────────────────────────────────────────────────
        new() { Key = "tagHarem5",        Check = c => TagCount(c, "harem") >= 5 },
        new() { Key = "tagHarem10",       Check = c => TagCount(c, "harem") >= 10 },
        new() { Key = "tagHarem25",       Check = c => TagCount(c, "harem") >= 25 },
        // ── Tag: yuri ─────────────────────────────────────────────────────
        new() { Key = "tagYuri5",         Check = c => TagCount(c, "yuri", "girl's love", "girls love") >= 5 },
        new() { Key = "tagYuri25",        Check = c => TagCount(c, "yuri", "girl's love", "girls love") >= 25 },
        // ── Tag: school ───────────────────────────────────────────────────
        new() { Key = "tagSchool5",       Check = c => TagCount(c, "school", "school life", "school setting") >= 5 },
        new() { Key = "tagSchool25",      Check = c => TagCount(c, "school", "school life", "school setting") >= 25 },
        // ── Tag: fantasy ──────────────────────────────────────────────────
        new() { Key = "tagFantasy5",      Check = c => TagCount(c, "fantasy") >= 5 },
        new() { Key = "tagFantasy25",     Check = c => TagCount(c, "fantasy") >= 25 },
        // ── Tag: tsundere ─────────────────────────────────────────────────
        new() { Key = "tagTsundere5",     Check = c => TagCount(c, "tsundere") >= 5 },
        new() { Key = "tagTsundere10",    Check = c => TagCount(c, "tsundere") >= 10 },
        // ── Tag: moege ────────────────────────────────────────────────────
        new() { Key = "tagMoege5",        Check = c => TagCount(c, "moege", "moe") >= 5 },
        new() { Key = "tagMoege25",       Check = c => TagCount(c, "moege", "moe") >= 25 },
        // ── Tag: romance ──────────────────────────────────────────────────
        new() { Key = "tagRomance5",      Check = c => TagCount(c, "romance") >= 5 },
        new() { Key = "tagRomance25",     Check = c => TagCount(c, "romance") >= 25 },
        // ── Tag: incest ───────────────────────────────────────────────────
        new() { Key = "tagIncest5",       Check = c => TagCount(c, "incest") >= 5 },
        new() { Key = "tagIncest10",      Check = c => TagCount(c, "incest") >= 10 },
        new() { Key = "tagIncest25",      Check = c => TagCount(c, "incest") >= 25 },
        // ── Tag: ahegao ───────────────────────────────────────────────────
        new() { Key = "tagAhegao5",       Check = c => TagCount(c, "ahegao") >= 5 },
        new() { Key = "tagAhegao25",      Check = c => TagCount(c, "ahegao") >= 25 },
        // ── Tag: exhibitionism ────────────────────────────────────────────
        new() { Key = "tagExhibit5",      Check = c => TagCount(c, "exhibitionism", "exhibitionist") >= 5 },
        new() { Key = "tagExhibit25",     Check = c => TagCount(c, "exhibitionism", "exhibitionist") >= 25 },
        // ── Tag: creampie ─────────────────────────────────────────────────
        new() { Key = "tagCreampie5",     Check = c => TagCount(c, "creampie") >= 5 },
        new() { Key = "tagCreampie25",    Check = c => TagCount(c, "creampie") >= 25 },
        // ── Tag: pregnancy ────────────────────────────────────────────────
        new() { Key = "tagPregnancy5",    Check = c => TagCount(c, "pregnancy", "pregnant") >= 5 },
        new() { Key = "tagPregnancy25",   Check = c => TagCount(c, "pregnancy", "pregnant") >= 25 },
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
