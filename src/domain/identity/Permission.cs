namespace Whycespace.Domain.Core.Identity;

public sealed record Permission(
    string Resource,
    string Action,
    string Scope
);
