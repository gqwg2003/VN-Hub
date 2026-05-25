namespace VnHub.Services;

public class MetadataResult
{
    public string ExternalId { get; set; } = "";
    public string Title { get; set; } = "";
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public double? Rating { get; set; }
    public int? LengthMinutes { get; set; }
}
