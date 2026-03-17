namespace Whycespace.Engines.T3I.Monitoring.Economic;

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