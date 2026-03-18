namespace Whycespace.Engines.T4A.Access.Contracts.Responses;

public sealed record PropertyResponse(
    string PropertyId,
    string Address,
    decimal AskingPrice,
    string Currency,
    string Status);
