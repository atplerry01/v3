namespace Whycespace.Systems.Midstream.HEOS.Orchestration;

using Whycespace.Systems.Midstream.HEOS;
using Whycespace.Systems.Midstream.HEOS.Routing;

public sealed class HEOSOrchestrator
{
    private readonly HEOSCoordinator _coordinator;
    private readonly IHEOSRouter _router;

    public HEOSOrchestrator(HEOSCoordinator coordinator, IHEOSRouter router)
    {
        _coordinator = coordinator;
        _router = router;
    }

    public async Task ProcessSignalAsync(EconomicSignal signal, HEOSContext context)
    {
        ArgumentNullException.ThrowIfNull(signal);
        ArgumentNullException.ThrowIfNull(context);

        _coordinator.EmitSignal(signal);
        await _router.RouteSignalAsync(signal, context);
    }

    public decimal EvaluateClusterHealth(string clusterId)
    {
        return _coordinator.GetClusterHealth(clusterId);
    }
}
