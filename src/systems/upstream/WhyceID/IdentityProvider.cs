namespace Whycespace.Systems.Upstream.WhyceID;

using Whycespace.Shared.Identity;

public sealed class IdentityProvider
{
    private readonly Dictionary<Guid, WhyceIdentity> _identities = new();

    public WhyceIdentity Register(string displayName, IReadOnlyList<string> roles)
    {
        var identity = new WhyceIdentity(
            Guid.NewGuid(), displayName, roles,
            new Dictionary<string, string> { ["registeredAt"] = DateTimeOffset.UtcNow.ToString("O") });
        _identities[identity.UserId] = identity;
        return identity;
    }

    public WhyceIdentity? Resolve(Guid userId)
    {
        _identities.TryGetValue(userId, out var identity);
        return identity;
    }

    public IReadOnlyList<WhyceIdentity> GetAll() => _identities.Values.ToList();
}
