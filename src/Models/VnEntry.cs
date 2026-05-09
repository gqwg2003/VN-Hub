namespace VnHub.Models;

public enum VnStatus
{
    Reading = 0,
    Completed = 1,
    OnHold = 2,
    Dropped = 3,
    PlanToRead = 4
}

public class VnEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? CoverPath { get; set; }
    public string? ExePath { get; set; }
    public string DateAdded { get; set; } = DateTime.UtcNow.ToString("o");
    public VnStatus Status { get; set; } = VnStatus.PlanToRead;
    public bool IsFavorite { get; set; }
    public bool IsPinned { get; set; }
    public string? Notes { get; set; }
    public string Tags { get; set; } = "[]";
    public long PlayTimeSeconds { get; set; }
    public string? LastLaunchedAt { get; set; }
    public string? GroupId { get; set; }
    public string? VndbId { get; set; }
    public string? Description { get; set; }
    public double? Rating { get; set; }
    public double? UserRating { get; set; }
    public string? CompletedAt { get; set; }
    public double? StoryRating { get; set; }
    public double? ArtRating { get; set; }
    public double? MusicRating { get; set; }
    public double? CharacterRating { get; set; }
    public string Links { get; set; } = "[]";
    public int ReadingProgress { get; set; }
    public bool SkipVndb { get; set; }
}
