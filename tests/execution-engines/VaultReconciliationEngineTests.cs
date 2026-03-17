namespace Whycespace.Tests.ExecutionEngines;

using Whycespace.Engines.T2E.Economic.Vault.Engines;
using Whycespace.Contracts.Engines;
using Xunit;

public sealed class VaultReconciliationEngineTests
{
    private readonly VaultReconciliationEngine _engine = new();

    // --- Reconciliation Success Tests ---

    [Fact]
    public async Task ReconciliationSuccess_BalancedLedger_ReturnsPass()
    {
        var vaultId = Guid.NewGuid().ToString();
        var txnId1 = Guid.NewGuid().ToString();
        var txnId2 = Guid.NewGuid().ToString();

        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = vaultId,
            ["reconciliationScope"] = "full",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["ledgerBalance"] = 700m,
            ["ledgerEntries"] = new List<object>
            {
                new Dictionary<string, object> { ["entryType"] = "Credit", ["amount"] = 1000m, ["transactionId"] = txnId1 },
                new Dictionary<string, object> { ["entryType"] = "Debit", ["amount"] = 300m, ["transactionId"] = txnId2 }
            },
            ["transactions"] = new List<object>
            {
                new Dictionary<string, object> { ["transactionId"] = txnId1, ["transactionType"] = "Contribution" },
                new Dictionary<string, object> { ["transactionId"] = txnId2, ["transactionType"] = "Withdrawal" }
            }
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isBalanced"]);
        Assert.Equal("Passed", result.Output["reconciliationStatus"]);
        Assert.Equal(1000m, result.Output["totalCredits"]);
        Assert.Equal(300m, result.Output["totalDebits"]);
        Assert.Equal(700m, result.Output["computedBalance"]);
    }

    // --- Debit/Credit Balance Tests ---

