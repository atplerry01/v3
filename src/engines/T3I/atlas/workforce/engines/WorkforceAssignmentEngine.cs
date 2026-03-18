using Whycespace.Engines.T3I.Atlas.Workforce.Models;
namespace Whycespace.Engines.T3I.Atlas.Workforce.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Engines.T3I.Shared;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("WorkforceAssignment", EngineTier.T3I, EngineKind.Decision, "WorkforceAssignmentRequest", typeof(EngineEvent))]
public sealed class WorkforceAssignmentEngine : IEngine, IIntelligenceEngine<EngineContext, EngineResult>
{
    public string Name => "WorkforceAssignment";
    public string EngineName => "WorkforceAssignment";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var intelligenceContext = IntelligenceContext<EngineContext>.Create(context.InvocationId, context);
        var result = Execute(intelligenceContext);
        return Task.FromResult(result.Success ? result.Output! : EngineResult.Fail(result.Error!));
    }

    public IntelligenceResult<EngineResult> Execute(IntelligenceContext<EngineContext> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var engineContext = context.Input;

        var taskType = engineContext.Data.GetValueOrDefault("taskType") as string ?? "general";
        var assigneeId = Guid.NewGuid().ToString();

        var events = new[]
        {
            EngineEvent.Create("WorkforceAssigned", Guid.Parse(engineContext.WorkflowId),
                new Dictionary<string, object>
                {
                    ["assigneeId"] = assigneeId,
                    ["taskType"] = taskType
                })
        };

        var engineResult = EngineResult.Ok(events,
            new Dictionary<string, object> { ["assigneeId"] = assigneeId });
        return IntelligenceResult<EngineResult>.Ok(engineResult,
            IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }
}
