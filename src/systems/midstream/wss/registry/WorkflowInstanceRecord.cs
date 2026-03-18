namespace Whycespace.Systems.Midstream.WSS.Registry;

using Whycespace.Systems.Midstream.WSS.Models;
using Whycespace.Systems.Midstream.WSS.Definition;
using Whycespace.Systems.Midstream.WSS.Execution;
using Whycespace.Systems.Midstream.WSS.Policies;

public sealed record WorkflowInstanceRecord(
    string InstanceId,
    string WorkflowId,
    string WorkflowName,
    string WorkflowVersion,
    WorkflowInstanceStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string CorrelationId,
    string InitiatedBy
);
