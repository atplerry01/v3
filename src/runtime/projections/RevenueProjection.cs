namespace Whycespace.Runtime.Projections;

using Whycespace.Contracts.Events;
using Whycespace.Shared.Projections;

public sealed class RevenueProjection : IProjection
{
    private readonly Dictionary<string, decimal> _revenues = new();

    public string Name => "Revenue";

    public Task HandleAsync(SystemEvent @event)
    {
        if (@event.EventType == "RevenueRecorded")
        {
            var spvId = @event.AggregateId.ToString();
            if (@event.Payload.GetValueOrDefault("amount") is decimal amount)
                _revenues[spvId] = _revenues.GetValueOrDefault(spvId) + amount;
        }
        return Task.CompletedTask;
    }

    public IReadOnlyDictionary<string, decimal> GetRevenues() => _revenues;
}
