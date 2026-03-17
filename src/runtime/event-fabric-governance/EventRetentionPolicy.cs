namespace Whycespace.Runtime.EventFabricGovernance;

public sealed class EventRetentionPolicy
{
    public static readonly TimeSpan DefaultRetention = TimeSpan.FromDays(7);
    public static readonly TimeSpan MinimumRetention = TimeSpan.FromHours(1);
    public static readonly TimeSpan MaximumRetention = TimeSpan.FromDays(365);

    public static void Validate(TimeSpan retention)
    {
        if (retention < MinimumRetention)
        {
            throw new EventFabricGovernanceException(
                $"Retention period {retention} is below minimum of {MinimumRetention}.");
        }

        if (retention > MaximumRetention)
        {
            throw new EventFabricGovernanceException(
                $"Retention period {retention} exceeds maximum of {MaximumRetention}.");
        }
    }
}
