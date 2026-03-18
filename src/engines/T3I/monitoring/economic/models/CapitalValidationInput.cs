using Whycespace.Engines.T3I.Monitoring.Economic.Engines;
namespace Whycespace.Engines.T3I.Monitoring.Economic.Models;

public sealed record CapitalValidationInput(
    ValidateCapitalOperationCommand Command,
    CapitalPoolSnapshot? Pool,
    CapitalReservationSnapshot? Reservation,
    CapitalAllocationSnapshot? Allocation,
    InvestorSnapshot? Investor);
