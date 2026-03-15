namespace Whycespace.ExecutionEngines.Tests;

using Whycespace.Engines.T2E.Economic.Vault;
using Whycespace.Contracts.Engines;

public sealed class VaultFreezeEngineTests
{
    private readonly VaultFreezeEngine _engine = new();

    // --- Vault Freeze Tests ---

    [Fact]
    public async Task VaultFreeze_BlocksOperations()
    {
        var vaultId = Guid.NewGuid().ToString();
        var accountId = Guid.NewGuid().ToString();

        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteVaultFreeze",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = vaultId,
                ["vaultAccountId"] = accountId,
                ["requestedBy"] = Guid.NewGuid().ToString(),
                ["freezeReason"] = "Suspected fraud detected",
                ["freezeScope"] = "Vault"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isFrozen"]);
        Assert.Equal("Vault", result.Output["freezeScope"]);
        Assert.Contains(result.Events, e => e.EventType == "VaultFreezeRequested");
        Assert.Contains(result.Events, e => e.EventType == "VaultFreezeApplied");
    }

    // --- Account Freeze Tests ---

    [Fact]
    public async Task AccountFreeze_BlocksAccountOperations()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteVaultFreeze",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["requestedBy"] = Guid.NewGuid().ToString(),
                ["freezeReason"] = "Governance intervention",
                ["freezeScope"] = "VaultAccount"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isFrozen"]);
        Assert.Equal("VaultAccount", result.Output["freezeScope"]);
    }

    // --- Operation Type Freeze Tests ---

    [Fact]
    public async Task OperationFreeze_BlocksSpecificOperationType()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteVaultFreeze",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["requestedBy"] = Guid.NewGuid().ToString(),
                ["freezeReason"] = "Withdrawal freeze due to risk escalation",
                ["freezeScope"] = "OperationType",
                ["restrictedOperationType"] = "Withdrawal"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isFrozen"]);
        Assert.Equal("OperationType", result.Output["freezeScope"]);
        Assert.Equal("Withdrawal", result.Output["restrictedOperationType"]);
    }

    [Fact]
    public async Task OperationFreeze_MissingRestrictedOperationType_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteVaultFreeze",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["requestedBy"] = Guid.NewGuid().ToString(),
                ["freezeReason"] = "Test",
                ["freezeScope"] = "OperationType"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    // --- Unfreeze Tests ---

    [Fact]
    public async Task Unfreeze_RestoresOperationCapability()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteVaultUnfreeze",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["requestedBy"] = Guid.NewGuid().ToString(),
                ["unfreezeReason"] = "Investigation cleared, no fraud found"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isFrozen"]);
        Assert.Contains(result.Events, e => e.EventType == "VaultFreezeReleased");
    }

    // --- Freeze Validation Tests ---

    [Fact]
    public async Task FreezeValidation_VaultFrozen_BlocksOperation()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ValidateFreezeStatus",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Withdrawal",
                ["isVaultFrozen"] = true,
                ["freezeScope"] = "Vault",
                ["freezeReason"] = "Security incident"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isFrozen"]);
        Assert.Contains(result.Events, e => e.EventType == "VaultFreezeValidationFailed");
    }

    [Fact]
    public async Task FreezeValidation_AccountFrozen_BlocksOperation()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ValidateFreezeStatus",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Transfer",
                ["isVaultFrozen"] = false,
                ["isAccountFrozen"] = true,
                ["freezeScope"] = "VaultAccount",
                ["freezeReason"] = "Compliance hold"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isFrozen"]);
        Assert.Contains(result.Events, e => e.EventType == "VaultFreezeValidationFailed");
    }

    [Fact]
    public async Task FreezeValidation_OperationTypeFrozen_BlocksMatchingOperation()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ValidateFreezeStatus",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Withdrawal",
                ["isVaultFrozen"] = false,
                ["isAccountFrozen"] = false,
                ["frozenOperationTypes"] = new List<object> { "Withdrawal", "Distribution" },
                ["freezeScope"] = "OperationType",
                ["freezeReason"] = "Withdrawal freeze"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isFrozen"]);
        Assert.Contains(result.Events, e => e.EventType == "VaultFreezeValidationFailed");
    }

    [Fact]
    public async Task FreezeValidation_NotFrozen_AllowsOperation()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ValidateFreezeStatus",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Contribution",
                ["isVaultFrozen"] = false,
                ["isAccountFrozen"] = false
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isFrozen"]);
        Assert.Contains(result.Events, e => e.EventType == "VaultFreezeValidationPassed");
    }

    // --- Input Validation Tests ---

    [Fact]
    public async Task Freeze_MissingVaultId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteVaultFreeze",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["requestedBy"] = Guid.NewGuid().ToString(),
                ["freezeReason"] = "Test",
                ["freezeScope"] = "Vault"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task Freeze_InvalidFreezeScope_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteVaultFreeze",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["requestedBy"] = Guid.NewGuid().ToString(),
                ["freezeReason"] = "Test",
                ["freezeScope"] = "InvalidScope"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task UnknownCommand_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "UnknownCommand",
            "partition-1", new Dictionary<string, object>());

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }
}
