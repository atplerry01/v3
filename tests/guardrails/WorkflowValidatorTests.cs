namespace Whycespace.Tests.Guardrails;

using Whycespace.ArchitectureGuardrails.Validation;
using Whycespace.Engines.T0U.WhycePolicy;
using Whycespace.Engines.T2E;
using Whycespace.Engines.T2E.Clusters.Mobility.Taxi;
using Whycespace.Engines.T2E.Clusters.Property.Letting;
using Whycespace.Engines.T3I.Clusters.Mobility.Taxi;
using Whycespace.Engines.T3I.Clusters.Property.Letting;
using Whycespace.Engines.T3I.Core.Workforce;
using Whycespace.Runtime.Registry;
using Whycespace.Contracts.Workflows;
using Whycespace.System.Midstream.WSS.Workflows;

public sealed class WorkflowValidatorTests
{
    private static EngineRegistry CreateRegistry()
    {
        var registry = new EngineRegistry();
        registry.Register(new PolicyValidationEngine());
        registry.Register(new DriverMatchingEngine());
        registry.Register(new TenantMatchingEngine());
        registry.Register(new WorkforceAssignmentEngine());
        registry.Register(new RideExecutionEngine());
        registry.Register(new PropertyExecutionEngine());
        registry.Register(new EconomicExecutionEngine());
        return registry;
    }

    [Fact]
    public void RideRequestWorkflow_Passes_Validation()
    {
        var registry = CreateRegistry();
        var validator = new WorkflowArchitectureValidator(registry);
        var graph = new RideRequestWorkflow().BuildGraph();

        var result = validator.ValidateWorkflow(graph);
        Assert.True(result.IsValid, string.Join("; ", result.Violations));
    }

    [Fact]
    public void PropertyListingWorkflow_Passes_Validation()
    {
        var registry = CreateRegistry();
        var validator = new WorkflowArchitectureValidator(registry);
        var graph = new PropertyListingWorkflow().BuildGraph();

        var result = validator.ValidateWorkflow(graph);
        Assert.True(result.IsValid, string.Join("; ", result.Violations));
    }

    [Fact]
    public void EconomicLifecycleWorkflow_Passes_Validation()
    {
        var registry = CreateRegistry();
        var validator = new WorkflowArchitectureValidator(registry);
        var graph = new EconomicLifecycleWorkflow().BuildGraph();

        var result = validator.ValidateWorkflow(graph);
        Assert.True(result.IsValid, string.Join("; ", result.Violations));
    }

    [Fact]
    public void Workflow_WithUnregisteredEngine_Fails()
    {
        var registry = new EngineRegistry(); // empty registry
        var validator = new WorkflowArchitectureValidator(registry);

        var graph = new WorkflowGraph(
            global::System.Guid.NewGuid().ToString(),
            "TestWorkflow",
            new[]
            {
                new WorkflowStep("step-1", "Step One", "NonExistentEngine", global::System.Array.Empty<string>())
            });

        var result = validator.ValidateWorkflow(graph);
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("NonExistentEngine"));
    }

    [Fact]
    public void Workflow_WithEmptySteps_Fails()
    {
        var registry = new EngineRegistry();
        var validator = new WorkflowArchitectureValidator(registry);

        var graph = new WorkflowGraph(
            global::System.Guid.NewGuid().ToString(),
            "EmptyWorkflow",
            global::System.Array.Empty<WorkflowStep>());

        var result = validator.ValidateWorkflow(graph);
        Assert.False(result.IsValid);
        Assert.Contains(result.Violations, v => v.Contains("at least one step"));
    }
}
