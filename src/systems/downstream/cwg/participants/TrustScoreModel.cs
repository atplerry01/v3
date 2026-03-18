namespace Whycespace.Systems.Downstream.Cwg.Participants;

public sealed record TrustScoreModel(
    Guid ParticipantId,
    decimal Score,
    int CompletedTasks,
    int FailedTasks,
    DateTimeOffset LastUpdated
)
{
    public static TrustScoreModel Initial(Guid participantId) => new(
        participantId,
        Score: 0m,
        CompletedTasks: 0,
        FailedTasks: 0,
        LastUpdated: DateTimeOffset.UtcNow
    );

    public TrustScoreModel WithTaskCompleted() => this with
    {
        CompletedTasks = CompletedTasks + 1,
        Score = Math.Min(100m, Score + 1m),
        LastUpdated = DateTimeOffset.UtcNow
    };

    public TrustScoreModel WithTaskFailed() => this with
    {
        FailedTasks = FailedTasks + 1,
        Score = Math.Max(0m, Score - 5m),
        LastUpdated = DateTimeOffset.UtcNow
    };
}
