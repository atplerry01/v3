namespace Whycespace.System.Midstream.WSS.Models;

public sealed record WorkflowGraph(
    string WorkflowId,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Transitions
);
