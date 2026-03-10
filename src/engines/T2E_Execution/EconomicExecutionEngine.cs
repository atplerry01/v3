namespace Whycespace.Engines.T2E_Execution;

using Whycespace.Contracts.Engines;

public sealed class EconomicExecutionEngine : IEngine
{
    public string Name => "EconomicExecution";

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var step = context.WorkflowStep;

        return step switch
        {
            "AllocateCapital" => HandleAllocateCapital(context),
            "CreateSpv" => HandleCreateSpv(context),
            "RecordRevenue" => HandleRecordRevenue(context),
            "DistributeProfit" => HandleDistributeProfit(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown step: {step}"))
        };
    }

    private static Task<EngineResult> HandleAllocateCapital(EngineContext context)
    {
        var hasAmount = context.Data.ContainsKey("amount");
        if (!hasAmount)
            return Task.FromResult(EngineResult.Fail("Missing allocation amount"));

        var events = new[] { EngineEvent.Create("CapitalAllocated", Guid.Parse(context.WorkflowId), context.Data) };
        return Task.FromResult(EngineResult.Ok(events));
    }

    private static Task<EngineResult> HandleCreateSpv(EngineContext context)
    {
        var hasName = context.Data.ContainsKey("spvName");
        if (!hasName)
            return Task.FromResult(EngineResult.Fail("Missing SPV name"));

        var events = new[] { EngineEvent.Create("SpvCreated", Guid.Parse(context.WorkflowId), context.Data) };
        return Task.FromResult(EngineResult.Ok(events));
    }

    private static Task<EngineResult> HandleRecordRevenue(EngineContext context)
    {
        var events = new[] { EngineEvent.Create("RevenueRecorded", Guid.Parse(context.WorkflowId), context.Data) };
        return Task.FromResult(EngineResult.Ok(events));
    }

    private static Task<EngineResult> HandleDistributeProfit(EngineContext context)
    {
        var events = new[] { EngineEvent.Create("ProfitDistributed", Guid.Parse(context.WorkflowId), context.Data) };
        return Task.FromResult(EngineResult.Ok(events));
    }
}
