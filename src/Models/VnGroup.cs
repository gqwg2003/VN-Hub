namespace VnHub.Models;

public class VnGroup
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
}
