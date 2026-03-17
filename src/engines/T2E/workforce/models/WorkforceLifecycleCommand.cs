namespace Whycespace.Engines.T2E.Workforce.Models;

public enum LifecycleAction
{
    Activate,
    Suspend,
    Terminate,
    Reactivate,
    SetUnavailable
}

public sealed record WorkforceLifecycleCommand(
    Guid WorkforceId,
    LifecycleAction LifecycleAction,
    Guid RequestedByOperatorId,
    string Reason,
    DateTimeOffset Timestamp
);
