namespace Whycespace.Tests.Domain.Vault;

using Whycespace.Domain.Core.Economic;
using Xunit;

public sealed class VaultTransactionTests
{
    private static VaultTransaction CreateValidTransaction(
        VaultTransactionType type = VaultTransactionType.Contribution,
        decimal amount = 100m)
    {
        return VaultTransaction.Create(
            vaultId: Guid.NewGuid(),
            vaultAccountId: Guid.NewGuid(),
            transactionType: type,
            amount: amount,
            currency: "GBP",
            initiatorIdentityId: Guid.NewGuid(),
            referenceId: Guid.NewGuid(),
            referenceType: "Invoice");
    }

    // --- TransactionCreationTest ---

    [Fact]
    public void Create_ValidParameters_CreatesTransaction()
    {
        var vaultId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var initiatorId = Guid.NewGuid();

        var tx = VaultTransaction.Create(
            vaultId, accountId, VaultTransactionType.Contribution,
            500m, "GBP", initiatorId, Guid.NewGuid(), "Invoice", "Test deposit");

        Assert.Equal(vaultId, tx.VaultId);
        Assert.Equal(accountId, tx.VaultAccountId);
        Assert.Equal(VaultTransactionType.Contribution, tx.TransactionType);
        Assert.Equal(VaultTransactionStatus.Pending, tx.Status);
        Assert.Equal(500m, tx.Amount);
        Assert.Equal("GBP", tx.Currency);
        Assert.Equal(initiatorId, tx.InitiatorIdentityId);
        Assert.Equal("Test deposit", tx.Description);
    }

