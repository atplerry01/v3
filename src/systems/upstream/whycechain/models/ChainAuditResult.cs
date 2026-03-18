namespace Whycespace.Systems.Upstream.WhyceChain.Models;

public sealed record ChainAuditResult(
    bool Valid,
    int BlocksAudited,
    int EntriesAudited,
    IReadOnlyList<string> Issues,
    DateTimeOffset AuditedAt);
