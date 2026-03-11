namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyConflictReport(
    string Domain,
    IReadOnlyList<PolicyConflict> Conflicts,
    DateTime GeneratedAt
);
