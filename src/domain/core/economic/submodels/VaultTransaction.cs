namespace Whycespace.Domain.Core.Vault;

public sealed class VaultTransaction
{
    public VaultTransactionId TransactionId { get; }
    public Guid VaultId { get; }
    public Guid VaultAccountId { get; }
    public VaultTransactionType TransactionType { get; }
    public VaultTransactionStatus Status { get; private set; }
    public decimal Amount { get; }
    public string Currency { get; }
    public Guid InitiatorIdentityId { get; }
    public Guid ReferenceId { get; }
    public string ReferenceType { get; }
    public string? Description { get; }
    public string? Metadata { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private VaultTransaction(
        VaultTransactionId transactionId,
        Guid vaultId,
        Guid vaultAccountId,
        VaultTransactionType transactionType,
        decimal amount,
        string currency,
        Guid initiatorIdentityId,
        Guid referenceId,
        string referenceType,
        string? description,
        string? metadata)
    {
        TransactionId = transactionId;
        VaultId = vaultId;
        VaultAccountId = vaultAccountId;
        TransactionType = transactionType;
        Status = VaultTransactionStatus.Pending;
        Amount = amount;
        Currency = currency;
        InitiatorIdentityId = initiatorIdentityId;
        ReferenceId = referenceId;
        ReferenceType = referenceType;
        Description = description;
        Metadata = metadata;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static VaultTransaction Create(
        Guid vaultId,
        Guid vaultAccountId,
        VaultTransactionType transactionType,
        decimal amount,
        string currency,
        Guid initiatorIdentityId,
        Guid referenceId,
        string referenceType,
        string? description = null,
        string? metadata = null)
    {
        if (vaultId == Guid.Empty)
            throw new InvalidOperationException("VaultId must be specified.");

        if (vaultAccountId == Guid.Empty)
            throw new InvalidOperationException("VaultAccountId must be specified.");

        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidOperationException("Currency must be specified.");

        if (initiatorIdentityId == Guid.Empty)
            throw new InvalidOperationException("InitiatorIdentityId must be specified.");

        return new VaultTransaction(
            VaultTransactionId.New(),
            vaultId,
            vaultAccountId,
            transactionType,
            amount,
            currency,
            initiatorIdentityId,
            referenceId,
            referenceType,
            description,
            metadata);
    }

    public void AuthorizeTransaction()
    {
        if (Status != VaultTransactionStatus.Pending)
            throw new InvalidOperationException("Only pending transactions can be authorized.");

        Status = VaultTransactionStatus.Authorized;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void StartProcessing()
    {
        if (Status != VaultTransactionStatus.Authorized)
            throw new InvalidOperationException("Only authorized transactions can start processing.");

        Status = VaultTransactionStatus.Processing;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CompleteTransaction()
    {
        if (Status != VaultTransactionStatus.Processing)
            throw new InvalidOperationException("Only processing transactions can be completed.");

        Status = VaultTransactionStatus.Completed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void FailTransaction()
    {
        if (Status == VaultTransactionStatus.Completed || Status == VaultTransactionStatus.Cancelled)
            throw new InvalidOperationException("Completed or cancelled transactions cannot be failed.");

        Status = VaultTransactionStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CancelTransaction()
    {
        if (Status != VaultTransactionStatus.Pending && Status != VaultTransactionStatus.Authorized)
            throw new InvalidOperationException("Only pending or authorized transactions can be cancelled.");

        Status = VaultTransactionStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
