namespace Whycespace.Engines.T0U_Constitutional;

using Whycespace.Contracts.Engines;
using Whycespace.EngineManifest.Manifest;
using Whycespace.EngineManifest.Models;

[EngineManifest("ConstitutionalSafeguard", EngineTier.T0U, EngineKind.Validation, "ConstitutionalSafeguardRequest", typeof(EngineEvent))]
public sealed class ConstitutionalSafeguardEngine : IEngine
{
    public string Name => "ConstitutionalSafeguard";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var violations = new List<string>();

        CheckConstitutionalRules(context.Data, violations);
        CheckPolicyRules(context.Data, violations);
        CheckGovernanceBoundaries(context.Data, violations);

        if (violations.Count > 0)
        {
            var rejectedEvents = new[]
            {
                EngineEvent.Create("ConstitutionalViolation", Guid.Parse(context.WorkflowId),
                    new Dictionary<string, object>
                    {
                        ["violations"] = string.Join("; ", violations),
                        ["violationCount"] = violations.Count
                    })
            };

            return Task.FromResult(EngineResult.Ok(rejectedEvents,
                new Dictionary<string, object>
                {
                    ["safeguardPassed"] = false,
                    ["violations"] = violations.ToArray(),
                    ["reason"] = $"Command rejected: {violations.Count} constitutional violation(s)"
                }));
        }

        var passedEvents = new[]
        {
            EngineEvent.Create("ConstitutionalSafeguardPassed", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object> { ["safeguardPassed"] = true })
        };

        return Task.FromResult(EngineResult.Ok(passedEvents,
            new Dictionary<string, object> { ["safeguardPassed"] = true }));
    }

    private static void CheckConstitutionalRules(IReadOnlyDictionary<string, object> data, List<string> violations)
    {
        // Rule: engines must not call other engines directly
        if (data.ContainsKey("targetEngine"))
            violations.Add("Constitutional: direct engine-to-engine invocation is forbidden");

        // Rule: workflows must not contain business logic flags
        if (data.ContainsKey("inlineBusinessLogic"))
            violations.Add("Constitutional: business logic in workflow definition is forbidden");

        // Rule: clusters must not contain domain model references
        if (data.ContainsKey("clusterDomainModel"))
            violations.Add("Constitutional: domain models in clusters are forbidden");

        // Rule: stateful engine execution is forbidden
        if (data.ContainsKey("engineMutableState"))
            violations.Add("Constitutional: mutable state in engine execution is forbidden");

        // Rule: decision engines must not perform direct database access
        if (data.ContainsKey("directDatabaseAccess") && data.ContainsKey("decisionEngine"))
            violations.Add("Constitutional: direct database access from decision engines is forbidden");
    }

    private static void CheckPolicyRules(IReadOnlyDictionary<string, object> data, List<string> violations)
    {
        // Rule: all commands require an authenticated identity
        if (data.TryGetValue("requiresIdentity", out var req) && req is true && !data.ContainsKey("userId"))
            violations.Add("Policy: authenticated identity required but userId missing");

        // Rule: economic operations require valid vault reference
        if (data.TryGetValue("operationType", out var opType) && opType as string == "economic"
            && !data.ContainsKey("vaultId"))
            violations.Add("Policy: economic operations require a valid vaultId");

        // Rule: negative amounts are forbidden
        if (data.TryGetValue("amount", out var amountObj))
        {
            var isNegative = amountObj switch
            {
                decimal d => d < 0,
                double d => d < 0,
                int i => i < 0,
                long l => l < 0,
                _ => false
            };
            if (isNegative)
                violations.Add("Policy: negative transaction amounts are forbidden");
        }
    }

    private static void CheckGovernanceBoundaries(IReadOnlyDictionary<string, object> data, List<string> violations)
    {
        // Rule: cross-cluster operations require explicit authorization
        if (data.ContainsKey("sourceClusterId") && data.ContainsKey("targetClusterId"))
        {
            var source = data["sourceClusterId"] as string;
            var target = data["targetClusterId"] as string;
            if (source != target && !data.ContainsKey("crossClusterAuthorization"))
                violations.Add("Governance: cross-cluster operations require explicit authorization");
        }

        // Rule: SPV dissolution requires governance approval
        if (data.TryGetValue("action", out var action) && action as string == "spv.dissolve"
            && !data.ContainsKey("governanceApproval"))
            violations.Add("Governance: SPV dissolution requires governance approval");
    }
}
