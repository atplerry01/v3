namespace Whycespace.Platform.Simulation.Config;

/// <summary>
/// Configuration for the simulation platform system.
/// Defines global simulation parameters, fault injection settings,
/// and integration toggles.
/// </summary>
public sealed record SimulationConfiguration
{
    /// <summary>
    /// Maximum number of concurrent simulation workers.
    /// </summary>
    public int MaxWorkers { get; init; } = 100;

    /// <summary>
    /// Global fault injection rate (0.0 - 1.0).
    /// </summary>
    public double FaultRate { get; init; }

    /// <summary>
    /// Whether to enable Atlas intelligence pipeline integration.
    /// </summary>
    public bool EnableAtlasIntegration { get; init; }

    /// <summary>
    /// Whether to enable WhycePolicy integration for simulation gating.
    /// </summary>
    public bool EnablePolicyIntegration { get; init; }

    /// <summary>
    /// Whether to enable ChaosEngine for advanced fault injection.
    /// </summary>
    public bool EnableChaosEngine { get; init; }

    /// <summary>
    /// Maximum simulation duration before timeout.
    /// </summary>
    public TimeSpan? MaxDuration { get; init; }

    /// <summary>
    /// Output directory for simulation reports.
    /// </summary>
    public string? ReportOutputPath { get; init; }

    public static SimulationConfiguration Default => new();
}
