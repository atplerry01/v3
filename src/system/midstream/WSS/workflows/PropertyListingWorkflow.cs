namespace Whycespace.System.Midstream.WSS.Workflows;

using Whycespace.Contracts.Workflows;
using Whycespace.System.Midstream.WSS.Contracts;

public sealed class PropertyListingWorkflow : IWorkflowDefinition
{
    public string WorkflowName => "PropertyListing";

    public WorkflowGraph BuildGraph()
    {
        var steps = new List<WorkflowStep>
        {
            new("validate-identity", "Verify Identity", "IdentityVerification", new[] { "validate-policy" }),
            new("validate-policy", "Validate Policy", "PolicyValidation", new[] { "ValidateListing" }),
            new("ValidateListing", "Validate Listing", "PropertyExecution", new[] { "PublishListing" }),
            new("PublishListing", "Publish Listing", "PropertyExecution", Array.Empty<string>())
        };

        return new WorkflowGraph(Guid.NewGuid().ToString(), WorkflowName, steps);
    }
}
