namespace Whycespace.Systems.Downstream.Spv.Lifecycle;

public sealed class SpvCreationPolicy
{
    private const decimal MinimumCapital = 1000m;

    public bool CanCreate(string clusterId, decimal allocatedCapital)
    {
        if (string.IsNullOrWhiteSpace(clusterId))
            return false;

        if (allocatedCapital < MinimumCapital)
            return false;

        return true;
    }
}
