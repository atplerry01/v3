namespace Whycespace.Domain.Economic.Vault;

public enum VaultTransactionStatus
{
    Pending,
    Authorized,
    Processing,
    Completed,
    Failed,
    Cancelled
}
