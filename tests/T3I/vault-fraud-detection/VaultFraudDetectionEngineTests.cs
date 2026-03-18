namespace Whycespace.VaultFraudDetection.Tests;

using Whycespace.Engines.T3I.Monitoring.Economic.Engines;
using Whycespace.Engines.T3I.Monitoring.Economic.Models;
using Whycespace.Contracts.Engines;

public sealed class VaultFraudDetectionEngineTests
{
    private readonly VaultFraudDetectionEngine _engine = new();

    [Fact]
    public async Task LowFraudScore_NormalBehavior_ReturnsLowRisk()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Contribution",
                ["amount"] = 500m,
                ["currency"] = "GBP",
                ["recentTransactionCount"] = 3,
                ["recentTransactionWindowMinutes"] = 60,
                ["averageTransactionAmount"] = 400m,
                ["recentFailedOperationCount"] = 0,
                ["recentLargeWithdrawalCount"] = 0,
                ["accountAgeDays"] = 365,
                ["isNewIdentity"] = false
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var fraudScore = (double)result.Output["fraudScore"];
        Assert.InRange(fraudScore, 0.0, 25.0);
        Assert.Equal("Low", result.Output["fraudRiskLevel"]);
        Assert.False((bool)result.Output["fraudAlertTriggered"]);
    }

    [Fact]
    public async Task SuspiciousTransaction_ElevatedVelocityAndAmount_ReturnsSuspicious()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Withdrawal",
                ["amount"] = 25_000m,
                ["currency"] = "GBP",
                ["recentTransactionCount"] = 25,
                ["recentTransactionWindowMinutes"] = 60,
                ["averageTransactionAmount"] = 2_000m,
                ["recentFailedOperationCount"] = 5,
                ["recentLargeWithdrawalCount"] = 3,
                ["accountAgeDays"] = 180,
                ["isNewIdentity"] = false
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var fraudScore = (double)result.Output["fraudScore"];
        Assert.InRange(fraudScore, 26.0, 60.0);
        Assert.Equal("Suspicious", result.Output["fraudRiskLevel"]);
    }

    [Fact]
    public async Task HighFraudScore_ExtremeAnomalies_ReturnsHighFraudRisk()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Withdrawal",
                ["amount"] = 500_000m,
                ["currency"] = "GBP",
                ["recentTransactionCount"] = 50,
                ["recentTransactionWindowMinutes"] = 30,
                ["averageTransactionAmount"] = 1_000m,
                ["recentFailedOperationCount"] = 15,
                ["recentLargeWithdrawalCount"] = 10,
                ["accountAgeDays"] = 2,
                ["isNewIdentity"] = true
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var fraudScore = (double)result.Output["fraudScore"];
        Assert.InRange(fraudScore, 61.0, 100.0);
        Assert.Equal("HighFraudRisk", result.Output["fraudRiskLevel"]);
    }

    [Fact]
    public async Task FraudAlertTrigger_WhenThresholdExceeded_EmitsAlertEvent()
    {
        var vaultId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = vaultId.ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Withdrawal",
                ["amount"] = 500_000m,
                ["currency"] = "GBP",
                ["recentTransactionCount"] = 50,
                ["recentTransactionWindowMinutes"] = 30,
                ["averageTransactionAmount"] = 1_000m,
                ["recentFailedOperationCount"] = 15,
                ["recentLargeWithdrawalCount"] = 10,
                ["accountAgeDays"] = 2,
                ["isNewIdentity"] = true
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.True((bool)result.Output["fraudAlertTriggered"]);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("VaultFraudEvaluationCompleted", result.Events[0].EventType);
        Assert.Equal("VaultFraudAlertTriggered", result.Events[1].EventType);
        Assert.Equal(vaultId, result.Events[0].AggregateId);
        Assert.Equal(vaultId, result.Events[1].AggregateId);
    }

    [Fact]
    public async Task FraudScore_Deterministic_SameInputsSameOutput()
    {
        var data = new Dictionary<string, object>
        {
            ["vaultId"] = Guid.NewGuid().ToString(),
            ["vaultAccountId"] = Guid.NewGuid().ToString(),
            ["transactionId"] = Guid.NewGuid().ToString(),
            ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
            ["operationType"] = "Transfer",
            ["amount"] = 25_000m,
            ["currency"] = "USD",
            ["recentTransactionCount"] = 12,
            ["recentTransactionWindowMinutes"] = 60,
            ["averageTransactionAmount"] = 5_000m,
            ["recentFailedOperationCount"] = 4,
            ["recentLargeWithdrawalCount"] = 2,
            ["accountAgeDays"] = 90,
            ["isNewIdentity"] = false
        };

        var context1 = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", data);

        var context2 = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", data);

        var result1 = await _engine.ExecuteAsync(context1);
        var result2 = await _engine.ExecuteAsync(context2);

        Assert.Equal(
            (double)result1.Output["fraudScore"],
            (double)result2.Output["fraudScore"]);
        Assert.Equal(
            result1.Output["fraudRiskLevel"],
            result2.Output["fraudRiskLevel"]);
        Assert.Equal(
            result1.Output["fraudAlertTriggered"],
            result2.Output["fraudAlertTriggered"]);
    }

    [Fact]
    public async Task NoFraudSignals_ReturnsNoFraudDetectedReason()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Contribution",
                ["amount"] = 100m,
                ["currency"] = "GBP",
                ["recentTransactionCount"] = 1,
                ["averageTransactionAmount"] = 100m,
                ["accountAgeDays"] = 500
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(0.0, (double)result.Output["fraudScore"]);
        Assert.Equal("No fraud signals detected", result.Output["fraudReason"]);
        Assert.Single(result.Events);
    }

    [Fact]
    public async Task MissingVaultId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", new Dictionary<string, object>
            {
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Withdrawal",
                ["amount"] = 100m,
                ["currency"] = "GBP"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingTransactionId_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Withdrawal",
                ["amount"] = 100m,
                ["currency"] = "GBP"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task MissingAmount_Fails()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Withdrawal",
                ["currency"] = "GBP"
            });

        var result = await _engine.ExecuteAsync(context);
        Assert.False(result.Success);
    }

    [Fact]
    public async Task FraudScore_ClampedWithinBounds()
    {
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = Guid.NewGuid().ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Withdrawal",
                ["amount"] = 99_999_999m,
                ["currency"] = "GBP",
                ["recentTransactionCount"] = 9999,
                ["recentTransactionWindowMinutes"] = 1,
                ["averageTransactionAmount"] = 1m,
                ["recentFailedOperationCount"] = 9999,
                ["recentLargeWithdrawalCount"] = 9999,
                ["accountAgeDays"] = 0,
                ["isNewIdentity"] = true
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        var fraudScore = (double)result.Output["fraudScore"];
        Assert.InRange(fraudScore, 0.0, 100.0);
    }

    [Fact]
    public async Task ConcurrentEvaluations_AllSucceed()
    {
        var tasks = Enumerable.Range(0, 100).Select(_ =>
        {
            var context = new EngineContext(
                Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
                "partition-1", new Dictionary<string, object>
                {
                    ["vaultId"] = Guid.NewGuid().ToString(),
                    ["vaultAccountId"] = Guid.NewGuid().ToString(),
                    ["transactionId"] = Guid.NewGuid().ToString(),
                    ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                    ["operationType"] = "Contribution",
                    ["amount"] = 1_000m,
                    ["currency"] = "GBP",
                    ["recentTransactionCount"] = 5,
                    ["averageTransactionAmount"] = 800m,
                    ["accountAgeDays"] = 180
                });

            return _engine.ExecuteAsync(context);
        });

        var results = await Task.WhenAll(tasks);

        Assert.All(results, r =>
        {
            Assert.True(r.Success);
            Assert.NotEmpty(r.Events);
            var score = (double)r.Output["fraudScore"];
            Assert.InRange(score, 0.0, 100.0);
        });
    }

    [Fact]
    public async Task EvaluationCompleted_EmitsEventWithCorrectAggregateId()
    {
        var vaultId = Guid.NewGuid();
        var context = new EngineContext(
            Guid.NewGuid(), Guid.NewGuid().ToString(), "EvaluateVaultFraud",
            "partition-1", new Dictionary<string, object>
            {
                ["vaultId"] = vaultId.ToString(),
                ["vaultAccountId"] = Guid.NewGuid().ToString(),
                ["transactionId"] = Guid.NewGuid().ToString(),
                ["initiatorIdentityId"] = Guid.NewGuid().ToString(),
                ["operationType"] = "Contribution",
                ["amount"] = 100m,
                ["currency"] = "GBP"
            });

        var result = await _engine.ExecuteAsync(context);

        Assert.True(result.Success);
        Assert.Equal(vaultId, result.Events[0].AggregateId);
        Assert.Equal("VaultFraudEvaluationCompleted", result.Events[0].EventType);
    }
}
