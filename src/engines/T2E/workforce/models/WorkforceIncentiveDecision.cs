namespace Whycespace.Engines.T2E.Workforce.Models;

public sealed record WorkforceIncentiveDecision(
    bool Eligible,
    string Reason,
    decimal IncentiveAmount,
    string Currency,
    string IncentiveType,
    string PayoutReference
)
{
    public static WorkforceIncentiveDecision Success(
        decimal amount, string currency, string incentiveType, string payoutReference)
        => new(true, "Incentive approved", amount, currency, incentiveType, payoutReference);

    public static WorkforceIncentiveDecision Rejected(string reason)
        => new(false, reason, 0m, string.Empty, string.Empty, string.Empty);
}
