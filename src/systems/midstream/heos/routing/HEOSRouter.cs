namespace Whycespace.Systems.Midstream.HEOS.Routing;

using Whycespace.Systems.Midstream.HEOS;
using Whycespace.Systems.Midstream.HEOS.Events;
using Whycespace.Systems.Midstream.HEOS.Orchestration;

public sealed class HEOSRouter : IHEOSRouter
{
    private readonly List<HEOSRoutingEvent> _routingLog = new();

    public Task RouteSignalAsync(EconomicSignal signal, HEOSContext context)
    {
        var routingEvent = HEOSRoutingEvent.Create(
            signal.SignalType,
            signal.ClusterId,
            context.TargetClusterId,
            $"Routed:{signal.SignalType}");

        _routingLog.Add(routingEvent);
        return Task.CompletedTask;
    }

    public IReadOnlyList<HEOSRoutingEvent> GetRoutingLog() => _routingLog;
}
