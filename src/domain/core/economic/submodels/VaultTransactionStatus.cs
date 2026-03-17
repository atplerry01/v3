namespace Whycespace.Domain.Core.Vault;

public enum VaultTransactionStatus
{
    Pending,
    Authorized,
    Processing,
    Completed,
    Failed,
    Cancelled
}
