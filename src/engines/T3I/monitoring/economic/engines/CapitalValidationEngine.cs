using Whycespace.Engines.T3I.Monitoring.Economic.Models;
using Whycespace.Engines.T3I.Shared;
namespace Whycespace.Engines.T3I.Monitoring.Economic.Engines;

public sealed class CapitalValidationEngine : IIntelligenceEngine<CapitalValidationInput, CapitalValidationResult>
{
    public string EngineName => "CapitalValidation";

    public IntelligenceResult<CapitalValidationResult> Execute(IntelligenceContext<CapitalValidationInput> context)
    {
        var startedAt = DateTimeOffset.UtcNow;
        var input = context.Input;
        var result = Validate(input.Command, input.Pool, input.Reservation, input.Allocation, input.Investor);
        return IntelligenceResult<CapitalValidationResult>.Ok(result, IntelligenceTrace.Create(EngineName, context.CorrelationId, startedAt));
    }

    private static readonly string[] SupportedCurrencies = ["GBP", "USD", "EUR", "NGN"];

    public CapitalValidationResult Validate(
        ValidateCapitalOperationCommand command,
        CapitalPoolSnapshot? pool,
        CapitalReservationSnapshot? reservation,
        CapitalAllocationSnapshot? allocation,
        InvestorSnapshot? investor)
    {
        var errors = new List<string>();

        ValidatePool(command, pool, errors);
        ValidateAmount(command, errors);
        ValidateCurrency(command, pool, errors);
        ValidateLifecycle(command, reservation, allocation, errors);
        ValidateInvestor(command, investor, errors);

        if (errors.Count > 0)
        {
            return CapitalValidationResult.Invalid(
                command.PoolId, command.Amount, command.Currency, errors);
        }

        return CapitalValidationResult.Valid(
            command.PoolId, command.Amount, command.Currency);
    }

    private static void ValidatePool(
        ValidateCapitalOperationCommand command,
        CapitalPoolSnapshot? pool,
        List<string> errors)
    {
        if (pool is null)
        {
            errors.Add("POOL_NOT_FOUND");
            return;
        }

        if (!pool.IsActive)
        {
            errors.Add("POOL_NOT_ACTIVE");
            return;
        }

        if (command.OperationType == CapitalOperationType.Reservation
            && command.Amount > pool.AvailableBalance)
        {
            errors.Add("INSUFFICIENT_POOL_CAPITAL");
        }
    }

    private static void ValidateAmount(
        ValidateCapitalOperationCommand command,
        List<string> errors)
    {
        if (command.Amount <= 0)
        {
            errors.Add("INVALID_AMOUNT");
        }
    }

    private static void ValidateCurrency(
        ValidateCapitalOperationCommand command,
        CapitalPoolSnapshot? pool,
        List<string> errors)
    {
        if (!Array.Exists(SupportedCurrencies, c => c == command.Currency))
        {
            errors.Add("UNSUPPORTED_CURRENCY");
            return;
        }

        if (pool is not null && pool.Currency != command.Currency)
        {
            errors.Add("CURRENCY_MISMATCH");
        }
    }

    private static void ValidateLifecycle(
        ValidateCapitalOperationCommand command,
        CapitalReservationSnapshot? reservation,
        CapitalAllocationSnapshot? allocation,
        List<string> errors)
    {
        if (command.OperationType == CapitalOperationType.Allocation)
        {
            if (reservation is null)
            {
                errors.Add("INVALID_RESERVATION");
                return;
            }

            if (!reservation.IsActive)
            {
                errors.Add("RESERVATION_NOT_ACTIVE");
            }

            if (command.Amount > reservation.RemainingAmount)
            {
                errors.Add("INSUFFICIENT_POOL_CAPITAL");
            }
        }

        if (command.OperationType == CapitalOperationType.Utilization)
        {
            if (allocation is null)
            {
                errors.Add("INVALID_ALLOCATION_REFERENCE");
                return;
            }

            if (!allocation.IsActive)
            {
                errors.Add("INVALID_LIFECYCLE_STATE");
            }

            if (command.Amount > allocation.RemainingAmount)
            {
                errors.Add("INSUFFICIENT_POOL_CAPITAL");
            }
        }
    }

    private static void ValidateInvestor(
        ValidateCapitalOperationCommand command,
        InvestorSnapshot? investor,
        List<string> errors)
    {
        if (command.OperationType is CapitalOperationType.Commitment
            or CapitalOperationType.Contribution)
        {
            if (investor is null)
            {
                errors.Add("INVESTOR_NOT_FOUND");
                return;
            }

            if (!investor.IsAuthorized)
            {
                errors.Add("INVESTOR_NOT_AUTHORIZED");
            }

            if (investor.MaxContribution > 0
                && command.Amount > investor.RemainingAllowance)
            {
                errors.Add("INVESTOR_LIMIT_EXCEEDED");
            }
        }
    }
}

public sealed record CapitalPoolSnapshot(
    Guid PoolId,
    bool IsActive,
    decimal TotalBalance,
    decimal AvailableBalance,
    string Currency);

public sealed record CapitalReservationSnapshot(
    Guid ReservationId,
    Guid PoolId,
    bool IsActive,
    decimal ReservedAmount,
    decimal AllocatedAmount)
{
    public decimal RemainingAmount => ReservedAmount - AllocatedAmount;
}

public sealed record CapitalAllocationSnapshot(
    Guid AllocationId,
    Guid ReservationId,
    bool IsActive,
    decimal AllocatedAmount,
    decimal UtilizedAmount)
{
    public decimal RemainingAmount => AllocatedAmount - UtilizedAmount;
}

public sealed record InvestorSnapshot(
    Guid InvestorIdentityId,
    bool IsAuthorized,
    decimal MaxContribution,
    decimal TotalContributed)
{
    public decimal RemainingAllowance => MaxContribution - TotalContributed;
}