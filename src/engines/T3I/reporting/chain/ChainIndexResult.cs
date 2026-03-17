namespace Whycespace.Engines.T3I.Reporting.Chain;

public sealed record ChainIndexResult(
    Dictionary<long, string> EntryIndexBySequence,
    Dictionary<string, long> EntryIndexByHash,
    Dictionary<long, string> BlockIndexByHeight,
    Dictionary<string, long> BlockIndexByHash,
    Dictionary<string, List<string>> TraceIndex,
    Dictionary<string, List<string>> CorrelationIndex,
    DateTime GeneratedAt,
    string TraceId);
