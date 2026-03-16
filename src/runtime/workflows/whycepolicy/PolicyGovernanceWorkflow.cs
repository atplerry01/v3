namespace Whycespace.Runtime.Workflows.WhycePolicy;

using Whycespace.Contracts.Workflows;
using Whycespace.Systems.Midstream.WSS.Contracts;

public sealed class PolicyGovernanceWorkflow : IWorkflowDefinition
{
    public string WorkflowName => "PolicyGovernance";

    public WorkflowGraph BuildGraph()
    {
        var steps = new List<WorkflowStep>
        {
            new("PolicySubmission", "Policy Submission", "PolicyValidation", new[] { "PolicySimulation" }),
            new("PolicySimulation", "Policy Simulation", "PolicySimulation", new[] { "PolicyConflictDetection" }),
            new("PolicyConflictDetection", "Policy Conflict Detection", "PolicyConflictDetection", new[] { "PolicyImpactForecast" }),
            new("PolicyImpactForecast", "Policy Impact Forecast", "PolicyImpactForecast", new[] { "GovernanceReview" }),
            new("GovernanceReview", "Governance Review", "GovernanceReview", new[] { "GuardianApproval" }),
            new("GuardianApproval", "Guardian / Quorum Approval", "GuardianApproval", new[] { "PolicyActivation" }),
            new("PolicyActivation", "Policy Activation", "PolicyActivation", Array.Empty<string>())
        };

        return new WorkflowGraph(Guid.NewGuid().ToString(), WorkflowName, steps);
    }
}
