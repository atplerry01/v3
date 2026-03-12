namespace Whycespace.Engines.T1M.WSS.Graph;

public sealed class GraphValidationException : Exception
{
    public IReadOnlyList<string> Violations { get; }

    public GraphValidationException(IReadOnlyList<string> violations)
        : base($"Graph validation failed: {string.Join("; ", violations)}")
    {
        Violations = violations;
    }
}
