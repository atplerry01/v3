namespace Whycespace.System.Upstream.WhyceChain.Ledger;

/// <summary>
/// Immutable metadata associated with a ledger entry.
/// Captures origin context of recorded evidence.
/// </summary>
public sealed record ChainLedgerMetadata(
    string OriginSystem,
    string OriginCluster,
    string OriginWorkflow,
    string OriginEngine,
    string ActorIdentityId,
    string PolicyDecisionId,
    string WorkflowInstanceId,
    string Signature);
