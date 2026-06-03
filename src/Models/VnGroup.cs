namespace VnHub.Models;

public class VnGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
    public string? Filter { get; set; }
    public int SortOrder { get; set; }
}

public class GroupFilter
{
    public int? Status { get; set; }
    public string? Tag { get; set; }
    public bool? IsFavorite { get; set; }
    public double? MinRating { get; set; }
}
