namespace Whycespace.System.Midstream.WSS.Workflows;

using Whycespace.Contracts.Workflows;
using Whycespace.System.Midstream.WSS.Contracts;

public sealed class RideRequestWorkflow : IWorkflowDefinition
{
    public string WorkflowName => "RideRequest";

    public WorkflowGraph BuildGraph()
    {
        var steps = new List<WorkflowStep>
        {
            new("validate-policy", "Validate Policy", "PolicyValidation", new[] { "match-driver" }),
            new("match-driver", "Match Driver", "DriverMatching", new[] { "ValidateRequest" }),
            new("ValidateRequest", "Validate Request", "RideExecution", new[] { "AssignDriver" }),
            new("AssignDriver", "Assign Driver", "RideExecution", new[] { "CompleteTrip" }),
            new("CompleteTrip", "Complete Trip", "RideExecution", Array.Empty<string>())
        };

        return new WorkflowGraph(Guid.NewGuid().ToString(), WorkflowName, steps);
    }
}
