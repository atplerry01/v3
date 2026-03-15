namespace Whycespace.System.Upstream.WhycePolicy.Dsl;

public enum PolicyActionType
{
    Allow = 0,
    Deny = 1,
    RequireApproval = 2,
    RequireGuardian = 3,
    RequireQuorum = 4
}
