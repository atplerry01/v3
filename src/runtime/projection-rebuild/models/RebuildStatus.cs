namespace Whycespace.ProjectionRebuild.Models;

public sealed class RebuildStatus
{
    public bool Rebuilding { get; set; }
    public string? CurrentProjection { get; set; }
    public int ProcessedEvents { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