    [Fact]
    public void Create_EmptyVaultId_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            VaultTransaction.Create(Guid.Empty, Guid.NewGuid(), VaultTransactionType.Contribution,
                100m, "GBP", Guid.NewGuid(), Guid.NewGuid(), "Invoice"));
    }

    [Fact]
    public void Create_EmptyVaultAccountId_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            VaultTransaction.Create(Guid.NewGuid(), Guid.Empty, VaultTransactionType.Contribution,
                100m, "GBP", Guid.NewGuid(), Guid.NewGuid(), "Invoice"));
    }

    [Fact]
    public void Create_ZeroAmount_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            VaultTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), VaultTransactionType.Contribution,
                0m, "GBP", Guid.NewGuid(), Guid.NewGuid(), "Invoice"));
    }

    [Fact]
    public void Create_NegativeAmount_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            VaultTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), VaultTransactionType.Contribution,
                -50m, "GBP", Guid.NewGuid(), Guid.NewGuid(), "Invoice"));
    }

    [Fact]
    public void Create_EmptyCurrency_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            VaultTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), VaultTransactionType.Contribution,
                100m, "", Guid.NewGuid(), Guid.NewGuid(), "Invoice"));
    }

    [Fact]
    public void Create_EmptyInitiatorId_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            VaultTransaction.Create(Guid.NewGuid(), Guid.NewGuid(), VaultTransactionType.Contribution,
                100m, "GBP", Guid.Empty, Guid.NewGuid(), "Invoice"));
    }

    // --- TransactionAuthorizationTest ---

    [Fact]
    public void AuthorizeTransaction_Pending_TransitionsToAuthorized()
    {
        var tx = CreateValidTransaction();

        tx.AuthorizeTransaction();

        Assert.Equal(VaultTransactionStatus.Authorized, tx.Status);
    }

    [Fact]
    public void AuthorizeTransaction_NotPending_Throws()
    {
        var tx = CreateValidTransaction();
        tx.AuthorizeTransaction();

        Assert.Throws<InvalidOperationException>(() => tx.AuthorizeTransaction());
    }

    // --- TransactionProcessingTest ---

    [Fact]
    public void StartProcessing_Authorized_TransitionsToProcessing()
    {
        var tx = CreateValidTransaction();
        tx.AuthorizeTransaction();

        tx.StartProcessing();

        Assert.Equal(VaultTransactionStatus.Processing, tx.Status);
    }

    [Fact]
    public void StartProcessing_NotAuthorized_Throws()
    {
        var tx = CreateValidTransaction();

        Assert.Throws<InvalidOperationException>(() => tx.StartProcessing());
    }

    // --- TransactionCompletionTest ---

    [Fact]
    public void CompleteTransaction_Processing_TransitionsToCompleted()
    {
        var tx = CreateValidTransaction();
        tx.AuthorizeTransaction();
        tx.StartProcessing();

        tx.CompleteTransaction();

        Assert.Equal(VaultTransactionStatus.Completed, tx.Status);
    }

    [Fact]
    public void CompleteTransaction_NotProcessing_Throws()
    {
        var tx = CreateValidTransaction();
        tx.AuthorizeTransaction();

        Assert.Throws<InvalidOperationException>(() => tx.CompleteTransaction());
    }

    // --- TransactionFailureTest ---

    [Fact]
    public void FailTransaction_Pending_TransitionsToFailed()
    {
        var tx = CreateValidTransaction();

        tx.FailTransaction();

        Assert.Equal(VaultTransactionStatus.Failed, tx.Status);
    }

    [Fact]
    public void FailTransaction_Authorized_TransitionsToFailed()
    {
        var tx = CreateValidTransaction();
        tx.AuthorizeTransaction();

        tx.FailTransaction();

        Assert.Equal(VaultTransactionStatus.Failed, tx.Status);
    }

    [Fact]
    public void FailTransaction_Processing_TransitionsToFailed()
    {
        var tx = CreateValidTransaction();
        tx.AuthorizeTransaction();
        tx.StartProcessing();

        tx.FailTransaction();

        Assert.Equal(VaultTransactionStatus.Failed, tx.Status);
    }

    [Fact]
    public void FailTransaction_Completed_Throws()
    {
        var tx = CreateValidTransaction();
        tx.AuthorizeTransaction();
        tx.StartProcessing();
        tx.CompleteTransaction();

        Assert.Throws<InvalidOperationException>(() => tx.FailTransaction());
    }

    [Fact]
    public void FailTransaction_Cancelled_Throws()
    {
        var tx = CreateValidTransaction();
        tx.CancelTransaction();

        Assert.Throws<InvalidOperationException>(() => tx.FailTransaction());
    }

    // --- TransactionCancellationTest ---

    [Fact]
    public void CancelTransaction_Pending_TransitionsToCancelled()
    {
        var tx = CreateValidTransaction();

        tx.CancelTransaction();

        Assert.Equal(VaultTransactionStatus.Cancelled, tx.Status);
    }

    [Fact]
    public void CancelTransaction_Authorized_TransitionsToCancelled()
    {
        var tx = CreateValidTransaction();
        tx.AuthorizeTransaction();

        tx.CancelTransaction();

        Assert.Equal(VaultTransactionStatus.Cancelled, tx.Status);
    }

    [Fact]
    public void CancelTransaction_Processing_Throws()
    {
        var tx = CreateValidTransaction();
        tx.AuthorizeTransaction();
        tx.StartProcessing();

        Assert.Throws<InvalidOperationException>(() => tx.CancelTransaction());
    }

    [Fact]
    public void CancelTransaction_Completed_Throws()
    {
        var tx = CreateValidTransaction();
        tx.AuthorizeTransaction();
        tx.StartProcessing();
        tx.CompleteTransaction();

        Assert.Throws<InvalidOperationException>(() => tx.CancelTransaction());
    }

    // --- VaultTransactionId Value Object ---

    [Fact]
    public void VaultTransactionId_IsStronglyTyped()
    {
        var id = VaultTransactionId.New();
        Guid guid = id;

        Assert.NotEqual(Guid.Empty, guid);
        Assert.Equal(id.Value, guid);
    }

    // --- Transaction Type Coverage ---

    [Theory]
    [InlineData(VaultTransactionType.Contribution)]
    [InlineData(VaultTransactionType.Transfer)]
    [InlineData(VaultTransactionType.Withdrawal)]
    [InlineData(VaultTransactionType.Distribution)]
    [InlineData(VaultTransactionType.Adjustment)]
    [InlineData(VaultTransactionType.Refund)]
    public void Create_AllTransactionTypes_Succeeds(VaultTransactionType type)
    {
        var tx = CreateValidTransaction(type: type);

        Assert.Equal(type, tx.TransactionType);
    }
}
