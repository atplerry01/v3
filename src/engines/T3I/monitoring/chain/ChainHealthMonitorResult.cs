namespace Whycespace.Engines.T3I.Monitoring.Chain;

public sealed record ChainHealthMonitorResult(
    long CurrentChainHeight,
    long SnapshotHeight,
    long ReplicationLag,
    long AnchorLag,
    string LedgerIntegrityStatus,
    string BlockContinuityStatus,
    string ChainHealthStatus,
    DateTime HealthTimestamp,
    string TraceId);
