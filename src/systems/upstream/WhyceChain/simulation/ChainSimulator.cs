namespace Whycespace.Systems.Upstream.WhyceChain.Simulation;

public sealed record ChainSimulationRequest(
    string EntryType,
    IReadOnlyDictionary<string, object> Data,
    int SimulatedBlockCount);

public sealed record ChainSimulationResult(
    bool IntegrityValid,
    int BlocksSimulated,
    string ProjectedHash,
    bool IsDryRun,
    DateTimeOffset SimulatedAt);

public sealed class ChainSimulator
{
    public ChainSimulationResult Simulate(ChainSimulationRequest request)
    {
        var projectedHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(
                    $"sim:{request.EntryType}:{request.SimulatedBlockCount}:{DateTimeOffset.UtcNow.Ticks}")));

        return new ChainSimulationResult(
            IntegrityValid: true,
            BlocksSimulated: request.SimulatedBlockCount,
            ProjectedHash: projectedHash,
            IsDryRun: true,
            SimulatedAt: DateTimeOffset.UtcNow);
    }
}
