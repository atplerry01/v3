namespace Whycespace.Engines.T2E.Economic.Capital;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("CapitalCommitment", EngineTier.T2E, EngineKind.Mutation, "CommitCapitalCommand", typeof(EngineEvent))]
public sealed class CapitalCommitmentEngine : IEngine
{
    public string Name => "CapitalCommitment";

    private static readonly string[] SupportedCurrencies = { "GBP", "USD", "EUR", "NGN" };
    private static readonly string[] ValidStatuses = { "Pending", "Updated", "Cancelled", "Fulfilled" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var step = context.WorkflowStep;

        return step switch
        {
            "CommitCapital" => HandleCommitCapital(context),
            "UpdateCapitalCommitment" => HandleUpdateCommitment(context),
            "CancelCapitalCommitment" => HandleCancelCommitment(context),
            "FulfillCapitalCommitment" => HandleFulfillCommitment(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown workflow step: {step}"))
        };
    }

    private Task<EngineResult> HandleCommitCapital(EngineContext context)
    {
        var commitmentId = context.Data.GetValueOrDefault("commitmentId") as string;
        if (string.IsNullOrEmpty(commitmentId))
            commitmentId = Guid.NewGuid().ToString();
        if (!Guid.TryParse(commitmentId, out var commitmentGuid))
            return Task.FromResult(EngineResult.Fail("Invalid commitmentId format"));

        var poolId = context.Data.GetValueOrDefault("poolId") as string;
        if (string.IsNullOrEmpty(poolId))
            return Task.FromResult(EngineResult.Fail("Missing poolId"));
        if (!Guid.TryParse(poolId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid poolId format"));

        var investorIdentityId = context.Data.GetValueOrDefault("investorIdentityId") as string;
        if (string.IsNullOrEmpty(investorIdentityId))
            return Task.FromResult(EngineResult.Fail("Missing investorIdentityId"));
        if (!Guid.TryParse(investorIdentityId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid investorIdentityId format"));

        var amount = ResolveDecimal(context.Data.GetValueOrDefault("amount"));
        if (amount is null)
            return Task.FromResult(EngineResult.Fail("Missing commitment amount"));
        if (amount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("Commitment amount must be positive"));

        var currency = context.Data.GetValueOrDefault("currency") as string ?? "GBP";
        if (!Array.Exists(SupportedCurrencies, c => c == currency))
            return Task.FromResult(EngineResult.Fail($"Unsupported currency: {currency}. Supported: {string.Join(", ", SupportedCurrencies)}"));

        var committedBy = context.Data.GetValueOrDefault("committedBy") as string;
        if (string.IsNullOrEmpty(committedBy))
            return Task.FromResult(EngineResult.Fail("Missing committedBy"));

        var committedAt = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalCommitted", commitmentGuid,
                new Dictionary<string, object>
                {
                    ["commitmentId"] = commitmentId,
                    ["poolId"] = poolId,
                    ["investorIdentityId"] = investorIdentityId,
                    ["amount"] = amount.Value,
                    ["currency"] = currency,
                    ["committedBy"] = committedBy,
                    ["status"] = "Pending",
                    ["timestamp"] = committedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["commitmentId"] = commitmentId,
                ["poolId"] = poolId,
                ["investorIdentityId"] = investorIdentityId,
                ["amount"] = amount.Value,
                ["currency"] = currency,
                ["status"] = "Pending",
                ["timestamp"] = committedAt.ToString("O")
            }));
    }

    private static Task<EngineResult> HandleUpdateCommitment(EngineContext context)
    {
        var commitmentId = context.Data.GetValueOrDefault("commitmentId") as string;
        if (string.IsNullOrEmpty(commitmentId))
            return Task.FromResult(EngineResult.Fail("Missing commitmentId"));
        if (!Guid.TryParse(commitmentId, out var commitmentGuid))
            return Task.FromResult(EngineResult.Fail("Invalid commitmentId format"));

        var newAmount = ResolveDecimal(context.Data.GetValueOrDefault("newAmount"));
        if (newAmount is null)
            return Task.FromResult(EngineResult.Fail("Missing newAmount"));
        if (newAmount.Value <= 0)
            return Task.FromResult(EngineResult.Fail("New amount must be positive"));

        var updatedBy = context.Data.GetValueOrDefault("updatedBy") as string;
        if (string.IsNullOrEmpty(updatedBy))
            return Task.FromResult(EngineResult.Fail("Missing updatedBy"));

        var updatedAt = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalCommitmentUpdated", commitmentGuid,
                new Dictionary<string, object>
                {
                    ["commitmentId"] = commitmentId,
                    ["newAmount"] = newAmount.Value,
                    ["updatedBy"] = updatedBy,
                    ["timestamp"] = updatedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["commitmentId"] = commitmentId,
                ["newAmount"] = newAmount.Value,
                ["status"] = "Updated",
                ["timestamp"] = updatedAt.ToString("O")
            }));
    }

    private static Task<EngineResult> HandleCancelCommitment(EngineContext context)
    {
        var commitmentId = context.Data.GetValueOrDefault("commitmentId") as string;
        if (string.IsNullOrEmpty(commitmentId))
            return Task.FromResult(EngineResult.Fail("Missing commitmentId"));
        if (!Guid.TryParse(commitmentId, out var commitmentGuid))
            return Task.FromResult(EngineResult.Fail("Invalid commitmentId format"));

        var reason = context.Data.GetValueOrDefault("reason") as string;
        if (string.IsNullOrEmpty(reason))
            return Task.FromResult(EngineResult.Fail("Missing cancellation reason"));

        var cancelledBy = context.Data.GetValueOrDefault("cancelledBy") as string;
        if (string.IsNullOrEmpty(cancelledBy))
            return Task.FromResult(EngineResult.Fail("Missing cancelledBy"));

        var cancelledAt = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalCommitmentCancelled", commitmentGuid,
                new Dictionary<string, object>
                {
                    ["commitmentId"] = commitmentId,
                    ["reason"] = reason,
                    ["cancelledBy"] = cancelledBy,
                    ["timestamp"] = cancelledAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["commitmentId"] = commitmentId,
                ["status"] = "Cancelled",
                ["reason"] = reason,
                ["timestamp"] = cancelledAt.ToString("O")
            }));
    }

    private static Task<EngineResult> HandleFulfillCommitment(EngineContext context)
    {
        var commitmentId = context.Data.GetValueOrDefault("commitmentId") as string;
        if (string.IsNullOrEmpty(commitmentId))
            return Task.FromResult(EngineResult.Fail("Missing commitmentId"));
        if (!Guid.TryParse(commitmentId, out var commitmentGuid))
            return Task.FromResult(EngineResult.Fail("Invalid commitmentId format"));

        var contributionId = context.Data.GetValueOrDefault("contributionId") as string;
        if (string.IsNullOrEmpty(contributionId))
            return Task.FromResult(EngineResult.Fail("Missing contributionId"));
        if (!Guid.TryParse(contributionId, out _))
            return Task.FromResult(EngineResult.Fail("Invalid contributionId format"));

        var fulfilledBy = context.Data.GetValueOrDefault("fulfilledBy") as string;
        if (string.IsNullOrEmpty(fulfilledBy))
            return Task.FromResult(EngineResult.Fail("Missing fulfilledBy"));

        var fulfilledAt = DateTimeOffset.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("CapitalCommitmentFulfilled", commitmentGuid,
                new Dictionary<string, object>
                {
                    ["commitmentId"] = commitmentId,
                    ["contributionId"] = contributionId,
                    ["fulfilledBy"] = fulfilledBy,
                    ["timestamp"] = fulfilledAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        return Task.FromResult(EngineResult.Ok(events,
            new Dictionary<string, object>
            {
                ["commitmentId"] = commitmentId,
                ["contributionId"] = contributionId,
                ["status"] = "Fulfilled",
                ["timestamp"] = fulfilledAt.ToString("O")
            }));
    }

    private static decimal? ResolveDecimal(object? value)
    {
        return value switch
        {
            decimal d => d,
            double d => (decimal)d,
            int i => i,
            long l => l,
            string s when decimal.TryParse(s, out var parsed) => parsed,
            _ => null
        };
    }
}
