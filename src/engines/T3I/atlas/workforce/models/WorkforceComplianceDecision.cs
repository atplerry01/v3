namespace Whycespace.Engines.T3I.Atlas.Workforce.Models;

public sealed record WorkforceComplianceDecision(
    bool Compliant,
    decimal ComplianceScore,
    IReadOnlyList<string> Violations,
    IReadOnlyList<string> Recommendations
);
