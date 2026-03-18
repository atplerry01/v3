using Whycespace.Domain.Core.Economic;

namespace Whycespace.Engines.T3I.Atlas.Economic.Models;

public sealed record CapitalBalanceInput(
    ComputeCapitalBalanceCommand Command,
    IReadOnlyList<CapitalRecord> CapitalRecords);
