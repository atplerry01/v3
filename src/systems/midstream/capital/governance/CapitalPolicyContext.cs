namespace Whycespace.Systems.Midstream.Capital.Governance;

public sealed record CapitalPolicyContext(
    CapitalOperationType OperationType,
    Guid PoolId,
    Guid InvestorIdentityId,
    Guid ReservationId,
    Guid AllocationId,
    decimal Amount,
    string Currency,
    Guid ClusterId,
    Guid SubClusterId,
    Guid SPVId,
    Guid RequestedBy,
    DateTime RequestedAt
);

public enum CapitalOperationType
{
    Commitment,
    Contribution,
    Reservation,
    Allocation,
    Utilization,
    Distribution
}
