namespace Whycespace.System.Upstream.Governance.Models;

public sealed record VoteTally(int Approve, int Reject, int Abstain)
{
    public int Total => Approve + Reject + Abstain;
}
