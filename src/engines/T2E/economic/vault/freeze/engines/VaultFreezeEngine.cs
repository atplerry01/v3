namespace Whycespace.Engines.T2E.Economic.Vault.Freeze.Engines;

using Whycespace.Contracts.Engines;
using Whycespace.Runtime.EngineManifest.Attributes;
using Whycespace.Runtime.EngineManifest.Models;

[EngineManifest("VaultFreeze", EngineTier.T2E, EngineKind.Mutation, "ExecuteVaultFreezeCommand", typeof(EngineEvent))]
public sealed class VaultFreezeEngine : IEngine
{
    public string Name => "VaultFreeze";

    private static readonly string[] ValidFreezeScopes = { "Vault", "VaultAccount", "OperationType" };

    public Task<EngineResult> ExecuteAsync(EngineContext context)
    {
        return context.WorkflowStep switch
        {
            "ExecuteVaultFreeze" => ExecuteFreeze(context),
            "ExecuteVaultUnfreeze" => ExecuteUnfreeze(context),
            "ValidateFreezeStatus" => ValidateFreezeStatus(context),
            _ => Task.FromResult(EngineResult.Fail($"Unknown command: {context.WorkflowStep}"))
        };
    }

    private Task<EngineResult> ExecuteFreeze(EngineContext context)
    {
        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate VaultAccountId ---
        var vaultAccountIdRaw = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultAccountId"));
        if (!Guid.TryParse(vaultAccountIdRaw, out var vaultAccountId) || vaultAccountId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid vaultAccountId format"));

        // --- Validate RequestedBy ---
        var requestedByRaw = context.Data.GetValueOrDefault("requestedBy") as string;
        if (string.IsNullOrEmpty(requestedByRaw))
            return Task.FromResult(EngineResult.Fail("Missing requestedBy"));
        if (!Guid.TryParse(requestedByRaw, out var requestedBy) || requestedBy == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid requestedBy format"));

        // --- Validate FreezeReason ---
        var freezeReason = context.Data.GetValueOrDefault("freezeReason") as string;
        if (string.IsNullOrEmpty(freezeReason))
            return Task.FromResult(EngineResult.Fail("Missing freezeReason"));

        // --- Validate FreezeScope ---
        var freezeScope = context.Data.GetValueOrDefault("freezeScope") as string;
        if (string.IsNullOrEmpty(freezeScope))
            return Task.FromResult(EngineResult.Fail("Missing freezeScope"));
        if (!Array.Exists(ValidFreezeScopes, s => s == freezeScope))
            return Task.FromResult(EngineResult.Fail($"Invalid freezeScope: {freezeScope}. Valid: {string.Join(", ", ValidFreezeScopes)}"));

        // --- Optional: restricted operation type (for OperationType scope) ---
        var restrictedOperationType = context.Data.GetValueOrDefault("restrictedOperationType") as string ?? "";
        if (freezeScope == "OperationType" && string.IsNullOrEmpty(restrictedOperationType))
            return Task.FromResult(EngineResult.Fail("restrictedOperationType is required when freezeScope is OperationType"));

        var evaluatedAt = DateTime.UtcNow;

        // --- Emit events ---
        var events = new[]
        {
            EngineEvent.Create("VaultFreezeRequested", vaultId,
                new Dictionary<string, object>
                {
                    ["vaultId"] = vaultIdRaw,
                    ["vaultAccountId"] = vaultAccountIdRaw,
                    ["requestedBy"] = requestedByRaw,
                    ["freezeReason"] = freezeReason,
                    ["freezeScope"] = freezeScope,
                    ["restrictedOperationType"] = restrictedOperationType,
                    ["topic"] = "whyce.economic.events"
                }),
            EngineEvent.Create("VaultFreezeApplied", vaultId,
                new Dictionary<string, object>
                {
                    ["vaultId"] = vaultIdRaw,
                    ["vaultAccountId"] = vaultAccountIdRaw,
                    ["isFrozen"] = true,
                    ["freezeScope"] = freezeScope,
                    ["freezeReason"] = freezeReason,
                    ["restrictedOperationType"] = restrictedOperationType,
                    ["evaluatedAt"] = evaluatedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["vaultId"] = vaultIdRaw,
            ["vaultAccountId"] = vaultAccountIdRaw,
            ["isFrozen"] = true,
            ["freezeScope"] = freezeScope,
            ["freezeReason"] = freezeReason,
            ["restrictedOperationType"] = restrictedOperationType,
            ["evaluatedAt"] = evaluatedAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private Task<EngineResult> ExecuteUnfreeze(EngineContext context)
    {
        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate VaultAccountId ---
        var vaultAccountIdRaw = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultAccountId"));
        if (!Guid.TryParse(vaultAccountIdRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid vaultAccountId format"));

        // --- Validate RequestedBy ---
        var requestedByRaw = context.Data.GetValueOrDefault("requestedBy") as string;
        if (string.IsNullOrEmpty(requestedByRaw))
            return Task.FromResult(EngineResult.Fail("Missing requestedBy"));
        if (!Guid.TryParse(requestedByRaw, out var requestedBy) || requestedBy == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid requestedBy format"));

        // --- Validate UnfreezeReason ---
        var unfreezeReason = context.Data.GetValueOrDefault("unfreezeReason") as string;
        if (string.IsNullOrEmpty(unfreezeReason))
            return Task.FromResult(EngineResult.Fail("Missing unfreezeReason"));

        var evaluatedAt = DateTime.UtcNow;

        var events = new[]
        {
            EngineEvent.Create("VaultFreezeReleased", vaultId,
                new Dictionary<string, object>
                {
                    ["vaultId"] = vaultIdRaw,
                    ["vaultAccountId"] = vaultAccountIdRaw,
                    ["requestedBy"] = requestedByRaw,
                    ["unfreezeReason"] = unfreezeReason,
                    ["isFrozen"] = false,
                    ["evaluatedAt"] = evaluatedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["vaultId"] = vaultIdRaw,
            ["vaultAccountId"] = vaultAccountIdRaw,
            ["isFrozen"] = false,
            ["freezeScope"] = "",
            ["freezeReason"] = "",
            ["unfreezeReason"] = unfreezeReason,
            ["evaluatedAt"] = evaluatedAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private Task<EngineResult> ValidateFreezeStatus(EngineContext context)
    {
        // --- Validate VaultId ---
        var vaultIdRaw = context.Data.GetValueOrDefault("vaultId") as string;
        if (string.IsNullOrEmpty(vaultIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultId"));
        if (!Guid.TryParse(vaultIdRaw, out var vaultId) || vaultId == Guid.Empty)
            return Task.FromResult(EngineResult.Fail("Invalid vaultId format"));

        // --- Validate VaultAccountId ---
        var vaultAccountIdRaw = context.Data.GetValueOrDefault("vaultAccountId") as string;
        if (string.IsNullOrEmpty(vaultAccountIdRaw))
            return Task.FromResult(EngineResult.Fail("Missing vaultAccountId"));
        if (!Guid.TryParse(vaultAccountIdRaw, out _))
            return Task.FromResult(EngineResult.Fail("Invalid vaultAccountId format"));

        // --- Validate OperationType ---
        var operationType = context.Data.GetValueOrDefault("operationType") as string;
        if (string.IsNullOrEmpty(operationType))
            return Task.FromResult(EngineResult.Fail("Missing operationType"));

        // --- Read freeze state from context ---
        var isVaultFrozen = ResolveBool(context.Data.GetValueOrDefault("isVaultFrozen"));
        var isAccountFrozen = ResolveBool(context.Data.GetValueOrDefault("isAccountFrozen"));
        var frozenOperationTypes = context.Data.GetValueOrDefault("frozenOperationTypes") as IReadOnlyList<object>;
        var freezeScope = context.Data.GetValueOrDefault("freezeScope") as string ?? "";
        var freezeReason = context.Data.GetValueOrDefault("freezeReason") as string ?? "";

        var evaluatedAt = DateTime.UtcNow;
        bool operationBlocked;
        string validationReason;

        // --- Check vault-level freeze ---
        if (isVaultFrozen)
        {
            operationBlocked = true;
            validationReason = $"Vault is frozen at vault level. Operation '{operationType}' is blocked.";
        }
        // --- Check account-level freeze ---
        else if (isAccountFrozen)
        {
            operationBlocked = true;
            validationReason = $"Vault account is frozen. Operation '{operationType}' is blocked.";
        }
        // --- Check operation-type freeze ---
        else if (frozenOperationTypes is not null && frozenOperationTypes.Any(t => t as string == operationType))
        {
            operationBlocked = true;
            validationReason = $"Operation type '{operationType}' is frozen for this vault.";
        }
        else
        {
            operationBlocked = false;
            validationReason = $"Operation '{operationType}' is permitted. No freeze restrictions apply.";
        }

        var eventType = operationBlocked
            ? "VaultFreezeValidationFailed"
            : "VaultFreezeValidationPassed";

        var events = new[]
        {
            EngineEvent.Create(eventType, vaultId,
                new Dictionary<string, object>
                {
                    ["vaultId"] = vaultIdRaw,
                    ["vaultAccountId"] = vaultAccountIdRaw,
                    ["operationType"] = operationType,
                    ["isOperationBlocked"] = operationBlocked,
                    ["freezeScope"] = freezeScope,
                    ["freezeReason"] = freezeReason,
                    ["validationReason"] = validationReason,
                    ["evaluatedAt"] = evaluatedAt.ToString("O"),
                    ["topic"] = "whyce.economic.events"
                })
        };

        var output = new Dictionary<string, object>
        {
            ["vaultId"] = vaultIdRaw,
            ["vaultAccountId"] = vaultAccountIdRaw,
            ["operationType"] = operationType,
            ["isFrozen"] = operationBlocked,
            ["freezeScope"] = freezeScope,
            ["freezeReason"] = freezeReason,
            ["validationReason"] = validationReason,
            ["evaluatedAt"] = evaluatedAt.ToString("O")
        };

        return Task.FromResult(EngineResult.Ok(events, output));
    }

    private static bool ResolveBool(object? value)
    {
        return value switch
        {
            bool b => b,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
