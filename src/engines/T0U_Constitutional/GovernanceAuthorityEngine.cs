namespace Whycespace.Engines.T0U_Constitutional;

using Whycespace.Shared.Contracts;

public sealed class GovernanceAuthorityEngine : IEngine
{
    public string Name => "GovernanceAuthority";

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> AuthorityMatrix =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["ClusterAdmin"] = new[] { "cluster.create", "cluster.update", "cluster.suspend", "subcluster.manage" },
            ["SPVManager"] = new[] { "spv.create", "spv.dissolve", "capital.allocate", "capital.withdraw", "revenue.distribute" },
            ["SystemAdmin"] = new[] { "cluster.create", "cluster.update", "cluster.suspend", "subcluster.manage",
                                      "spv.create", "spv.dissolve", "capital.allocate", "capital.withdraw", "revenue.distribute",
                                      "policy.create", "policy.update", "governance.override", "system.configure" }
        };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var role = context.Data.GetValueOrDefault("governanceRole") as string;
        var action = context.Data.GetValueOrDefault("action") as string;
        var userId = context.Data.GetValueOrDefault("userId") as string;

        if (string.IsNullOrEmpty(role))
            return Task.FromResult(EngineResult.Fail("Missing governanceRole"));

        if (string.IsNullOrEmpty(action))
            return Task.FromResult(EngineResult.Fail("Missing action"));

        if (string.IsNullOrEmpty(userId))
            return Task.FromResult(EngineResult.Fail("Missing userId"));

        var authorized = ValidateAuthority(role, action);

        if (!authorized)
        {
            var deniedEvents = new[]
            {
                EngineEvent.Create("GovernanceAuthorityDenied", Guid.Parse(context.WorkflowId),
                    new Dictionary<string, object>
                    {
                        ["userId"] = userId,
                        ["role"] = role,
                        ["action"] = action,
                        ["authorized"] = false
                    })
            };

            return Task.FromResult(EngineResult.Ok(deniedEvents,
                new Dictionary<string, object> { ["authorized"] = false, ["reason"] = $"Role '{role}' lacks authority for action '{action}'" }));
        }

        var grantedEvents = new[]
        {
            EngineEvent.Create("GovernanceAuthorityGranted", Guid.Parse(context.WorkflowId),
                new Dictionary<string, object>
                {
                    ["userId"] = userId,
                    ["role"] = role,
                    ["action"] = action,
                    ["authorized"] = true
                })
        };

        return Task.FromResult(EngineResult.Ok(grantedEvents,
            new Dictionary<string, object> { ["authorized"] = true }));
    }

    private static bool ValidateAuthority(string role, string action)
    {
        if (!AuthorityMatrix.TryGetValue(role, out var permissions))
            return false;

        return permissions.Contains(action);
    }
}
