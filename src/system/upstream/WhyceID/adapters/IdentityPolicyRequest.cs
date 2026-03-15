namespace Whycespace.System.WhyceID.Adapters;

using Whycespace.System.WhyceID.Models;

public sealed record IdentityPolicyRequest(
    string PolicyDomain,
    string Operation,
    IdentityPolicyContext IdentityContext,
    IReadOnlyDictionary<string, string> Metadata
);

public static class IdentityPolicyOperations
{
    public const string CreateIdentity = "CreateIdentity";
    public const string AssignRole = "AssignRole";
    public const string GrantPermission = "GrantPermission";
    public const string RegisterDevice = "RegisterDevice";
    public const string RevokeIdentity = "RevokeIdentity";
    public const string RecoverIdentity = "RecoverIdentity";
}
