namespace Whycespace.System.Midstream.HEOS;

public sealed record EconomicSignal(
    string SignalType,
    string ClusterId,
    decimal Value,
    DateTimeOffset Timestamp
);

public sealed class HEOSCoordinator
{
    private readonly List<EconomicSignal> _signals = new();

    public void EmitSignal(EconomicSignal signal) => _signals.Add(signal);

    public IReadOnlyList<EconomicSignal> GetSignals(string? clusterId = null)
    {
        return clusterId is null
            ? _signals
            : _signals.Where(s => s.ClusterId == clusterId).ToList();
    }

    public decimal GetClusterHealth(string clusterId)
    {
        var signals = GetSignals(clusterId);
        return signals.Count == 0 ? 0m : signals.Average(s => s.Value);
    }
}
