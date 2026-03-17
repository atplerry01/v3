namespace Whycespace.Runtime.ControlPlane;

using System.Reflection;
using Whycespace.Runtime.CommandRegistry;
using Whycespace.Runtime.EngineRegistry;
using Whycespace.Runtime.EventSchemaRegistry.Builder;
using Whycespace.Runtime.EventSchemaRegistry.Registry;

public sealed class RuntimeControlPlaneBuilder
{
    private readonly List<Assembly> _assemblies = [];
    private readonly CommandRegistryBuilder _commandBuilder = new();
    private readonly EngineRegistryBuilder _engineBuilder = new();
    private readonly EventRegistryBuilder _eventBuilder = new();

    public RuntimeControlPlaneBuilder AddAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (_assemblies.Contains(assembly))
            return this;

        _assemblies.Add(assembly);
        _commandBuilder.AddAssembly(assembly);
        _engineBuilder.AddAssembly(assembly);
        _eventBuilder.AddAssembly(assembly);

        return this;
    }

    public RuntimeControlPlane Build()
    {
        if (_assemblies.Count == 0)
            throw new RuntimeControlPlaneException("No assemblies added to the control plane builder.");

        CommandRegistry commands;
        EngineRegistry engines;
        EventRegistry events;

        try
        {
            commands = _commandBuilder.Build();
        }
        catch (Exception ex) when (ex is not RuntimeControlPlaneException)
        {
            throw new RuntimeControlPlaneException("Failed to build command registry.", ex);
        }

        try
        {
            engines = _engineBuilder.Build();
        }
        catch (Exception ex) when (ex is not RuntimeControlPlaneException)
        {
            throw new RuntimeControlPlaneException("Failed to build engine registry.", ex);
        }

        try
        {
            events = _eventBuilder.Build();
        }
        catch (Exception ex) when (ex is not RuntimeControlPlaneException)
        {
            throw new RuntimeControlPlaneException("Failed to build event registry.", ex);
        }

        return new RuntimeControlPlane(commands, engines, events);
    }
}
