namespace Whycespace.Engines.T4A.Access.Contracts.Dto;

public sealed record PropertyListingDto(
    string PropertyId,
    string Address,
    decimal AskingPrice,
    string Currency,
    string PropertyType,
    string Status);
