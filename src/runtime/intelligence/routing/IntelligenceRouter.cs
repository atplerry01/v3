namespace Whycespace.IntelligenceRuntime.Routing;

using Whycespace.IntelligenceRuntime.Models;
using Whycespace.IntelligenceRuntime.Registry;

public sealed class IntelligenceRouter
{
    private readonly IntelligenceEngineRegistry _registry;

    public IntelligenceRouter(IntelligenceEngineRegistry registry)
    {
        _registry = registry;
    }

    public IntelligenceEngineDescriptor Route(IntelligenceRequest request)
    {
        var descriptor = _registry.Resolve(request.EngineId);

        if (descriptor.Capability != request.Capability)
        {
            throw new InvalidOperationException(
                $"Engine '{request.EngineId}' is registered under " +
                $"'{descriptor.Capability}' but request targets '{request.Capability}'.");
        }

        return descriptor;
    }

    public IReadOnlyList<IntelligenceEngineDescriptor> ResolveCapability(IntelligenceCapability capability)
        => _registry.GetByCapability(capability);
}
