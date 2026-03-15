namespace Whycespace.Engines.T1M.WSS.Governance;

using Whycespace.Contracts.Engines;
using Whycespace.Domain.Events.Governance;
using Whycespace.Engines.T1M.WSS.Governance.Commands;
using Whycespace.Engines.T1M.WSS.Governance.Results;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;
using Whycespace.System.Midstream.WSS.Governance;

[EngineManifest("GovernanceWorkflowEngine", EngineTier.T1M, EngineKind.Decision,
    "GovernanceWorkflowRequest", typeof(EngineEvent))]
public sealed class GovernanceWorkflowEngine : IEngine
{
    private static readonly GovernanceWorkflowStep[] OrderedSteps =
    {
        GovernanceWorkflowStep.ProposalCreated,
        GovernanceWorkflowStep.ProposalSubmitted,
        GovernanceWorkflowStep.ProposalUnderReview,
        GovernanceWorkflowStep.VotingOpen,
        GovernanceWorkflowStep.VotingClosed,
        GovernanceWorkflowStep.QuorumEvaluation,
        GovernanceWorkflowStep.GovernanceDecision,
        GovernanceWorkflowStep.GovernanceExecution,
        GovernanceWorkflowStep.WorkflowCompleted
    };

    public string Name => "GovernanceWorkflowEngine";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string;

