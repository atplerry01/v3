namespace Whycespace.Systems.Downstream.Spv.Lifecycle;

public sealed class SpvTerminationPolicy
{
    public bool CanTerminate(Guid spvId, string reason)
    {
        if (spvId == Guid.Empty)
            return false;

        if (string.IsNullOrWhiteSpace(reason))
            return false;

        return true;
    }
}
