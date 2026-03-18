namespace Whycespace.Systems.Midstream.HEOS.Routing;

using Whycespace.Systems.Midstream.HEOS;
using Whycespace.Systems.Midstream.HEOS.Orchestration;

public interface IHEOSRouter
{
    Task RouteSignalAsync(EconomicSignal signal, HEOSContext context);
}
