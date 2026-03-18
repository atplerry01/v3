using Whycespace.Engines.T3I.Reporting.Economic.Models;

namespace Whycespace.Engines.T3I.Atlas.Economic.Models;

public sealed record VaultBalanceAnalyticsInput(
    ExecuteVaultBalanceAnalyticsCommand Command,
    IReadOnlyList<LedgerEntry> LedgerEntries);
