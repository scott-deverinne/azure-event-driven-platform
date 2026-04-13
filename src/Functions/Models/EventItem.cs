namespace Functions.Models;

public class EventItem
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}