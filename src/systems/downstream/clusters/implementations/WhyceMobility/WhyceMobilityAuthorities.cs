namespace Whycespace.Systems.Downstream.Clusters.Implementations.WhyceMobility;

public static class WhyceMobilityAuthorities
{
    public static readonly IReadOnlyList<string> RequiredAuthorities = new[]
    {
        "TransportLicensing",
        "VehicleRegistration",
        "DriverVerification"
    };

    public static bool HasRequiredAuthority(string authority)
        => RequiredAuthorities.Contains(authority);
}
