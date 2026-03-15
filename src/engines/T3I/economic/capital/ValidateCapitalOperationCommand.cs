namespace Whycespace.Engines.T3I.Economic.Capital;

public enum CapitalOperationType
{
    Commitment,
    Contribution,
    Reservation,
    Allocation,
    Utilization,
    Distribution
}

public sealed record ValidateCapitalOperationCommand(
    CapitalOperationType OperationType,
    Guid PoolId,
    Guid? ReservationId,
    Guid? AllocationId,
    Guid InvestorIdentityId,
    decimal Amount,
    string Currency,
    Guid RequestedBy,
    DateTime RequestedAt);