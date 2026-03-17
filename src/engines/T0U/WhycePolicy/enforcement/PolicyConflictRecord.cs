namespace Whycespace.Engines.T0U.WhycePolicy.Enforcement;

public enum ConflictType
{
    ACTION_CONFLICT,
    CONDITION_CONFLICT,
    PRIORITY_CONFLICT,
    LIFECYCLE_CONFLICT
}

public sealed record PolicyConflictRecord(
    string PolicyA,
    string PolicyB,
    ConflictType ConflictType,
    string ConflictDescription
);
