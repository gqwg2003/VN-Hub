using VnHub.Database;

namespace VnHub.Services;

public static class AchievementService
{
    public class Definition
    {
        public string Key { get; init; } = "";
        public int Target { get; init; } = 1;
        public Func<StatsContext, int> Current { get; init; } = _ => 0;
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
        public int Progress { get; set; }
        public int Target { get; set; }
    }

    private static int TagCount(StatsContext ctx, params string[] aliases)
        => aliases.Max(a => ctx.TagCounts.TryGetValue(a, out var c) ? c : 0);

    private static Definition Def(string key, int target, Func<StatsContext, int> current)
        => new() { Key = key, Target = target, Current = current };

    private static Definition Tag(string key, int target, params string[] aliases)
        => new() { Key = key, Target = target, Current = c => TagCount(c, aliases) };

    private static readonly List<Definition> Defs = new()
    {
        // --Library size 
        Def("achFirstVn",       1,   c => c.TotalVn),
        Def("ach10Vn",          10,  c => c.TotalVn),
        Def("ach50Vn",          50,  c => c.TotalVn),
        Def("ach100Vn",         100, c => c.TotalVn),
        // -- Completions 
        Def("achFirstComplete", 1,   c => c.Completed),
        Def("ach10Complete",    10,  c => c.Completed),
        Def("ach25Complete",    25,  c => c.Completed),
        // -- Play time
        Def("ach10Hours",       10,  c => (int)c.TotalHours),
        Def("ach100Hours",      100, c => (int)c.TotalHours),
        Def("ach500Hours",      500, c => (int)c.TotalHours),
        // -- Engagement
        Def("achFirstFav",      1,   c => c.FavCount),
        Def("achFirstRating",   1,   c => c.HasRating ? 1 : 0),
        // -- Tag: milf
        Tag("tagMilf5",         5,   "milf", "mature woman"),
        Tag("tagMilf10",        10,  "milf", "mature woman"),
        Tag("tagMilf25",        25,  "milf", "mature woman"),
        // -- Tag: harem
        Tag("tagHarem5",        5,   "harem"),
        Tag("tagHarem10",       10,  "harem"),
        Tag("tagHarem25",       25,  "harem"),
        // -- Tag: yuri
        Tag("tagYuri5",         5,   "yuri", "girl's love", "girls love"),
        Tag("tagYuri25",        25,  "yuri", "girl's love", "girls love"),
        // -- Tag: school
        Tag("tagSchool5",       5,   "school", "school life", "school setting"),
        Tag("tagSchool25",      25,  "school", "school life", "school setting"),
        // -- Tag: fantasy
        Tag("tagFantasy5",      5,   "fantasy"),
        Tag("tagFantasy25",     25,  "fantasy"),
        // -- Tag: tsundere
        Tag("tagTsundere5",     5,   "tsundere"),
        Tag("tagTsundere10",    10,  "tsundere"),
        // -- Tag: moege
        Tag("tagMoege5",        5,   "moege", "moe"),
        Tag("tagMoege25",       25,  "moege", "moe"),
        // -- Tag: romance
        Tag("tagRomance5",      5,   "romance"),
        Tag("tagRomance25",     25,  "romance"),
        // -- Tag: incest
        Tag("tagIncest5",       5,   "incest"),
        Tag("tagIncest10",      10,  "incest"),
        Tag("tagIncest25",      25,  "incest"),
        // -- Tag: ahegao
        Tag("tagAhegao5",       5,   "ahegao"),
        Tag("tagAhegao25",      25,  "ahegao"),
        // -- Tag: exhibitionism
        Tag("tagExhibit5",      5,   "exhibitionism", "exhibitionist"),
        Tag("tagExhibit25",     25,  "exhibitionism", "exhibitionist"),
        // -- Tag: creampie
        Tag("tagCreampie5",     5,   "creampie"),
        Tag("tagCreampie25",    25,  "creampie"),
        // -- Tag: pregnancy
        Tag("tagPregnancy5",    5,   "pregnancy", "pregnant"),
        Tag("tagPregnancy25",   25,  "pregnancy", "pregnant"),
        // -- Tag: bdsm
        Tag("tagBdsm5",         5,   "bdsm", "bondage", "s&m"),
        Tag("tagBdsm10",        10,  "bdsm", "bondage", "s&m"),
        Tag("tagBdsm25",        25,  "bdsm", "bondage", "s&m"),
        // -- Tag: grandmother-grandson
        Tag("tagGrandma5",      5,   "grandmother-grandson", "grandmother/grandson", "grandparent-grandchild"),
        Tag("tagGrandma10",     10,  "grandmother-grandson", "grandmother/grandson", "grandparent-grandchild"),
        Tag("tagGrandma25",     25,  "grandmother-grandson", "grandmother/grandson", "grandparent-grandchild"),
        // -- Tag: mother-son
        Tag("tagMotherSon5",    5,   "mother-son", "mother/son"),
        Tag("tagMotherSon10",   10,  "mother-son", "mother/son"),
        Tag("tagMotherSon25",   25,  "mother-son", "mother/son"),
        // -- Tag: aunt-nephew
        Tag("tagAunt5",         5,   "aunt-nephew", "aunt/nephew"),
        Tag("tagAunt10",        10,  "aunt-nephew", "aunt/nephew"),
        Tag("tagAunt25",        25,  "aunt-nephew", "aunt/nephew"),
        // -- Tag: brother-sister
        Tag("tagSiblings5",     5,   "brother-sister", "siblings", "sister-brother"),
        Tag("tagSiblings10",    10,  "brother-sister", "siblings", "sister-brother"),
        Tag("tagSiblings25",    25,  "brother-sister", "siblings", "sister-brother"),
        // -- Tag: cousin
        Tag("tagCousin5",       5,   "cousin", "cousins", "female cousin-male cousin"),
        Tag("tagCousin10",      10,  "cousin", "cousins", "female cousin-male cousin"),
        Tag("tagCousin25",      25,  "cousin", "cousins", "female cousin-male cousin"),
    };

    public static (List<UnlockedAchievement> All, List<string> NewlyUnlocked) Evaluate(StatsContext ctx)
    {
        var stored = AchievementRepository.GetAll();
        var newlyUnlocked = new List<string>();
        var nowIso = DateTime.UtcNow.ToString("o");

        foreach (var def in Defs)
        {
            if (def.Current(ctx) >= def.Target && !stored.ContainsKey(def.Key))
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
            UnlockedAt = stored.TryGetValue(d.Key, out var ts) ? ts : null,
            Progress = Math.Min(d.Current(ctx), d.Target),
            Target = d.Target
        }).ToList();

        return (all, newlyUnlocked);
    }
}