        return action switch
        {
            "start" => Task.FromResult(HandleStart(context)),
            "advance" => Task.FromResult(HandleAdvance(context)),
            "complete" => Task.FromResult(HandleComplete(context)),
            _ => Task.FromResult(EngineResult.Fail(
                $"Unknown action '{action}'. Expected: start, advance, complete"))
        };
    }

    public GovernanceWorkflowResult ExecuteStart(StartGovernanceWorkflowCommand command)
    {
        if (command.ProposalId == Guid.Empty)
            return Fail(command.ProposalId, "ProposalId is required");

        if (command.StartedByGuardianId == Guid.Empty)
            return Fail(command.ProposalId, "StartedByGuardianId is required");

        var firstStep = GovernanceWorkflowStep.ProposalCreated;

        return new GovernanceWorkflowResult(
            Success: true,
            ProposalId: command.ProposalId,
            CurrentStep: firstStep.ToString(),
            NextStep: GetNextStepName(firstStep),
            Message: "Governance workflow started",
            ExecutedAt: DateTime.UtcNow);
    }

    public GovernanceWorkflowResult ExecuteAdvance(AdvanceGovernanceWorkflowCommand command)
    {
        if (command.ProposalId == Guid.Empty)
            return Fail(command.ProposalId, "ProposalId is required");

        if (!TryParseStep(command.CurrentStep, out var currentStep))
            return Fail(command.ProposalId, $"Invalid current step: {command.CurrentStep}");

        if (!TryParseStep(command.NextStep, out var nextStep))
            return Fail(command.ProposalId, $"Invalid next step: {command.NextStep}");

        if (currentStep == GovernanceWorkflowStep.WorkflowCompleted)
            return Fail(command.ProposalId, "Workflow is already completed. Cannot advance.");

        if (!IsValidTransition(currentStep, nextStep))
            return Fail(command.ProposalId,
                $"Invalid transition from {command.CurrentStep} to {command.NextStep}");

        return new GovernanceWorkflowResult(
            Success: true,
            ProposalId: command.ProposalId,
            CurrentStep: nextStep.ToString(),
            NextStep: GetNextStepName(nextStep),
            Message: $"Workflow advanced from {command.CurrentStep} to {command.NextStep}",
            ExecutedAt: DateTime.UtcNow);
    }

    public GovernanceWorkflowResult ExecuteComplete(CompleteGovernanceWorkflowCommand command)
    {
        if (command.ProposalId == Guid.Empty)
            return Fail(command.ProposalId, "ProposalId is required");

        if (command.CompletedBy == Guid.Empty)
            return Fail(command.ProposalId, "CompletedBy is required");

        return new GovernanceWorkflowResult(
            Success: true,
            ProposalId: command.ProposalId,
            CurrentStep: GovernanceWorkflowStep.WorkflowCompleted.ToString(),
            NextStep: "",
            Message: "Governance workflow completed",
            ExecutedAt: DateTime.UtcNow);
    }

    private EngineResult HandleStart(EngineContext context)
    {
        var proposalIdStr = context.Data.GetValueOrDefault("proposalId") as string;
        var guardianIdStr = context.Data.GetValueOrDefault("startedByGuardianId") as string;

        if (!Guid.TryParse(proposalIdStr, out var proposalId))
            return EngineResult.Fail("Missing or invalid proposalId");

        if (!Guid.TryParse(guardianIdStr, out var guardianId))
            return EngineResult.Fail("Missing or invalid startedByGuardianId");

        var command = new StartGovernanceWorkflowCommand(
            Guid.NewGuid(), proposalId, guardianId, DateTime.UtcNow);

        var result = ExecuteStart(command);
        if (!result.Success)
            return EngineResult.Fail(result.Message);

        var evt = EngineEvent.Create(
            nameof(GovernanceWorkflowStartedEvent),
            proposalId,
            new Dictionary<string, object>
            {
                ["startedByGuardianId"] = guardianId.ToString(),
                ["currentStep"] = result.CurrentStep
            });

        return EngineResult.Ok(new[] { evt }, new Dictionary<string, object>
        {
            ["proposalId"] = proposalId.ToString(),
            ["currentStep"] = result.CurrentStep,
            ["nextStep"] = result.NextStep,
            ["message"] = result.Message
        });
    }

    private EngineResult HandleAdvance(EngineContext context)
    {
        var proposalIdStr = context.Data.GetValueOrDefault("proposalId") as string;
        var currentStep = context.Data.GetValueOrDefault("currentStep") as string;
        var nextStep = context.Data.GetValueOrDefault("nextStep") as string;
        var triggeredByStr = context.Data.GetValueOrDefault("triggeredBy") as string;

        if (!Guid.TryParse(proposalIdStr, out var proposalId))
            return EngineResult.Fail("Missing or invalid proposalId");

        if (string.IsNullOrWhiteSpace(currentStep) || string.IsNullOrWhiteSpace(nextStep))
            return EngineResult.Fail("Missing currentStep or nextStep");

        Guid.TryParse(triggeredByStr, out var triggeredBy);

        var command = new AdvanceGovernanceWorkflowCommand(
            Guid.NewGuid(), proposalId, currentStep, nextStep, triggeredBy, DateTime.UtcNow);

        var result = ExecuteAdvance(command);
        if (!result.Success)
            return EngineResult.Fail(result.Message);

        var evt = EngineEvent.Create(
            nameof(GovernanceWorkflowAdvancedEvent),
            proposalId,
            new Dictionary<string, object>
            {
                ["previousStep"] = currentStep,
                ["nextStep"] = result.CurrentStep,
                ["advancedBy"] = triggeredBy.ToString()
            });

        return EngineResult.Ok(new[] { evt }, new Dictionary<string, object>
        {
            ["proposalId"] = proposalId.ToString(),
            ["currentStep"] = result.CurrentStep,
            ["nextStep"] = result.NextStep,
            ["message"] = result.Message
        });
    }

    private EngineResult HandleComplete(EngineContext context)
    {
        var proposalIdStr = context.Data.GetValueOrDefault("proposalId") as string;
        var completedByStr = context.Data.GetValueOrDefault("completedBy") as string;

        if (!Guid.TryParse(proposalIdStr, out var proposalId))
            return EngineResult.Fail("Missing or invalid proposalId");

        if (!Guid.TryParse(completedByStr, out var completedBy))
            return EngineResult.Fail("Missing or invalid completedBy");

        var command = new CompleteGovernanceWorkflowCommand(
            Guid.NewGuid(), proposalId, completedBy, DateTime.UtcNow);

        var result = ExecuteComplete(command);
        if (!result.Success)
            return EngineResult.Fail(result.Message);

        var evt = EngineEvent.Create(
            nameof(GovernanceWorkflowCompletedEvent),
            proposalId,
            new Dictionary<string, object>
            {
                ["completedBy"] = completedBy.ToString()
            });

        return EngineResult.Ok(new[] { evt }, new Dictionary<string, object>
        {
            ["proposalId"] = proposalId.ToString(),
            ["currentStep"] = result.CurrentStep,
            ["message"] = result.Message
        });
    }

    private static bool TryParseStep(string? stepName, out GovernanceWorkflowStep step)
    {
        return Enum.TryParse(stepName, ignoreCase: true, out step)
            && Enum.IsDefined(step);
    }

    private static bool IsValidTransition(GovernanceWorkflowStep current, GovernanceWorkflowStep next)
    {
        var currentIndex = Array.IndexOf(OrderedSteps, current);
        var nextIndex = Array.IndexOf(OrderedSteps, next);
        return currentIndex >= 0 && nextIndex == currentIndex + 1;
    }

    private static string GetNextStepName(GovernanceWorkflowStep current)
    {
        var index = Array.IndexOf(OrderedSteps, current);
        if (index >= 0 && index < OrderedSteps.Length - 1)
            return OrderedSteps[index + 1].ToString();
        return "";
    }

    private static GovernanceWorkflowResult Fail(Guid proposalId, string message) =>
        new(false, proposalId, "", "", message, DateTime.UtcNow);
}
