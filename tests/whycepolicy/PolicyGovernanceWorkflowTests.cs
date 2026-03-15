using Whycespace.Runtime.Workflows.WhycePolicy;

namespace Whycespace.WhycePolicy.Tests;

public sealed class PolicyGovernanceWorkflowTests
{
    private readonly PolicyGovernanceWorkflow _workflow = new();

    [Fact]
    public void WorkflowName_ReturnsPolicyGovernance()
    {
        Assert.Equal("PolicyGovernance", _workflow.WorkflowName);
    }

    [Fact]
    public void BuildGraph_ReturnsValidGraph()
    {
        var graph = _workflow.BuildGraph();

        Assert.Equal("PolicyGovernance", graph.Name);
        Assert.NotEmpty(graph.Steps);
        Assert.NotNull(graph.WorkflowId);
    }

    [Fact]
    public void BuildGraph_HasSevenSteps()
    {
        var graph = _workflow.BuildGraph();

        Assert.Equal(7, graph.Steps.Count);
    }

    [Fact]
    public void BuildGraph_FirstStepIsPolicySubmission()
    {
        var graph = _workflow.BuildGraph();
        var firstStep = graph.Steps[0];

        Assert.Equal("PolicySubmission", firstStep.StepId);
        Assert.Equal("Policy Submission", firstStep.Name);
        Assert.Equal("PolicyValidation", firstStep.EngineName);
        Assert.Contains("PolicySimulation", firstStep.NextSteps);
    }

    [Fact]
    public void BuildGraph_SimulationStageTransition()
    {
        var graph = _workflow.BuildGraph();
        var step = graph.Steps[1];

        Assert.Equal("PolicySimulation", step.StepId);
        Assert.Equal("PolicySimulation", step.EngineName);
        Assert.Contains("PolicyConflictDetection", step.NextSteps);
    }

    [Fact]
    public void BuildGraph_ConflictDetectionStageTransition()
    {
        var graph = _workflow.BuildGraph();
        var step = graph.Steps[2];

        Assert.Equal("PolicyConflictDetection", step.StepId);
        Assert.Equal("PolicyConflictDetection", step.EngineName);
        Assert.Contains("PolicyImpactForecast", step.NextSteps);
    }

    [Fact]
    public void BuildGraph_GovernanceReviewStage()
    {
        var graph = _workflow.BuildGraph();
        var step = graph.Steps[4];

        Assert.Equal("GovernanceReview", step.StepId);
        Assert.Equal("GovernanceReview", step.EngineName);
        Assert.Contains("GuardianApproval", step.NextSteps);
    }

    [Fact]
    public void BuildGraph_GuardianApprovalRequired()
    {
        var graph = _workflow.BuildGraph();
        var step = graph.Steps[5];

        Assert.Equal("GuardianApproval", step.StepId);
        Assert.Equal("GuardianApproval", step.EngineName);
        Assert.Contains("PolicyActivation", step.NextSteps);
    }

    [Fact]
    public void BuildGraph_PolicyActivationIsFinalStep()
    {
        var graph = _workflow.BuildGraph();
        var lastStep = graph.Steps[^1];

        Assert.Equal("PolicyActivation", lastStep.StepId);
        Assert.Equal("PolicyActivation", lastStep.EngineName);
        Assert.Empty(lastStep.NextSteps);
    }

    [Fact]
    public void BuildGraph_WorkflowRejectionScenario_ApprovalStepCanBlockActivation()
    {
        var graph = _workflow.BuildGraph();
        var approvalStep = graph.Steps.Single(s => s.StepId == "GuardianApproval");
        var activationStep = graph.Steps.Single(s => s.StepId == "PolicyActivation");

        // Guardian approval must precede activation in the DAG
        Assert.Contains("PolicyActivation", approvalStep.NextSteps);
        // Activation has no further steps — terminal
        Assert.Empty(activationStep.NextSteps);
    }

    [Fact]
    public void BuildGraph_DeterministicWorkflowExecution()
    {
        var graph1 = _workflow.BuildGraph();
        var graph2 = _workflow.BuildGraph();

        Assert.Equal(graph1.Name, graph2.Name);
        Assert.Equal(graph1.Steps.Count, graph2.Steps.Count);

        for (var i = 0; i < graph1.Steps.Count; i++)
        {
            Assert.Equal(graph1.Steps[i].StepId, graph2.Steps[i].StepId);
            Assert.Equal(graph1.Steps[i].Name, graph2.Steps[i].Name);
            Assert.Equal(graph1.Steps[i].EngineName, graph2.Steps[i].EngineName);
            Assert.Equal(graph1.Steps[i].NextSteps, graph2.Steps[i].NextSteps);
        }
    }

    [Fact]
    public void PolicyGovernanceState_CanBeCreated()
    {
        var state = new PolicyGovernanceState(
            WorkflowId: "wf-1",
            PolicyId: "pol-1",
            CurrentStage: PolicyGovernanceStage.Submitted,
            SubmittedBy: "actor-1",
            SubmittedAt: DateTimeOffset.UtcNow,
            ApprovalStatus: PolicyApprovalStatus.Pending,
            ActivatedAt: null
        );

        Assert.Equal("wf-1", state.WorkflowId);
        Assert.Equal(PolicyGovernanceStage.Submitted, state.CurrentStage);
        Assert.Equal(PolicyApprovalStatus.Pending, state.ApprovalStatus);
        Assert.Null(state.ActivatedAt);
    }

    [Fact]
    public void PolicyGovernanceCommand_CanBeCreated()
    {
        var command = new PolicyGovernanceCommand(
            CommandId: Guid.NewGuid(),
            Timestamp: DateTimeOffset.UtcNow,
            PolicyDefinition: "test-policy-dsl",
            SubmittedBy: "actor-1",
            SubmissionReason: "New governance policy",
            GovernanceDomain: "platform"
        );

        Assert.Equal("test-policy-dsl", command.PolicyDefinition);
        Assert.Equal("actor-1", command.SubmittedBy);
        Assert.Equal("platform", command.GovernanceDomain);
    }

    [Fact]
    public void BuildGraph_AllStepsFormLinearDag()
    {
        var graph = _workflow.BuildGraph();

        // Every step except the last should have exactly one next step
        for (var i = 0; i < graph.Steps.Count - 1; i++)
        {
            Assert.Single(graph.Steps[i].NextSteps);
            Assert.Equal(graph.Steps[i + 1].StepId, graph.Steps[i].NextSteps[0]);
        }
    }
}
