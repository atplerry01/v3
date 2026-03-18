namespace Whycespace.Domain.Economic.Vault;

public sealed class VaultAggregate
{
    private readonly List<VaultParticipant> _participants = new();
    private readonly List<VaultTransactionReference> _transactionHistory = new();

    public VaultId Id { get; }
    public string VaultName { get; }
    public VaultPurposeType Purpose { get; }
    public VaultStatus Status { get; private set; }
    public VaultBalance Balance { get; private set; }
    public IReadOnlyList<VaultParticipant> Participants => _participants.AsReadOnly();
    public IReadOnlyList<VaultTransactionReference> TransactionHistory => _transactionHistory.AsReadOnly();
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private VaultAggregate(
        VaultId id,
        string vaultName,
        VaultPurposeType purpose,
        string currency,
        VaultParticipant owner)
    {
        Id = id;
        VaultName = vaultName;
        Purpose = purpose;
        Status = VaultStatus.Active;
        Balance = new VaultBalance(0m, currency);
        _participants.Add(owner);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static VaultAggregate Create(
        string vaultName,
        VaultPurposeType purpose,
        string currency,
        Guid ownerId)
    {
        if (string.IsNullOrWhiteSpace(vaultName))
            throw new InvalidOperationException("Vault name must be specified.");

        if (string.IsNullOrWhiteSpace(currency))
            throw new InvalidOperationException("Currency must be specified.");

        if (ownerId == Guid.Empty)
            throw new InvalidOperationException("Owner identity must be specified.");

        var id = VaultId.New();
        var owner = new VaultParticipant(id, ownerId, VaultParticipantRole.Owner);
        return new VaultAggregate(id, vaultName, purpose, currency, owner);
    }

    public void AddParticipant(Guid identityId, VaultParticipantRole role, decimal ownershipPercentage = 0m)
    {
        if (_participants.Any(p => p.IdentityId == identityId && p.IsActiveParticipant()))
            throw new InvalidOperationException("Participant already exists in this vault.");

        _participants.Add(new VaultParticipant(Id, identityId, role, ownershipPercentage));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SuspendParticipant(Guid identityId)
    {
        var participant = _participants.FirstOrDefault(p => p.IdentityId == identityId && p.IsActiveParticipant());
        if (participant is null)
            throw new InvalidOperationException("Participant not found in this vault.");

        participant.SuspendParticipant();
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RemoveParticipant(Guid identityId)
    {
        var participant = _participants.FirstOrDefault(p => p.IdentityId == identityId && p.IsActiveParticipant());
        if (participant is null)
            throw new InvalidOperationException("Participant not found in this vault.");

        bool isLastOwner = participant.IsOwner()
            && _participants.Count(p => p.IsOwner() && p.IsActiveParticipant()) <= 1;

        participant.RemoveParticipant(isLastOwner);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void FreezeVault()
    {
        if (Status == VaultStatus.Closed)
            throw new InvalidOperationException("Closed vault cannot be frozen.");

        if (Status == VaultStatus.Frozen)
            throw new InvalidOperationException("Vault is already frozen.");

        Status = VaultStatus.Frozen;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UnfreezeVault()
    {
        if (Status != VaultStatus.Frozen)
            throw new InvalidOperationException("Only frozen vaults can be unfrozen.");

        Status = VaultStatus.Active;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CloseVault()
    {
        if (Status == VaultStatus.Closed)
            throw new InvalidOperationException("Vault is already closed.");

        if (Balance.Amount != 0m)
            throw new InvalidOperationException("Vault balance must be zero before closing.");

        Status = VaultStatus.Closed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateBalance(decimal amount)
    {
        if (Status == VaultStatus.Closed)
            throw new InvalidOperationException("Closed vault cannot process transactions.");

        if (Status == VaultStatus.Frozen && amount < Balance.Amount)
            throw new InvalidOperationException("Frozen vault cannot process withdrawals.");

        if (amount < 0)
            throw new InvalidOperationException("Vault balance cannot be negative.");

        Balance = Balance.WithAmount(amount);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AddTransactionReference(Guid transactionId, string transactionType)
    {
        if (Status == VaultStatus.Closed)
            throw new InvalidOperationException("Closed vault cannot record transactions.");

        _transactionHistory.Add(new VaultTransactionReference(
            transactionId,
            DateTimeOffset.UtcNow,
            transactionType));
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
