namespace Whycespace.Systems.Midstream.WSS.Models;

public enum WorkflowInstanceStatus
{
    Created = 0,
    Running = 1,
    Waiting = 2,
    Retrying = 3,
    Completed = 4,
    Failed = 5,
    Terminated = 6,
    Cancelled = 7
}
