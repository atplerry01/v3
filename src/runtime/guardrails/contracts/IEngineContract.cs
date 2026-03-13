namespace Whycespace.ArchitectureGuardrails.Contracts;

using Whycespace.Contracts.Engines;

/// <summary>
/// Defines the architectural contract that all engines must satisfy.
/// Engines implementing IEngine are validated against these rules:
/// - Must expose a Name property
/// - Must implement ExecuteAsync accepting EngineContext
/// - Must be sealed classes (no inheritance chains)
/// - Must be stateless (no mutable instance fields)
/// </summary>
public interface IEngineContract
{
    string EngineName { get; }

    Task<EngineResult> ExecuteAsync(
        EngineContext context,
        CancellationToken cancellationToken);
}
