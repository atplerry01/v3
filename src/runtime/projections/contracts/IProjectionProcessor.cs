namespace Whycespace.Projections.Contracts;

public interface IProjectionProcessor<in TEvent> where TEvent : class
{
    string ProjectionName { get; }

    IReadOnlyCollection<string> HandledEventTypes { get; }

    Task ProcessAsync(ProjectionEvent projectionEvent, TEvent payload);
}
