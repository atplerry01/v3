namespace Whycespace.Systems.Midstream.WSS.Workflows;

using Whycespace.Contracts.Workflows;
using Whycespace.Systems.Midstream.WSS.Contracts;

public sealed class EconomicLifecycleWorkflow : IWorkflowDefinition
{
    public string WorkflowName => "EconomicLifecycle";

    public WorkflowGraph BuildGraph()
    {
        var steps = new List<WorkflowStep>
        {
            new("AllocateCapital", "Allocate Capital", "EconomicExecution", new[] { "CreateSpv" }),
            new("CreateSpv", "Create SPV", "EconomicExecution", new[] { "RecordRevenue" }),
            new("RecordRevenue", "Record Revenue", "EconomicExecution", new[] { "DistributeProfit" }),
            new("DistributeProfit", "Distribute Profit", "EconomicExecution", Array.Empty<string>())
        };

        return new WorkflowGraph(Guid.NewGuid().ToString(), WorkflowName, steps);
    }
}
