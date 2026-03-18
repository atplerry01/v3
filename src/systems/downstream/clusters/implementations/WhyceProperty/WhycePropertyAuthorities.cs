namespace Whycespace.Systems.Downstream.Clusters.Implementations.WhyceProperty;

public static class WhycePropertyAuthorities
{
    public static readonly IReadOnlyList<string> RequiredAuthorities = new[]
    {
        "PropertyRegistration",
        "TenancyLicensing",
        "BuildingCompliance"
    };

    public static bool HasRequiredAuthority(string authority)
        => RequiredAuthorities.Contains(authority);
}
