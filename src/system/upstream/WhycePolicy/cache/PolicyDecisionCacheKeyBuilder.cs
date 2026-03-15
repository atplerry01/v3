namespace Whycespace.System.Upstream.WhycePolicy.Cache;

using global::System.Security.Cryptography;
using global::System.Text;
using Whycespace.System.Upstream.WhycePolicy.Models;

public static class PolicyDecisionCacheKeyBuilder
{
    public static string BuildKey(PolicyContext context, IEnumerable<PolicyDefinition> policies)
    {
        var sb = new StringBuilder();

        sb.Append("ctx:").Append(context.ContextId).Append('|');
        sb.Append("actor:").Append(context.ActorId).Append('|');
        sb.Append("domain:").Append(context.TargetDomain).Append('|');

        foreach (var kvp in context.Attributes.OrderBy(a => a.Key, StringComparer.Ordinal))
        {
            sb.Append(kvp.Key).Append('=').Append(kvp.Value).Append(';');
        }

        sb.Append('|');

        foreach (var policy in policies.OrderBy(p => p.PolicyId, StringComparer.Ordinal))
        {
            sb.Append(policy.PolicyId).Append(':').Append(policy.Version).Append(';');
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash);
    }
}