    [Fact]
    public async Task DebitCreditBalance_CorrectTotals()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "ledger",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["ledgerBalance"] = 500m,
            ["ledgerEntries"] = new List<object>
            {
                new Dictionary<string, object> { ["entryType"] = "Credit", ["amount"] = 1000m, ["transactionId"] = Guid.NewGuid().ToString() },
                new Dictionary<string, object> { ["entryType"] = "Credit", ["amount"] = 250m, ["transactionId"] = Guid.NewGuid().ToString() },
                new Dictionary<string, object> { ["entryType"] = "Debit", ["amount"] = 500m, ["transactionId"] = Guid.NewGuid().ToString() },
                new Dictionary<string, object> { ["entryType"] = "Debit", ["amount"] = 250m, ["transactionId"] = Guid.NewGuid().ToString() }
            }
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(true, result.Output["isBalanced"]);
        Assert.Equal(1250m, result.Output["totalCredits"]);
        Assert.Equal(750m, result.Output["totalDebits"]);
        Assert.Equal(500m, result.Output["computedBalance"]);
    }

    // --- Missing Ledger Entry Tests ---

    [Fact]
    public async Task MissingLedgerEntry_DetectsAnomaly()
    {
        var txnId1 = Guid.NewGuid().ToString();
        var txnId2 = Guid.NewGuid().ToString();

        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "full",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["ledgerBalance"] = 1000m,
            ["ledgerEntries"] = new List<object>
            {
                new Dictionary<string, object> { ["entryType"] = "Credit", ["amount"] = 1000m, ["transactionId"] = txnId1 }
            },
            ["transactions"] = new List<object>
            {
                new Dictionary<string, object> { ["transactionId"] = txnId1, ["transactionType"] = "Contribution" },
                new Dictionary<string, object> { ["transactionId"] = txnId2, ["transactionType"] = "Transfer" }
            }
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isBalanced"]);
        Assert.Equal("Failed", result.Output["reconciliationStatus"]);
        Assert.Contains("Missing ledger entry for transaction", result.Output["reconciliationNotes"].ToString()!);
    }

    // --- Duplicate Entry Tests ---

    [Fact]
    public async Task DuplicateEntry_DetectsAnomaly()
    {
        var txnId = Guid.NewGuid().ToString();

        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "ledger",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["ledgerBalance"] = 2000m,
            ["ledgerEntries"] = new List<object>
            {
                new Dictionary<string, object> { ["entryType"] = "Credit", ["amount"] = 1000m, ["transactionId"] = txnId },
                new Dictionary<string, object> { ["entryType"] = "Credit", ["amount"] = 1000m, ["transactionId"] = txnId }
            }
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isBalanced"]);
        Assert.Equal("Failed", result.Output["reconciliationStatus"]);
        Assert.Contains("Duplicate ledger entry for transaction", result.Output["reconciliationNotes"].ToString()!);
    }

    // --- Reconciliation Failure Tests ---

    [Fact]
    public async Task ReconciliationFailure_BalanceMismatch()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "ledger",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["ledgerBalance"] = 9999m,
            ["ledgerEntries"] = new List<object>
            {
                new Dictionary<string, object> { ["entryType"] = "Credit", ["amount"] = 500m, ["transactionId"] = Guid.NewGuid().ToString() }
            }
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(false, result.Output["isBalanced"]);
        Assert.Equal("Failed", result.Output["reconciliationStatus"]);
        Assert.Contains("Balance mismatch", result.Output["reconciliationNotes"].ToString()!);
    }

    // --- Event Tests ---

    [Fact]
    public async Task Success_EmitsStartedAndCompletedEvents()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "ledger",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["ledgerBalance"] = 0m,
            ["ledgerEntries"] = new List<object>()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultReconciliationStarted");
        Assert.Contains(result.Events, e => e.EventType == "VaultReconciliationCompleted");
    }

    [Fact]
    public async Task Failure_EmitsStartedAndFailedEvents()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "ledger",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["ledgerBalance"] = 9999m,
            ["ledgerEntries"] = new List<object>
            {
                new Dictionary<string, object> { ["entryType"] = "Credit", ["amount"] = 100m, ["transactionId"] = Guid.NewGuid().ToString() }
            }
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Contains(result.Events, e => e.EventType == "VaultReconciliationStarted");
        Assert.Contains(result.Events, e => e.EventType == "VaultReconciliationFailed");
    }

    [Fact]
    public async Task Anomaly_EmitsAnomalyDetectedEvent()
    {
        var txnId = Guid.NewGuid().ToString();

        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "ledger",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["ledgerBalance"] = 2000m,
            ["ledgerEntries"] = new List<object>
            {
                new Dictionary<string, object> { ["entryType"] = "Credit", ["amount"] = 1000m, ["transactionId"] = txnId },
                new Dictionary<string, object> { ["entryType"] = "Credit", ["amount"] = 1000m, ["transactionId"] = txnId }
            }
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.Contains(result.Events, e => e.EventType == "VaultReconciliationAnomalyDetected");
    }

    [Fact]
    public async Task Events_ContainTopicField()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "ledger",
            ["requestedBy"] = Guid.NewGuid().ToString(),
            ["ledgerBalance"] = 0m,
            ["ledgerEntries"] = new List<object>()
        });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        foreach (var evt in result.Events)
        {
            Assert.Equal("whyce.economic.events", evt.Payload["topic"]);
        }
    }

    // --- Input Validation Tests ---

    [Fact]
    public async Task MissingReconciliationId_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "ledger",
            ["requestedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingVaultId_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "ledger",
            ["requestedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingReconciliationScope_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["requestedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task InvalidReconciliationScope_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "invalid",
            ["requestedBy"] = Guid.NewGuid().ToString()
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingRequestedBy_Fails()
    {
        var context = CreateContext(new Dictionary<string, object>
        {
            ["reconciliationId"] = Guid.NewGuid().ToString(),
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["reconciliationScope"] = "ledger"
        });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    // --- Helper methods ---

    private static EngineContext CreateContext(Dictionary<string, object> data)
    {
        return new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "ExecuteVaultReconciliation",
            "partition-1", data);
    }
}
