namespace Whycespace.EventReplay.Models;

public sealed class ReplayStatus
{
    public bool Replaying { get; set; }
    public int ProcessedEvents { get; set; }
    public string? CurrentTopic { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
