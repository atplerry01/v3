namespace Whycespace.Engines.T2E.Economic.Vault;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultAllocation", EngineTier.T2E, EngineKind.Mutation, "ExecuteVaultAllocationCommand", typeof(EngineEvent))]
public sealed class VaultAllocationEngine : IEngine
{
    public string Name => "VaultAllocation";

    private static readonly string[] ValidAllocationTypes =
        { "Ownership", "Investment", "ProfitDistribution", "Treasury", "Reserve" };

    private static readonly string[] ValidLifecycleActions =
        { "Create", "Suspend", "Close" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        var action = context.Data.GetValueOrDefault("action") as string ?? "Create";

        return action switch
        {
            "Create" => HandleCreate(context),
            "Suspend" => HandleSuspend(context),
            "Close" => HandleClose(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown action: {action}. Supported: {string.Join(", ", ValidLifecycleActions)}"))
        };
    }

    private static Task<EngineResult> HandleCreate(EngineContext context)
    {
        // --- Validate AllocationId ---
        var allocationIdRaw = context.Data.GetValueOrDefault("allocationId") as string;
        if (string.IsNullOrEmpty(allocationIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing allocationId"));
        if (!Guid.TryParse(allocationIdRaw, out var allocationId))
            return Task.FromResult(EngineResult.Fail("Invalid allocationId format"));

        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out var vaultId))
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate RecipientIdentityId ---
        var recipientIdRaw = context.Data.GetValueOrDefault("recipientIdentityId") as string;
        if (string.IsNullOrEmpty(recipientIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing recipientIdentityId"));
        if (!Guid.TryParse(recipientIdRaw, out var recipientIdentityId))
            return Task.FromResult(EngineResult.Fail("Invalid recipientIdentityId format"));

        // --- Validate InitiatorIdentityId ---
        var initiatorIdRaw = context.Data.GetValueOrDefault("initiatorIdentityId") as string;
        if (string.IsNullOrEmpty(initiatorIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing initiatorIdentityId"));
        if (!Guid.TryParse(initiatorIdRaw, out var initiatorIdentityId))
            return Task.FromResult(EngineResult.Fail("Invalid initiatorIdentityId format"));

        // --- Validate AllocationType ---
        var allocationType = context.Data.GetValueOrDefault("allocationType") as string;
        if (string.IsNullOrEmpty(allocationType))
            return Task.FromResult(EngineResult.Fail("Missing allocationType"));
        if (!Array.Exists(ValidAllocationTypes, t => t == allocationType))
            return Task.FromResult(EngineResult.Fail($"Invalid allocationType: {allocationType}. Supported: {string.Join(", ", ValidAllocationTypes)}"));

        // --- Validate AllocationPercentage ---
        var allocationPercentage = ResolveDecimal(context.Data.GetValueOrDefault("allocationPercentage"));
        if (allocationPercentage is null)
            return Task.FromResult(EngineResult.Fail("Missing or invalid allocationPercentage"));
        if (allocationPercentage.Value < 0 || allocationPercentage.Value > 100)
            return Task.FromResult(EngineResult.Fail("AllocationPercentage must be between 0 and 100"));

        // --- Validate AllocationAmount (optional, defaults to 0) ---
        var allocationAmount = ResolveDecimal(context.Data.GetValueOrDefault("allocationAmount")) ?? 0m;
        if (allocationAmount < 0)
            return Task.FromResult(EngineResult.Fail("AllocationAmount cannot be negative"));

        // --- Validate ownership allocation total does not exceed 100% ---
        if (allocationType == "Ownership")
        {
            var existingOwnershipTotal = ResolveDecimal(context.Data.GetValueOrDefault("existingOwnershipTotal")) ?? 0m;
            if (existingOwnershipTotal + allocationPercentage.Value > 100)
                return Task.FromResult(EngineResult.Fail(
                    $"Ownership allocations would exceed 100%: existing {existingOwnershipTotal}% + new {allocationPercentage.Value}% = {existingOwnershipTotal + allocationPercentage.Value}%"));
        }

        // --- Optional fields ---
        var description = context.Data.GetValueOrDefault("description") as string ?? "";
        var allocationReference = context.Data.GetValueOrDefault("allocationReference") as string ?? "";

        var completedAt = DateTimeOffset.UtcNow;
        var events = new List<EngineEvent>();

        // Event 1: VaultAllocationCreated
        events.Add(EngineEvent.Create("VaultAllocationCreated", allocationId,
            new Dictionary<string, object>
            {
                ["allocationId"] = allocationIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["recipientIdentityId"] = recipientIdRaw,
                ["initiatorIdentityId"] = initiatorIdRaw,
                ["allocationType"] = allocationType,
                ["allocationPercentage"] = allocationPercentage.Value,
                ["allocationAmount"] = allocationAmount,
                ["allocationStatus"] = "Active",
                ["description"] = description,
                ["allocationReference"] = allocationReference,
                ["completedAt"] = completedAt.ToString("O"),
                ["topic"] = "whyce.economic.events"
            }));

        // Event 2: VaultAllocationRegistered (registry integration)
        events.Add(EngineEvent.Create("VaultAllocationRegistered", allocationId,
            new Dictionary<string, object>
            {
                ["allocationId"] = allocationIdRaw,
                ["vaultId"] = vaultIdRaw,
                ["recipientIdentityId"] = recipientIdRaw,
                ["allocationType"] = allocationType,
                ["allocationPercentage"] = allocationPercentage.Value,
                ["allocationAmount"] = allocationAmount,
                ["allocationStatus"] = "Active",
                ["topic"] = "whyce.economic.events"
            }));

        var output = new Dictionary<string, object>
        {
            ["allocationId"] = allocationIdRaw,
            ["vaultId"] = vaultIdRaw,
            ["recipientIdentityId"] = recipientIdRaw,
            ["allocationType"] = allocationType,
            ["allocationPercentage"] = allocationPercentage.Value,
            ["allocationAmount"] = allocationAmount,
            ["allocationStatus"] = "Active",
            ["completedAt"] = completedAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static Task<EngineResult> HandleSuspend(EngineContext context)
    {
        // --- Validate AllocationId ---
        var allocationIdRaw = context.Data.GetValueOrDefault("allocationId") as string;
        if (string.IsNullOrEmpty(allocationIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing allocationId"));
        if (!Guid.TryParse(allocationIdRaw, out var allocationId))
            return Task.FromResult(EngineResult.Fail("Invalid allocationId format"));

        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate current status ---
        var currentStatus = context.Data.GetValueOrDefault("currentAllocationStatus") as string;
        if (currentStatus != "Active")
            return Task.FromResult(EngineResult.Fail("Only Active allocations can be suspended"));

        var initiatorIdRaw = context.Data.GetValueOrDefault("initiatorIdentityId") as string ?? "";
        var completedAt = DateTimeOffset.UtcNow;

        var events = new List<EngineEvent>
        {
            EngineEvent.Create("VaultAllocationSuspended", allocationId,
                new Dictionary<string, object>
                {
                    ["allocationId"] = allocationIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["initiatorIdentityId"] = initiatorIdRaw,
                    ["allocationStatus"] = "Suspended",
                    ["suspendedAt"] = completedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["allocationId"] = allocationIdRaw,
            ["vaultId"] = vaultIdRaw,
            ["allocationStatus"] = "Suspended",
            ["completedAt"] = completedAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static Task<EngineResult> HandleClose(EngineContext context)
    {
        // --- Validate AllocationId ---
        var allocationIdRaw = context.Data.GetValueOrDefault("allocationId") as string;
        if (string.IsNullOrEmpty(allocationIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing allocationId"));
        if (!Guid.TryParse(allocationIdRaw, out var allocationId))
            return Task.FromResult(EngineResult.Fail("Invalid allocationId format"));

        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate current status (cannot close already-closed allocations) ---
        var currentStatus = context.Data.GetValueOrDefault("currentAllocationStatus") as string;
        if (currentStatus == "Closed")
            return Task.FromResult(EngineResult.Fail("Allocation is already closed and cannot be reactivated"));

        var initiatorIdRaw = context.Data.GetValueOrDefault("initiatorIdentityId") as string ?? "";
        var completedAt = DateTimeOffset.UtcNow;

        var events = new List<EngineEvent>
        {
            EngineEvent.Create("VaultAllocationClosed", allocationId,
                new Dictionary<string, object>
                {
                    ["allocationId"] = allocationIdRaw,
                    ["vaultId"] = vaultIdRaw,
                    ["initiatorIdentityId"] = initiatorIdRaw,
                    ["allocationStatus"] = "Closed",
                    ["closedAt"] = completedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["allocationId"] = allocationIdRaw,
            ["vaultId"] = vaultIdRaw,
            ["allocationStatus"] = "Closed",
            ["completedAt"] = completedAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
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
