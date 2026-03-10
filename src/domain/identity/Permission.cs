namespace Whycespace.Domain.Identity;

public sealed record Permission(
    string Resource,
    string Action,
    string Scope
);
