namespace Whycespace.Engines.T3I.Reporting.Policy;

public enum PolicyChangeType
{
    ADDED,
    REMOVED,
    MODIFIED
}

public sealed record PolicyChangeRecord(
    string FieldName,
    string? PreviousValue,
    string? ProposedValue,
    PolicyChangeType ChangeType
);
